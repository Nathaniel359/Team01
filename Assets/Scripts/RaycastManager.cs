using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;

// Main class for raycasting and interaction
public class RaycastManager : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public float rayDistance = 10f;
    public GameObject teleportationPlane;
    public GameObject character;
    public GameObject heavyMenuCanvas;
    public GameObject lightMenuCanvas;
    public GameObject settingsMenuCanvas;
    public EventSystem eventSystem;

    public TextMeshProUGUI rotateLabel;
    public TextMeshProUGUI scaleLabel;

    private string[] settingsButtonTags = { "Resume", "RaycastLength", "Speed", "Accessibility" };
    private int currentButtonIndex;
    private InteractableObject currentInteractable = null;
    private InteractableObject currentInteractableWithMenu = null;
    private PointerEventData pointerEventData;
    private Selectable currentUISelection = null;
    private Selectable lastHoveredUI = null;
    private GameObject activeObjectMenuCanvas = null;

    private Slider selectedSlider = null;
    public float sliderSpeed = 50f;

    private float navigationDelay = 0.2f;
    private float lastNavigationTime = 0f;

    void Update()
    {
        /*
         * Handle settings menu toggle
         */
        if (Input.GetButtonDown(InputMappings.ButtonMenu) || Input.GetKeyDown(KeyCode.O))
        {
            OpenSettingsMenu();
        }

        /*
         * Handle settings menu navigation
         */
        if (HandleMenu(settingsMenuCanvas, settingsButtonTags, ref currentButtonIndex, ref lastNavigationTime, navigationDelay))
        {
            DisableCharacterMovementAndLineRenderer();
            return;
        }

        EnableCharacterMovementAndLineRenderer();

        /*
         * Handle raycasting for interactable objects
         */
        Vector3 startPosition = transform.position + Vector3.down * 0.1f;
        Vector3 endPosition = startPosition + transform.forward * rayDistance;
        RaycastHit hit;

        /*
         * Adjust selected slider value
         */
        if (selectedSlider != null)
        {
            float horizontal = Input.GetAxis("Horizontal");
            if (Mathf.Abs(horizontal) > 0.1f)
            {
                float adjustmentSpeed = selectedSlider.gameObject.name == "Scale" ? sliderSpeed * 0.5f : sliderSpeed;
                selectedSlider.value += horizontal * Time.deltaTime * adjustmentSpeed;

                if (currentInteractableWithMenu != null)
                {
                    if (selectedSlider.gameObject.name == "Rotate")
                    {
                        currentInteractableWithMenu.transform.rotation = Quaternion.Euler(0f, selectedSlider.value, 0f);
                        if (rotateLabel != null)
                            rotateLabel.text = $"Rotate: {Mathf.RoundToInt(selectedSlider.value)}°";
                    }
                    else if (selectedSlider.gameObject.name == "Scale")
                    {
                        currentInteractableWithMenu.transform.localScale = Vector3.one * selectedSlider.value;
                        if (scaleLabel != null)
                            scaleLabel.text = $"Scale: {selectedSlider.value:F1}x";
                    }
                }
            }
        }

        /*
         * Handle object menu raycasting
         */
        if (activeObjectMenuCanvas != null && activeObjectMenuCanvas.activeSelf && eventSystem != null)
        {
            GraphicRaycaster currentRaycaster = activeObjectMenuCanvas.GetComponent<GraphicRaycaster>();
            if (currentRaycaster != null)
            {
                character.GetComponent<CharacterMovement>().enabled = false;
                pointerEventData = new PointerEventData(eventSystem);
                Ray ray = new Ray(startPosition, transform.forward);
                Plane uiPlane = new Plane(activeObjectMenuCanvas.transform.forward, activeObjectMenuCanvas.transform.position);
                float distance;

                if (uiPlane.Raycast(ray, out distance))
                {
                    Vector3 worldPos = ray.GetPoint(distance);
                    Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
                    pointerEventData.position = screenPos;

                    List<RaycastResult> results = new List<RaycastResult>();
                    currentRaycaster.Raycast(pointerEventData, results);

                    if (results.Count > 0)
                    {
                        GameObject uiElement = results[0].gameObject;
                        Selectable selectable = uiElement.GetComponentInParent<Selectable>();

                        if (selectable != null)
                        {
                            if (lastHoveredUI != null && lastHoveredUI != selectable)
                            {
                                Image lastImage = lastHoveredUI.GetComponent<Image>();
                                if (lastImage != null)
                                    lastImage.color = Color.white;
                            }

                            currentUISelection = selectable;
                            lastHoveredUI = selectable;

                            Button button = selectable.GetComponent<Button>();
                            Slider slider = selectable.GetComponent<Slider>();

                            Image image = selectable.GetComponent<Image>();
                            if (image != null)
                                image.color = Color.yellow;

                            if (slider != null)
                            {
                                selectedSlider = slider;

                                if (slider.gameObject.name == "Rotate" && rotateLabel != null)
                                    rotateLabel.text = $"Rotate: {Mathf.RoundToInt(slider.value)}°";
                                else if (slider.gameObject.name == "Scale" && scaleLabel != null)
                                    scaleLabel.text = $"Scale: {slider.value:F1}x";
                            }

                            if ((Input.GetButtonDown(InputMappings.ButtonY) || Input.GetKeyDown(KeyCode.Y)) && button != null)
                            {
                                if (button.gameObject.name == "Grab")
                                {
                                    if (currentInteractableWithMenu != null)
                                    {
                                        character.GetComponent<VRGrab>().TryGrabObject(currentInteractableWithMenu.gameObject);
                                    }
                                }
                                else if (button.gameObject.name == "Exit")
                                {
                                    CloseObjectMenu();
                                }
                                else if (button.gameObject.name == "Interact")
                                {
                                    if (currentInteractableWithMenu != null)
                                    {
                                        var light = currentInteractableWithMenu.GetComponent<LightToggle>();
                                        if (light != null)
                                        {
                                            light.ToggleLight();
                                            return;
                                        }

                                        var tv = currentInteractableWithMenu.GetComponent<TVToggle>();
                                        if (tv != null)
                                        {
                                            tv.ToggleTV();
                                            return;
                                        }

                                        var door = currentInteractableWithMenu.GetComponent<DoorToggle>();
                                        if (door != null)
                                        {
                                            door.ToggleDoor();
                                            return;
                                        }

                                        var sit = currentInteractableWithMenu.GetComponent<SitTarget>();
                                        if (sit != null)
                                        {
                                            sit.ToggleSit();
                                            return;
                                        }
                                    }
                                }

                                CloseObjectMenu();
                            }
                        }
                    }
                }
            }
        }

        /*
         * Handle UI hover state reset
         */
        if (lastHoveredUI != null && activeObjectMenuCanvas != null)
        {
            GraphicRaycaster currentRaycaster = activeObjectMenuCanvas.GetComponent<GraphicRaycaster>();
            if (currentRaycaster != null)
            {
                pointerEventData = new PointerEventData(eventSystem);
                Ray ray = new Ray(startPosition, transform.forward);
                Plane uiPlane = new Plane(activeObjectMenuCanvas.transform.forward, activeObjectMenuCanvas.transform.position);
                float distance;

                if (uiPlane.Raycast(ray, out distance))
                {
                    Vector3 worldPos = ray.GetPoint(distance);
                    Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
                    pointerEventData.position = screenPos;

                    List<RaycastResult> results = new List<RaycastResult>();
                    currentRaycaster.Raycast(pointerEventData, results);

                    bool isStillHovering = false;
                    foreach (var result in results)
                    {
                        Selectable hovered = result.gameObject.GetComponentInParent<Selectable>();
                        if (hovered != null && hovered == lastHoveredUI)
                        {
                            isStillHovering = true;
                            break;
                        }
                    }

                    if (!isStillHovering)
                    {
                        Image image = lastHoveredUI.GetComponent<Image>();
                        if (image != null)
                            image.color = Color.white;

                        lastHoveredUI = null;
                        currentUISelection = null;
                        selectedSlider = null;
                        character.GetComponent<CharacterMovement>().enabled = true;
                    }
                }
            }
        }

        /*
         * Handle teleportation
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
                    outline.enabled = true;
                    currentInteractable = interactable;
                    if (Input.GetButtonDown(InputMappings.ButtonX) || Input.GetKeyDown(KeyCode.X))
                    {
                        OpenObjectMenu(interactable);
                    }
                }
            }
        }

        if ((Input.GetButtonDown(InputMappings.ButtonA) || Input.GetKeyDown(KeyCode.P)) && hit.collider != null && hit.collider.gameObject == teleportationPlane && currentInteractableWithMenu == null)
        {
            Vector3 teleportPosition = hit.point + Vector3.up;
            character.transform.position = teleportPosition;
        }
    }

    /*
     * Helper function to open the object menu
     */
    private void OpenObjectMenu(InteractableObject interactable)
    {
        GameObject selectedMenuCanvas = interactable.CompareTag("Grab") ? lightMenuCanvas : heavyMenuCanvas;

        if (selectedMenuCanvas.activeSelf && currentInteractableWithMenu == interactable)
        {
            selectedMenuCanvas.SetActive(false);
            currentInteractableWithMenu = null;
            currentUISelection = null;
            lastHoveredUI = null;
            character.GetComponent<CharacterMovement>().enabled = true;
            activeObjectMenuCanvas = null;
            selectedSlider = null;
            return;
        }

        heavyMenuCanvas.SetActive(false);
        lightMenuCanvas.SetActive(false);

        currentInteractableWithMenu = interactable;

        Vector3 directionToCharacter = (character.transform.position - interactable.transform.position).normalized;
        selectedMenuCanvas.transform.position = interactable.transform.position + Vector3.up * 1.5f + directionToCharacter * 1f;

        selectedMenuCanvas.transform.LookAt(Camera.main.transform);
        selectedMenuCanvas.transform.Rotate(0, 180, 0);
        selectedMenuCanvas.SetActive(true);

        activeObjectMenuCanvas = selectedMenuCanvas;

        GameObject interactButton = selectedMenuCanvas.transform.Find("Interact").gameObject;
        if (interactButton != null)
        {
            bool hasInteractableComponent = interactable.GetComponent<LightToggle>() != null ||
                                            interactable.GetComponent<TVToggle>() != null ||
                                            interactable.GetComponent<DoorToggle>() != null ||
                                            interactable.GetComponent<SitTarget>() != null;

            interactButton.SetActive(hasInteractableComponent);
        }
    }

    /*
     * Helper function to close the object menu
     */
    public void CloseObjectMenu()
    {
        if (currentInteractableWithMenu != null)
        {
            currentInteractableWithMenu.Exit();
            currentInteractableWithMenu = null;
        }

        if (activeObjectMenuCanvas != null)
        {
            activeObjectMenuCanvas.SetActive(false);
            activeObjectMenuCanvas = null;
        }

        if (currentUISelection != null)
        {
            Image image = currentUISelection.GetComponent<Image>();
            if (image != null)
                image.color = Color.white;
            currentUISelection = null;
        }

        if (lastHoveredUI != null)
        {
            Image image = lastHoveredUI.GetComponent<Image>();
            if (image != null)
                image.color = Color.white;
            lastHoveredUI = null;
        }

        character.GetComponent<CharacterMovement>().enabled = true;
        selectedSlider = null;
    }

    /*
     * Helper function to handle menu navigation
     */
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
            if (Input.GetButtonDown(InputMappings.ButtonY) || Input.GetKeyDown(KeyCode.Y))
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

    /*
     * Helper function to disable character movement and line renderer
     */
    private void DisableCharacterMovementAndLineRenderer()
    {
        character.GetComponent<CharacterMovement>().enabled = false;
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    /*
     * Helper function to enable character movement and line renderer
     */
    private void EnableCharacterMovementAndLineRenderer()
    {
        character.GetComponent<CharacterMovement>().enabled = true;
        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
        }
    }

    /*
     * Helper function to open the settings menu
     */
    private void OpenSettingsMenu()
    {
        var agentDialog = FindFirstObjectByType<AgentDialog>();
        if (agentDialog != null && agentDialog.currentDialog != null)
        {
            return;
        }

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
                Image image = currentUISelection.GetComponent<Image>();
                if (image != null)
                    image.color = Color.white;
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

            currentButtonIndex = 0;
            GameObject firstButton = GameObject.FindGameObjectWithTag(settingsButtonTags[currentButtonIndex]);
            if (firstButton != null)
            {
                Image image = firstButton.GetComponent<Image>();
                if (image != null)
                    image.color = Color.yellow;
                currentUISelection = firstButton.GetComponent<Selectable>();
            }
        }
    }
}
