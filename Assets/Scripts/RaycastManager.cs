using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;

public enum AccessibilityTheme
{
    Default,
    HighContrast,
    ColorblindTritanopia
}

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
    public Material overlayMaterial;

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
    private bool isDropdownOpen = false;
    private AccessibilityTheme currentTheme = AccessibilityTheme.Default;

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
                            if (currentTheme == AccessibilityTheme.HighContrast)
                            {
                                TextMeshProUGUI text = selectable.GetComponentInChildren<TextMeshProUGUI>();
                                if (text != null) text.color = Color.yellow;
                            }
                            else if (image != null)
                            {
                                image.color = Color.yellow;
                            }


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
                        if (currentTheme == AccessibilityTheme.HighContrast)
                        {
                            TextMeshProUGUI text = lastHoveredUI.GetComponentInChildren<TextMeshProUGUI>();
                            if (text != null) text.color = Color.white;
                        }
                        else
                        {
                            Image image = lastHoveredUI.GetComponent<Image>();
                            if (image != null) image.color = Color.white;
                        }


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
            // Prevent navigation of settings buttons if the dropdown is open
            if (isDropdownOpen)
            {
                HandleAccessibilityDropdown();
                return true;
            }

            float vertComp = Input.GetAxis("Vertical");
            if (Time.time - lastNavTime > navDelay)
            {
                if (vertComp > 0.5f && buttonIndex > 0)
                {
                    if (currentUISelection != null)
                    {
                        if (currentTheme == AccessibilityTheme.HighContrast)
                        {
                            TextMeshProUGUI text = currentUISelection.GetComponentInChildren<TextMeshProUGUI>();
                            if (text != null) text.color = currentTheme == AccessibilityTheme.Default ? Color.black : Color.white;
                        }
                        else
                        {
                            currentUISelection.GetComponent<Image>().color = Color.white;
                        }
                    }


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
                        if (currentTheme == AccessibilityTheme.HighContrast)
                        {
                            TextMeshProUGUI text = nextButton.GetComponentInChildren<TextMeshProUGUI>();
                            if (text != null) text.color = Color.yellow;
                        }
                        else
                        {
                            nextButton.GetComponent<Image>().color = Color.yellow;
                        }

                        currentUISelection = nextButton.GetComponent<Selectable>();
                        lastNavTime = Time.time;
                    }
                }
                else if (vertComp < -0.5f && buttonIndex < buttonTags.Length - 1)
                {
                    if (currentTheme == AccessibilityTheme.HighContrast)
                    {
                        TextMeshProUGUI text = currentUISelection.GetComponentInChildren<TextMeshProUGUI>();
                        if (text != null) text.color = Color.white;
                    }
                    else
                    {
                        Image image = currentUISelection.GetComponent<Image>();
                        if (image != null) image.color = Color.white;
                    }

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
                        if (currentTheme == AccessibilityTheme.HighContrast)
                        {
                            TextMeshProUGUI text = nextButton.GetComponentInChildren<TextMeshProUGUI>();
                            if (text != null) text.color = Color.yellow;
                        }
                        else
                        {
                            nextButton.GetComponent<Image>().color = Color.yellow;
                        }
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
                        HandleAccessibilityDropdown();
                        break;
                }
            }
            return true;
        }
        return false;
    }

    /*
     * Helper function to handle the accessibility dropdown
     */
    private void HandleAccessibilityDropdown()
    {
        TMP_Dropdown dropdown = GameObject.FindGameObjectWithTag("Accessibility").GetComponent<TMP_Dropdown>();
        if (dropdown != null)
        {
            if (!isDropdownOpen)
            {
                dropdown.Show();
                StartCoroutine(ApplyDropdownOverlayMaterial());
                isDropdownOpen = true;
                return;
            }

            float vertComp = Input.GetAxis("Vertical");
            if (Time.time - lastNavigationTime > navigationDelay)
            {
                if (vertComp > 0.5f && dropdown.value > 0)
                {
                    dropdown.value--;
                    lastNavigationTime = Time.time;
                }
                else if (vertComp < -0.5f && dropdown.value < dropdown.options.Count - 1)
                {
                    dropdown.value++;
                    lastNavigationTime = Time.time;
                }
            }

            if (Input.GetButtonDown(InputMappings.ButtonY) || Input.GetKeyDown(KeyCode.Y))
            {
                dropdown.Hide();
                ApplyAccessibilityTheme(dropdown);
                isDropdownOpen = false;
            }
        }
    }

    /*
     * Helper function to apply the selected accessibility theme
     */
    private void ApplyAccessibilityTheme(TMP_Dropdown dropdown)
    {
        int themeIndex = dropdown.value;
        AccessibilityTheme theme = (AccessibilityTheme)themeIndex;
        ColorBlock colors = ColorBlock.defaultColorBlock;
        currentTheme = theme;

        Color textColor = Color.black;
        Sprite backgroundSprite = null;

        switch (theme)
        {
            case AccessibilityTheme.Default:
                colors.normalColor = Color.white;
                colors.highlightedColor = colors.normalColor;
                colors.pressedColor = new Color(0.6f, 0.6f, 0.6f);
                colors.selectedColor = colors.normalColor;
                colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                textColor = Color.black;
                break;

            case AccessibilityTheme.HighContrast:
                colors.normalColor = Color.black;
                colors.highlightedColor = colors.normalColor;
                colors.pressedColor = new Color(1f, 0.5f, 0f);
                colors.selectedColor = colors.normalColor;
                colors.disabledColor = new Color(0.2f, 0.2f, 0.2f);
                textColor = Color.white;
                backgroundSprite = Resources.Load<Sprite>("HighContrastBackground");
                break;

            case AccessibilityTheme.ColorblindTritanopia:
                colors.normalColor = new Color(0.1f, 0.4f, 0.8f);
                colors.highlightedColor = colors.normalColor;
                colors.pressedColor = new Color(0f, 0.3f, 0.6f);
                colors.selectedColor = colors.normalColor;
                colors.disabledColor = new Color(0.4f, 0.4f, 0.4f);
                textColor = Color.white;
                backgroundSprite = Resources.Load<Sprite>("TritanopiaBackground");
                break;
        }

        // Update settings menu buttons
        UpdateButtons(settingsMenuCanvas, colors, textColor, backgroundSprite);

        // Update object menu buttons
        UpdateButtons(heavyMenuCanvas, colors, textColor, backgroundSprite);
        UpdateButtons(lightMenuCanvas, colors, textColor, backgroundSprite);

        // Update dropdown colors
        if (dropdown != null)
        {
            TMP_Dropdown.DropdownEvent dropdownEvent = dropdown.onValueChanged;
            dropdown.onValueChanged = null;

            var dropdownColors = dropdown.colors;
            dropdownColors.normalColor = colors.normalColor;
            dropdownColors.highlightedColor = colors.highlightedColor;
            dropdownColors.pressedColor = colors.pressedColor;
            dropdownColors.selectedColor = colors.selectedColor;
            dropdownColors.disabledColor = colors.disabledColor;
            dropdown.colors = dropdownColors;

            dropdown.onValueChanged = dropdownEvent;

            // Update dropdown label text color
            TextMeshProUGUI label = dropdown.GetComponentInChildren<TextMeshProUGUI>();
            if (currentTheme == AccessibilityTheme.HighContrast)
            {
                label.color = Color.yellow;
            }
            else if (currentTheme == AccessibilityTheme.ColorblindTritanopia)
            {
                label.color = Color.white;
            }
            else
            {
                label.color = Color.black;
            }
        }

        Transform dropdownList = GameObject.Find("Dropdown List")?.transform;
        if (dropdownList != null)
        {
            Image[] dropdownImages = dropdownList.GetComponentsInChildren<Image>(true);
            foreach (var img in dropdownImages)
            {
                if (backgroundSprite != null)
                {
                    img.sprite = backgroundSprite;
                    img.type = Image.Type.Sliced;
                }
                img.color = colors.normalColor; // or whatever you want visible
            }

            TextMeshProUGUI[] texts = dropdownList.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in texts)
            {
                text.color = textColor;
            }
        }
    }

    /*
     * Helper function to apply the overlay material to dropdown images
     */
    private System.Collections.IEnumerator ApplyDropdownOverlayMaterial()
    {
        yield return null;

        GameObject dropdownList = GameObject.Find("Dropdown List");
        if (dropdownList != null)
        {
            var images = dropdownList.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                img.material = overlayMaterial; // Assign your UIOverlay material here
            }
        }
    }


    /*
     * Helper function to update button colors and text
     */
    private void UpdateButtons(GameObject canvas, ColorBlock colors, Color textColor, Sprite background)
    {
        if (canvas == null) return;

        Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons)
        {
            btn.colors = colors;

            Image btnImage = btn.GetComponent<Image>();
            if (btnImage != null && background != null)
            {
                btnImage.sprite = background;
                btnImage.type = Image.Type.Sliced;
            }

            TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.color = textColor;
            }
        }
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
            return;
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
                if (currentTheme == AccessibilityTheme.HighContrast)
                {
                    TextMeshProUGUI text = firstButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (text != null) text.color = Color.yellow;
                }
                else
                {
                    firstButton.GetComponent<Image>().color = Color.yellow;
                }
                currentUISelection = firstButton.GetComponent<Selectable>();
            }
        }
    }
}
