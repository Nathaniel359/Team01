using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class AgentDialog : MonoBehaviour
{
    public GameObject dialogBox;
    public GameObject dialogTemplate;
    private bool isPlayerDetected = false;
    private Transform detectedPlayer;
    private GameObject currentDialog;
    private Camera playerCamera;
    private bool hasSentRequest = false;

    private int buttonIndex = 0;
    private float lastNavTime = 0f;
    private float navDelay = 0.5f;
    private GameObject[] dialogButtons;
    private Selectable currentUISelection;
    private float previousSpeed = -1f;
    private bool inputFieldActive = false;
    private TMP_InputField agentInputField;

    void Update()
    {
        if (isPlayerDetected && Input.GetKeyDown(KeyCode.E))
        {
            if (!hasSentRequest)
            {
                ShowThinkingDialog();
                StartCoroutine(SendToGemini(""));
                hasSentRequest = true;
            }
        }

        if (currentDialog != null && playerCamera != null)
        {
            PositionDialog();
            if (!inputFieldActive)
                HandleDialogNavigation();
        }
    }

    // Displays the dialog box with the "Thinking..." message
    private void ShowThinkingDialog()
    {
        // Destroy existing dialog before creating a new one
        if (currentDialog != null)
        {
            Destroy(currentDialog);
            currentDialog = null;
        }

        CreateEmptyDialog();
        UpdateDialogText("Thinking...");
    }

    // Updates the text in the dialog box
    private void UpdateDialogText(string text)
    {
        if (currentDialog != null)
        {
            var textComponent = currentDialog.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = text;
            }
        }
    }

    // Creates an empty dialog box
    private void CreateEmptyDialog()
    {
        currentDialog = Instantiate(dialogTemplate, dialogBox.transform);
        currentDialog.transform.SetParent(dialogBox.transform, false);

        if (detectedPlayer != null)
            playerCamera = detectedPlayer.GetComponentInChildren<Camera>();

        currentDialog.SetActive(true);

        dialogButtons = new GameObject[]
        {
            currentDialog.transform.Find("Button1")?.gameObject,
            currentDialog.transform.Find("Button2")?.gameObject,
            currentDialog.transform.Find("Button3")?.gameObject
        };

        var inputGO = currentDialog.transform.Find("InputField (TMP)");
        if (inputGO != null)
        {
            agentInputField = inputGO.GetComponent<TMP_InputField>();
            if (agentInputField != null)
                agentInputField.gameObject.SetActive(false);
        }

        // Disable player movement
        GameObject character = GameObject.FindGameObjectWithTag("Character");
        if (character != null)
        {
            var movement = character.GetComponent<CharacterMovement>();
            if (movement != null)
            {
                previousSpeed = movement.speed;
                movement.speed = 0;
            }
        }

        buttonIndex = 0;
        HighlightButton(buttonIndex);
    }

    // Handles navigation through the dialog buttons
    private void HandleDialogNavigation()
    {
        if (dialogButtons == null || dialogButtons.Length == 0) return;

        float horComp = Input.GetAxis("Horizontal");

        if (Time.time - lastNavTime > navDelay)
        {
            int newIndex = buttonIndex;

            if (horComp > 0.5f && buttonIndex < dialogButtons.Length - 1)
                newIndex = buttonIndex + 1;
            else if (horComp < -0.5f && buttonIndex > 0)
                newIndex = buttonIndex - 1;

            if (newIndex != buttonIndex)
            {
                HighlightButton(newIndex);
                buttonIndex = newIndex;
                lastNavTime = Time.time;
            }
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            if (dialogButtons[buttonIndex].tag == "Button1")
            {
                GameObject livingRoomMarker = GameObject.Find("Living Room Marker");
                if (livingRoomMarker != null)
                {
                    var agentNavigate = GetComponent<Navigate>();
                    if (agentNavigate != null)
                        agentNavigate.current_room = livingRoomMarker;
                }

                CloseDialog();
            }
            else if (dialogButtons[buttonIndex].tag == "Button2")
            {
                ActivateInputField();
            }
            else if (dialogButtons[buttonIndex].tag == "Button3")
            {
                CloseDialog();
            }
        }
    }

    // Activates the input field for user input
    private void ActivateInputField()
    {
        if (agentInputField != null)
        {
            inputFieldActive = true;
            agentInputField.gameObject.SetActive(true);
            agentInputField.ActivateInputField();
            agentInputField.Select();
            agentInputField.caretPosition = 0;
            StartCoroutine(WaitForInput(agentInputField));
        }
    }

    // Waits for user input and processes it
    private IEnumerator WaitForInput(TMP_InputField inputField)
    {
        bool submitted = false;

        inputField.onSubmit.AddListener((_) => submitted = true);

        while (!submitted)
            yield return null;

        string question = inputField.text;
        inputField.text = "";
        inputField.onSubmit.RemoveAllListeners();
        inputField.gameObject.SetActive(false);

        inputFieldActive = false;
        HighlightButton(0);
        buttonIndex = 0;

        ShowThinkingDialog();
        StartCoroutine(SendToGemini(question));
    }

    // Sends the user's question to the Gemini API and processes the response
    private IEnumerator SendToGemini(string question)
    {
        string url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key=" + Environment.GEMINI_API_KEY;

        string prompt = string.IsNullOrEmpty(question)
            ? "You are a real estate agent. Act professionally and describe the home's features briefly. Keep responses short and engaging. The home is 1 floor and has 3 bedrooms and 2 bathrooms. For the first interaction, ask if the user wants to do a guided tour. Never use emojis and never generate a response more than 250 characters long. Respond to: Hi"
            : "You are a professional real estate agent talking to a buyer during a home tour. Respond briefly and clearly to the user's question about a 3-bed, 2-bath, single-story home. You are currently inside the home. Never use emojis. Max 250 characters. Question: " + question;

        string jsonRequestBody = @"{
            ""contents"": [
                {
                    ""role"": ""user"",
                    ""parts"": [
                        {
                            ""text"": """ + prompt + @"""
                        }
                    ]
                }
            ]
        }";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonRequestBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Gemini API Error: " + request.error);
                UpdateDialogText("Sorry, something went wrong.");
            }
            else
            {
                string responseText = request.downloadHandler.text;
                string extractedText = ExtractTextFromResponse(responseText);
                StartCoroutine(TypeText(extractedText));
            }
        }
    }

    // Extracts the text from the Gemini API response
    private string ExtractTextFromResponse(string json)
    {
        int textIndex = json.IndexOf("\"text\":");
        if (textIndex != -1)
        {
            int start = json.IndexOf("\"", textIndex + 7) + 1;
            int end = json.IndexOf("\"", start);
            if (start != -1 && end != -1)
                return json.Substring(start, end - start);
        }
        return "Sorry, I can't help right now.";
    }

    // Types the text character by character in the dialog box
    private IEnumerator TypeText(string fullText)
    {
        if (currentDialog != null)
        {
            var textComponent = currentDialog.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = "";
                foreach (char c in fullText)
                {
                    textComponent.text += c;
                    yield return new WaitForSeconds(0.03f);
                }
            }
        }
    }

    // Highlights the selected button in the dialog box
    private void HighlightButton(int index)
    {
        for (int i = 0; i < dialogButtons.Length; i++)
        {
            Image img = dialogButtons[i].GetComponent<Image>();
            if (img != null)
                img.color = (i == index) ? Color.yellow : Color.white;
        }

        currentUISelection = dialogButtons[index].GetComponent<Selectable>();
        if (currentUISelection != null)
            EventSystem.current.SetSelectedGameObject(currentUISelection.gameObject);
    }

    // Positions the dialog box in front of the player
    private void PositionDialog()
    {
        if (playerCamera != null)
        {
            Vector3 cameraForward = playerCamera.transform.forward;
            Vector3 cameraDown = -playerCamera.transform.up;
            Vector3 dialogPosition = playerCamera.transform.position + (cameraForward * 5f) + (cameraDown * 1.25f);

            dialogBox.transform.position = dialogPosition;
            dialogBox.transform.rotation = playerCamera.transform.rotation;
        }
    }

    // Closes the dialog box and resets player movement
    private void CloseDialog()
    {
        if (currentDialog != null)
        {
            Destroy(currentDialog);
            currentDialog = null;
        }

        GameObject character = GameObject.FindGameObjectWithTag("Character");
        if (character != null)
        {
            var movement = character.GetComponent<CharacterMovement>();
            if (movement != null && previousSpeed >= 0)
            {
                movement.speed = previousSpeed;
                previousSpeed = -1f;
            }
        }

        hasSentRequest = false;
        inputFieldActive = false;
    }

    // When the player enters the trigger area, set the detected player and camera
    private void OnTriggerEnter(Collider other)
    {
        if (other.name == "Character")
        {
            isPlayerDetected = true;
            detectedPlayer = other.transform;
        }
    }

    // When the player exits the trigger area, reset the dialog and player state
    private void OnTriggerExit(Collider other)
    {
        if (other.name == "Character")
        {
            isPlayerDetected = false;
            detectedPlayer = null;
            playerCamera = null;
            hasSentRequest = false;

            if (currentDialog != null)
            {
                Destroy(currentDialog);
                currentDialog = null;
            }
        }
    }
}