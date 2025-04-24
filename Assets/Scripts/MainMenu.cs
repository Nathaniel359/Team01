using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;

public class MainMenu : MonoBehaviour
{
    public GameObject menuCanvas;
    public GameObject[] mainMenuUIElements; // All UI elements (buttons and dropdowns) in main menu panel
    private int currentIndex = 0;
    private float lastNavTime = 0f;
    private Color defaultTextColor = Color.black;
    public float navDelay = 0.3f;

    public Color defaultColor = Color.white;
    public Color highlightColor = Color.yellow;
    public Material overlayMaterial;

    private Selectable currentUISelection;
    private bool isQualityDropdownOpen = false;
    private bool isAccessibilityDropdownOpen = false;
    private AccessibilityTheme currentTheme = AccessibilityTheme.Default;

    void Start()
    {
        int defaultQuality = 2; //index for Medium

        // Set the default quality only if none is saved
        if (!PlayerPrefs.HasKey("QualityLevel"))
        {
            QualitySettings.SetQualityLevel(defaultQuality, true);
            PlayerPrefs.SetInt("QualityLevel", defaultQuality);
            PlayerPrefs.Save();
        }

        int currentQuality = QualitySettings.GetQualityLevel();

        // Sync dropdown to match current quality
        TMP_Dropdown qualityDropdown = GameObject.FindGameObjectWithTag("Quality").GetComponent<TMP_Dropdown>();
        if (qualityDropdown != null)
        {
            qualityDropdown.value = currentQuality;
            qualityDropdown.RefreshShownValue();
        }

        if (mainMenuUIElements.Length > 0)
        {
            currentUISelection = mainMenuUIElements[0].GetComponent<Selectable>();
            currentIndex = 0;
            HighlightCurrent();
        }
    }

    void Update()
    {
        HandleMenu();
    }

    void HandleMenu()
    {
        if (!menuCanvas.activeSelf) return;

        // Prevent navigation of settings buttons if the dropdown is open
        if (isAccessibilityDropdownOpen)
        {
            HandleAccessibilityDropdown();
            return;
        }
        if (isQualityDropdownOpen)
        {
            HandleQualityDropdown();
            return;
        }

        float vertComp = Input.GetAxis("Vertical");

        if (Time.time - lastNavTime > navDelay)
        {
            if (vertComp > 0.5f && currentIndex > 0)
            {
                UnhighlightCurrent();
                currentIndex--;
                HighlightCurrent();
                lastNavTime = Time.time;
            }
            else if (vertComp < -0.5f && currentIndex < mainMenuUIElements.Length - 1)
            {
                UnhighlightCurrent();
                currentIndex++;
                HighlightCurrent();
                lastNavTime = Time.time;
            }
        }

        if (Input.GetButtonDown(InputMappings.ButtonY) || Input.GetKeyDown(KeyCode.Y))
        {
            SelectCurrent();
        }
    }

