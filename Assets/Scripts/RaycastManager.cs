using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;

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

        // === Adjust selected slider value ===
        if (selectedSlider != null)
        {
            float horizontal = Input.GetAxis("Horizontal");
            if (Mathf.Abs(horizontal) > 0.1f)
            {
                selectedSlider.value += horizontal * Time.deltaTime * sliderSpeed;

                // Apply changes
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

                            // Highlighting stays the same
                            Image image = selectable.GetComponent<Image>();
                            if (image != null)
                                image.color = Color.yellow;

                            // If looking at a slider — auto-select it
                            if (slider != null)
                            {
                                selectedSlider = slider;

                                if (slider.gameObject.name == "Rotate" && rotateLabel != null)
                                    rotateLabel.text = $"Rotate: {Mathf.RoundToInt(slider.value)}°";
                                else if (slider.gameObject.name == "Scale" && scaleLabel != null)
                                    scaleLabel.text = $"Scale: {slider.value:F1}x";
                            }

                            // If pressing B while looking at a button — trigger button logic
                            if ((Input.GetButtonDown(InputMappings.ButtonB) || Input.GetKeyDown(KeyCode.B)) && button != null)
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

                                // Only close menu if button was clicked
                                CloseObjectMenu();
                            }

                        }
                    }
                }
            }
        }

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

        if ((Input.GetButtonDown(InputMappings.ButtonY) || Input.GetKeyDown(KeyCode.Y)) && hit.collider != null && hit.collider.gameObject == teleportationPlane && currentInteractableWithMenu == null)
        {
            Vector3 teleportPosition = hit.point + Vector3.up;
            character.transform.position = teleportPosition;
        }
    }

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
        selectedMenuCanvas.transform.position = interactable.transform.position + Vector3.up * 1f;
        selectedMenuCanvas.transform.LookAt(Camera.main.transform);
        selectedMenuCanvas.transform.Rotate(0, 180, 0);
        selectedMenuCanvas.SetActive(true);

        activeObjectMenuCanvas = selectedMenuCanvas;

        // Show "Interact" button only if the object has one of the specified components
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

    private bool HandleMenu(GameObject menuCanvas, string[] buttonTags, ref int buttonIndex, ref float lastNavTime, float navDelay)
    {
        // Your existing HandleMenu method can stay unchanged
        return false;
    }

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

    private void OpenSettingsMenu()
    {
        var agentDialog = FindObjectOfType<AgentDialog>();
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
