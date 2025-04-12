using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class RaycastManager : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public float rayDistance = 10f;
    public GameObject teleportationPlane;
    public GameObject character;
    public GameObject objectMenuCanvas;
    public GameObject settingsMenuCanvas;
    public GraphicRaycaster uiRaycaster;
    public EventSystem eventSystem;

    private string[] settingsButtonTags = { "Resume", "RaycastLength", "Speed", "Accessibility" };
    private int currentButtonIndex;
    private InteractableObject currentInteractable = null;
    private InteractableObject currentInteractableWithMenu = null;
    private PointerEventData pointerEventData;
    private Selectable currentUISelection = null;
    private float navigationDelay = 0.2f;
    private float lastNavigationTime = 0f;

    void Update()
    {
        if (Input.GetButtonDown(InputMappings.ButtonMenu) || Input.GetKeyDown(KeyCode.O))
        {
            OpenSettingsMenu();
        }

        if (HandleMenu(settingsMenuCanvas, settingsButtonTags, ref currentButtonIndex, ref lastNavigationTime, navigationDelay))
        {
            DisableCharacterMovementAndLineRenderer();
            return;
        }

        EnableCharacterMovementAndLineRenderer();

        Vector3 startPosition = transform.position + Vector3.down * 0.1f;
        Vector3 endPosition = startPosition + transform.forward * rayDistance;
        RaycastHit hit;

        /*
         * Handle object menu raycast
         */
        if (objectMenuCanvas.activeSelf && uiRaycaster != null && eventSystem != null)
        {
            character.GetComponent<CharacterMovement>().enabled = false;
            pointerEventData = new PointerEventData(eventSystem);
            Ray ray = new Ray(startPosition, transform.forward);
            Plane uiPlane = new Plane(objectMenuCanvas.transform.forward, objectMenuCanvas.transform.position);
            float distance;

            if (uiPlane.Raycast(ray, out distance))
            {
                Vector3 worldPos = ray.GetPoint(distance);
                Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
                pointerEventData.position = screenPos;

                // Perform UI raycast
                List<RaycastResult> results = new List<RaycastResult>();
                uiRaycaster.Raycast(pointerEventData, results);

                if (results.Count > 0)
                {
                    // Find the button that is being hovered over
                    GameObject uiElement = results[0].gameObject;
                    Selectable selectable = uiElement.GetComponentInParent<Selectable>();

                    if (selectable != null)
                    {
                        // Highlight the button
                        currentUISelection = selectable;
                        Button button = selectable as Button;
                        button.GetComponent<Image>().color = Color.yellow;

                        // Handle button click
                        if (Input.GetButtonDown(InputMappings.ButtonB) || Input.GetKeyDown(KeyCode.B))
                        {
                            if (button != null)
                            {
                                // Grab
                                if (button.gameObject.name == "Grab")
                                {
                                    currentInteractableWithMenu.Grab();
                                }

                                // Close menu
                                CloseObjectMenu();
                            }
                        }

                        // Set endPosition of ray to the hit point on the menu
                        endPosition = worldPos;
                    }
                }
            }
        }

        // If the object menu item is no longer open, re-enable character movement
        if (currentUISelection != null)
        {
            pointerEventData = new PointerEventData(eventSystem);
            Ray ray = new Ray(startPosition, transform.forward);
            Plane uiPlane = new Plane(objectMenuCanvas.transform.forward, objectMenuCanvas.transform.position);
            float distance;

            if (uiPlane.Raycast(ray, out distance))
            {
                Vector3 worldPos = ray.GetPoint(distance);
                Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
                pointerEventData.position = screenPos;
                List<RaycastResult> results = new List<RaycastResult>();
                uiRaycaster.Raycast(pointerEventData, results);

                bool isStillHovering = false;
                foreach (var result in results)
                {
                    if (result.gameObject.GetComponentInParent<Selectable>().gameObject == currentUISelection.gameObject)
                    {
                        isStillHovering = true;
                        break;
                    }
                }

                if (!isStillHovering)
                {
                    character.GetComponent<CharacterMovement>().enabled = true;
                    currentUISelection.GetComponent<Image>().color = Color.white;
                    currentUISelection = null;
                }
            }
        }

        /*
         * Handle line renderer
         */
        if (Physics.Raycast(startPosition, transform.forward, out hit, rayDistance))
        {
            endPosition = hit.point;
        }

        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, startPosition);
            lineRenderer.SetPosition(1, endPosition);
        }

        /*
         * Handle interactable objects
         */
        if (currentInteractable != null)
        {
            currentInteractable.GetComponent<Outline>().enabled = false;
            currentInteractable = null;
        }
        if (Physics.Raycast(startPosition, transform.forward, out hit, rayDistance))
        {
            InteractableObject interactable = hit.collider.GetComponent<InteractableObject>();
            if (interactable != null)
            {
                Outline outline = interactable.GetComponent<Outline>();
                if (outline != null)
                {
                    // Enable outline
                    outline.enabled = true;
                    currentInteractable = interactable;
                    // Display menu when X button is pressed
                    if (!interactable.isGrabbed && (Input.GetButtonDown(InputMappings.ButtonX) || Input.GetKeyDown(KeyCode.X)))
                    {
                        OpenObjectMenu(interactable);
                    }
                }
            }
        }

        /*
         * Handle teleportation
         */
        if ((Input.GetButtonDown(InputMappings.ButtonY) || Input.GetKeyDown(KeyCode.Y)) && hit.collider != null && hit.collider.gameObject == teleportationPlane && currentInteractableWithMenu == null)
        {
            Vector3 teleportPosition = hit.point + Vector3.up;
            character.transform.position = teleportPosition;
        }
    }

    // Helper function that displays the object menu and positions it in front of the object
    private void OpenObjectMenu(InteractableObject interactable)
    {
        if (objectMenuCanvas.activeSelf && currentInteractableWithMenu == interactable)
        {
            objectMenuCanvas.SetActive(false);
            currentInteractableWithMenu = null;
            currentUISelection = null;
            character.GetComponent<CharacterMovement>().enabled = true;
            return;
        }
        objectMenuCanvas.SetActive(false);
        currentInteractableWithMenu = interactable;
        objectMenuCanvas.transform.position = interactable.transform.position + Vector3.up * 2f;
        objectMenuCanvas.transform.LookAt(Camera.main.transform);
        objectMenuCanvas.transform.Rotate(0, 180, 0);
        objectMenuCanvas.SetActive(true);
    }

    // Helper function that closes the object menu
    public void CloseObjectMenu()
    {
        if (currentInteractableWithMenu != null)
        {
            currentInteractableWithMenu.Exit();
            currentInteractableWithMenu = null;
        }
    }

    // Helper function to draw an action menu
    private bool HandleMenu(GameObject menuCanvas, string[] buttonTags, ref int buttonIndex, ref float lastNavTime, float navDelay)
    {
        if (menuCanvas.activeSelf)
        {
            float vertComp = Input.GetAxis("Vertical");
            if (Time.time - lastNavTime > navDelay)
            {
                if (vertComp > 0.5f && buttonIndex > 0)
                {
                    if (currentUISelection != null)
                        currentUISelection.GetComponent<Image>().color = Color.white;

                    // Find the next active button going up
                    int newIndex = buttonIndex - 1;
                    GameObject nextButton = null;

                    while (newIndex >= 0)
                    {
                        nextButton = GameObject.FindGameObjectWithTag(buttonTags[newIndex]);
                        if (nextButton != null && nextButton.activeSelf)
                            break;
                        newIndex--;
                    }

                    if (newIndex >= 0 && nextButton != null && nextButton.activeSelf)
                    {
                        buttonIndex = newIndex;
                        nextButton.GetComponent<Image>().color = Color.yellow;
                        currentUISelection = nextButton.GetComponent<Selectable>();
                        lastNavTime = Time.time;
                    }
                }
                else if (vertComp < -0.5f && buttonIndex < buttonTags.Length - 1)
                {
                    if (currentUISelection != null)
                        currentUISelection.GetComponent<Image>().color = Color.white;

                    // Find the next active button going down
                    int newIndex = buttonIndex + 1;
                    GameObject nextButton = null;

                    while (newIndex < buttonTags.Length)
                    {
                        nextButton = GameObject.FindGameObjectWithTag(buttonTags[newIndex]);
                        if (nextButton != null && nextButton.activeSelf)
                            break;
                        newIndex++;
                    }

                    if (newIndex < buttonTags.Length && nextButton != null && nextButton.activeSelf)
                    {
                        buttonIndex = newIndex;
                        nextButton.GetComponent<Image>().color = Color.yellow;
                        currentUISelection = nextButton.GetComponent<Selectable>();
                        lastNavTime = Time.time;
                    }
                }
            }

            // Handle menu item selection
            if (Input.GetButtonDown(InputMappings.ButtonB) || Input.GetKeyDown(KeyCode.B))
            {
                MenuFunctions menuFunctions = GetComponent<MenuFunctions>();
                switch (buttonTags[buttonIndex])
                {
                    case "Resume":
                        menuCanvas.SetActive(false);
                        EnableCharacterMovementAndLineRenderer();
                        break;
                    case "RaycastLength":
                        menuFunctions.SetRaycastLength();
                        break;
                    case "Speed":
                        menuFunctions.SetSpeed();
                        break;
                    case "Accessibility":
                        Debug.Log("Accessibility settings not implemented yet.");
                        break;
                }
            }
            return true;
        }
        return false;
    }

    // Helper functions to enable and disable character movement and line renderer
    private void DisableCharacterMovementAndLineRenderer()
    {
        character.GetComponent<CharacterMovement>().enabled = false;
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    private void EnableCharacterMovementAndLineRenderer()
    {
        character.GetComponent<CharacterMovement>().enabled = true;
        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
        }
    }

    // Helper function to open the settings menu
    private void OpenSettingsMenu()
    {
        if (settingsMenuCanvas.activeSelf)
        {
            settingsMenuCanvas.SetActive(false);
            character.GetComponent<CharacterMovement>().enabled = true;
            if (lineRenderer != null)
            {
                lineRenderer.enabled = true;
            }
            if (currentUISelection != null)
            {
                currentUISelection.GetComponent<Image>().color = Color.white;
                currentUISelection = null;
            }
        }
        else
        {
            settingsMenuCanvas.SetActive(true);
            settingsMenuCanvas.transform.SetParent(Camera.main.transform, false);
            settingsMenuCanvas.transform.localPosition = new Vector3(0, 0, 5f);
            settingsMenuCanvas.transform.localRotation = Quaternion.identity;
            character.GetComponent<CharacterMovement>().enabled = false;
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
            CloseObjectMenu();

            // Highlight the first button
            currentButtonIndex = 0;
            GameObject firstButton = GameObject.FindGameObjectWithTag(settingsButtonTags[currentButtonIndex]);
            if (firstButton != null)
            {
                firstButton.GetComponent<Image>().color = Color.yellow;
                currentUISelection = firstButton.GetComponent<Selectable>();
            }
        }
    }
}