    void HighlightCurrent()
    {
        GameObject uiElement = mainMenuUIElements[currentIndex];
        Debug.Log(mainMenuUIElements[currentIndex].name);
        if (uiElement)
        {
            currentUISelection = uiElement.GetComponent<Selectable>();
            if (currentTheme == AccessibilityTheme.HighContrast)
            {
                TextMeshProUGUI text = uiElement.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null) text.color = Color.yellow;
            }
            else
            {
                uiElement.GetComponent<Image>().color = highlightColor;
            }
        }
    }

    void UnhighlightCurrent()
    {
        if (currentUISelection)
        {
            if (currentTheme == AccessibilityTheme.HighContrast)
            {
                TextMeshProUGUI text = currentUISelection.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null) text.color = Color.white;
            }
            else
            {
                currentUISelection.GetComponent<Image>().color = defaultColor;
            }
        }
    }

    void SelectCurrent()
    {
        GameObject selectedElement = mainMenuUIElements[currentIndex];
        string elementName = selectedElement.name;

        switch (elementName)
        {
            case "Start":
                selectedElement.GetComponent<Button>().onClick.Invoke();
                //UnityEngine.SceneManagement.SceneManager.LoadScene("UnrealEstate");
                break;

            case "Quit":
                Application.Quit();
                #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false; // For testing in the editor
                #endif
                break;

            case "AccessibilityDropdown":
                HandleAccessibilityDropdown();
                break;

            case "QualityDropdown":
                HandleQualityDropdown();
                break;

            default:
                Debug.LogWarning("No action assigned to: " + elementName);
                break;
        }
    }

    /*
     * Helper function to handle the accessibility dropdown
     */
    private void HandleQualityDropdown()
    {
        TMP_Dropdown dropdown = GameObject.FindGameObjectWithTag("Quality").GetComponent<TMP_Dropdown>();
        if (dropdown != null)
        {
            if (!isQualityDropdownOpen)
            {
                dropdown.Show();
                StartCoroutine(ApplyDropdownOverlayMaterial());
                isQualityDropdownOpen = true;
                return;
            }

            float vertComp = Input.GetAxis("Vertical");
            if (Time.time - lastNavTime > navDelay)
            {
                if (vertComp > 0.5f && dropdown.value > 0)
                {
                    dropdown.value--;
                    lastNavTime = Time.time;
                }
                else if (vertComp < -0.5f && dropdown.value < dropdown.options.Count - 1)
                {
                    dropdown.value++;
                    lastNavTime = Time.time;
                }
            }

            if (Input.GetButtonDown(InputMappings.ButtonY) || Input.GetKeyDown(KeyCode.Y))
            {
                dropdown.Hide();
                QualitySettings.SetQualityLevel(dropdown.value, true);
                isQualityDropdownOpen = false;
                Debug.Log("Quality set to: " + QualitySettings.names[dropdown.value]);
            }
        }
    }

    /*
     * Helper function to handle the accessibility dropdown
     */
    private void HandleAccessibilityDropdown()
    {
        TMP_Dropdown dropdown = GameObject.FindGameObjectWithTag("Accessibility").GetComponent<TMP_Dropdown>();
        if (dropdown != null)
        {
            if (!isAccessibilityDropdownOpen)
            {
                dropdown.Show();
                StartCoroutine(ApplyDropdownOverlayMaterial());
                isAccessibilityDropdownOpen = true;
                return;
            }

            float vertComp = Input.GetAxis("Vertical");
            if (Time.time - lastNavTime > navDelay)
            {
                if (vertComp > 0.5f && dropdown.value > 0)
                {
                    dropdown.value--;
                    lastNavTime = Time.time;
                }
                else if (vertComp < -0.5f && dropdown.value < dropdown.options.Count - 1)
                {
                    dropdown.value++;
                    lastNavTime = Time.time;
                }
            }

            if (Input.GetButtonDown(InputMappings.ButtonY) || Input.GetKeyDown(KeyCode.Y))
            {
                dropdown.Hide();
                ApplyAccessibilityTheme(dropdown);
                isAccessibilityDropdownOpen = false;
            }
        }
    }

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

        // Update menu buttons
        UpdateButtons(menuCanvas, colors, textColor, backgroundSprite);
        UpdateDropdowns(menuCanvas, colors, textColor, backgroundSprite);

        // Update current dropdown label text color
        TextMeshProUGUI label = dropdown.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            if (currentTheme == AccessibilityTheme.HighContrast)
                label.color = Color.yellow;
            else if (currentTheme == AccessibilityTheme.ColorblindTritanopia)
                label.color = Color.white;
            else
                label.color = Color.black;
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

        // Update sliders
        Slider[] sliders = canvas.GetComponentsInChildren<Slider>(true);
        foreach (Slider slider in sliders)
        {
            Image sliderImage = slider.GetComponent<Image>();
            if (sliderImage != null)
            {
                sliderImage.color = colors.normalColor;
            }

            TextMeshProUGUI sliderLabel = slider.GetComponentInChildren<TextMeshProUGUI>();
            if (sliderLabel != null)
            {
                sliderLabel.color = textColor;
            }
        }
    }

    /*
     * Helper function to update button colors and text
     */
    private void UpdateDropdowns(GameObject canvas, ColorBlock colors, Color textColor, Sprite background)
    {
        if (canvas == null) return;

        // Apply theme to ALL TMP_Dropdowns inside menuCanvas
        TMP_Dropdown[] dropdowns = menuCanvas.GetComponentsInChildren<TMP_Dropdown>(true);
        foreach (var dropdown in dropdowns)
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
            if (label != null)
            {
                if (currentTheme == AccessibilityTheme.HighContrast)
                    label.color = Color.white;
                else if (currentTheme == AccessibilityTheme.ColorblindTritanopia)
                    label.color = Color.white;
                else
                    label.color = Color.black;
            }
        }

        // Update the active dropdown list in the scene (if open)
        Transform dropdownList = GameObject.Find("Dropdown List")?.transform;
        if (dropdownList != null)
        {
            Image[] dropdownImages = dropdownList.GetComponentsInChildren<Image>(true);
            foreach (var img in dropdownImages)
            {
                if (background != null)
                {
                    img.sprite = background;
                    img.type = Image.Type.Sliced;
                }
                img.color = colors.normalColor;
            }

            TextMeshProUGUI[] texts = dropdownList.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in texts)
            {
                text.color = textColor;
            }
        }
    }
}