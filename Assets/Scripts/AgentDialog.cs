using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

// AI Agent dialog system
public class AgentDialog : MonoBehaviour
{
    public GameObject dialogBox;
    public GameObject dialogTemplate;
    public GameObject currentDialog;

    private bool isPlayerDetected = false;
    private Transform detectedPlayer;
    private Camera playerCamera;
    private bool hasSentRequest = false;
    private int buttonIndex = 0;
    private float lastNavTime = 0f;
    private float navDelay = 0.5f;
    private GameObject[] dialogButtons;
    private Selectable currentUISelection;
    private float previousSpeed = -1f;
    private string chatHistory = "";
    [SerializeField] private SpeechToText speechToText;
    private bool isRecording = false;
    private GameObject tooltip;


    void Update()
    {
        if (isPlayerDetected && (Input.GetKeyDown(KeyCode.M) || Input.GetButtonDown(InputMappings.ButtonHamburger)))
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

        buttonIndex = 0;
        HighlightButton(buttonIndex);

        isRecording = false;

        // Disable player movement
        GameObject character = GameObject.FindGameObjectWithTag("Character");
        if (character != null)
        {
            var movement = character.GetComponent<CharacterMovement>();
            if (movement != null)
            {
                if (previousSpeed < 0f)
                {
                    previousSpeed = movement.speed;
                }

                movement.speed = 0;
            }
        }
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

        var speechTMP = currentDialog?.transform?.Find("SpeechToTextTMP")?.GetComponent<TextMeshProUGUI>()?.gameObject;

        if (speechTMP != null)
        {
            speechTMP.SetActive(dialogButtons[buttonIndex]?.tag == "Button2");
        }

        if (Input.GetKeyDown(KeyCode.Y) || Input.GetButtonDown(InputMappings.ButtonY))
        {
            if (dialogButtons[buttonIndex].tag == "Button1")
            {
                Debug.Log("Button1 selected and Y pressed.");
            }
            else if (dialogButtons[buttonIndex].tag == "Button2")
            {
                // Voice input logic
                if (!isRecording)
                {
                    StartSpeechInput();
                }
                else
                {
                    StopSpeechInput();
                }
            }
            else if (dialogButtons[buttonIndex].tag == "Button3")
            {
                CloseDialog();
            }
        }
    }

    // Start speech-to-text recording
    private void StartSpeechInput()
    {
        if (speechToText != null)
        {
            isRecording = true;
            SetButton2Text("Stop");
            var speechTMP = currentDialog?.transform?.Find("SpeechToTextTMP")?.GetComponent<TextMeshProUGUI>();
            if (speechTMP != null)
            {
                speechTMP.text = "Listening... (Y to stop recording)";
            }
            speechToText.StartRecording(OnSpeechResult);
        }
    }

    // Stop speech-to-text recording
    private void StopSpeechInput()
    {
        if (speechToText != null)
        {
            speechToText.StopRecording();
            SetButton2Text("Ask Agent");
            isRecording = false;
            // Do not change TMP text here; OnSpeechResult will update it
        }
    }

    // Callback for speech-to-text result
    private void OnSpeechResult(string result)
    {
        SetButton2Text("Ask Agent");
        isRecording = false;
        var speechTMP = currentDialog?.transform?.Find("SpeechToTextTMP")?.GetComponent<TextMeshProUGUI>();
        if (speechTMP != null)
        {
            speechTMP.text = "User: " + result;
        }
        if (!string.IsNullOrEmpty(result))
        {
            StartCoroutine(DelayedShowThinkingDialog(result));
        }
    }

    // Coroutine to delay showing the "Thinking..." dialog
    private IEnumerator DelayedShowThinkingDialog(string question)
    {
        yield return new WaitForSeconds(2f); // Delay for 2 seconds
        ShowThinkingDialog();
        StartCoroutine(SendToGemini(question));
    }

    // Utility to set Button2 text
    private void SetButton2Text(string text)
    {
        if (dialogButtons != null && dialogButtons.Length > 1 && dialogButtons[1] != null)
        {
            var btnText = dialogButtons[1].GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
                btnText.text = text;
        }
    }

    private void UpdateSpeechToText(string text)
    {
        var tmp = currentDialog?.transform?.Find("SpeechToTextTMP")?.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
            tmp.text = text;
    }

    // Sends the user's question to the Gemini API and processes the response
    private IEnumerator SendToGemini(string question)
    {
        string url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key=" + Environment.GEMINI_API_KEY;

        string userPrompt;
        if (string.IsNullOrEmpty(question))
        {
            userPrompt = "You are a real estate agent. Act professionally but be cheerful. Ask how you can help out. Output must be a JSON object with 'response' and optional 'destination' field. Valid destinations: Entrance, LivingRoom, Bedroom1, Bedroom2, MasterBedroom, Office, Garage, Bathroom, or empty string. No emojis. Max 250 characters.";
        }
        else
        {
            userPrompt = "Chat History: " + chatHistory + "\nUser: " + question + "\nYou are a professional real estate agent giving a tour. Respond to the user clearly about a 3-bed, 1-bath, single-story home. There is an office, living room, and kitchen. There are no windows, don't mention natural light. The master bedroom has room for a large bed, dresser, and nightstands. The other bedrooms are cozy and can fit 1 bed and a dresser. It has 2000 square feet. The user might ask to be shown a specific room. However, if it is not explicitly mentioned do not populate destination. Do not infer the destination from context. If you take a user to a room, only say Sure, let's go to that room. Only respond in the following JSON format: {\\\"response\\\": \\\"<text>\\\", \\\"destination\\\": \\\"<room name or null>\\\"}. Valid destination values: Entrance, LivingRoom, Bedroom1, Bedroom2, MasterBedroom, Office, Garage, Bathroom, or empty string. If you want to take the user to a destination, first ask for confirmation. Only if they confirm do you populate destination and give a brief description of the destination. Never ask a question and have a destination value at the same time. No emojis. Max 250 characters.";
        }

        string jsonRequestBody = @"{
            ""contents"": [
                {
                    ""role"": ""user"",
                    ""parts"": [
                        {
                            ""text"": """ + userPrompt + @"""
                        }
                    ]
                }
            ],
            ""generationConfig"": {
                ""response_mime_type"": ""application/json"",
                ""response_schema"": {
                    ""type"": ""OBJECT"",
                    ""properties"": {
                        ""response"": {""type"": ""STRING""},
                        ""destination"": {""type"": ""STRING""}
                    },
                    ""required"": [""response""]
                }
            }
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
                (string reply, string destination) = ExtractResponseAndDestination(responseText);

                if (string.IsNullOrEmpty(question))
                    chatHistory += "\nAgent: " + reply;
                else
                    chatHistory += "\nUser: " + question + "\nAgent: " + reply;

                StartCoroutine(HandleAgentResponse(reply, destination));
            }
        }
    }

    // Extracts the response and destination from the Gemini API response
    private (string, string) ExtractResponseAndDestination(string json)
    {
        try
        {
            var parsed = JsonUtility.FromJson<GeminiResponseWrapper>(json);
            if (parsed != null && parsed.candidates.Length > 0)
            {
                var part = parsed.candidates[0].content.parts[0];
                var data = JsonUtility.FromJson<GeminiStructuredResponse>(part.text);
                return (data.response, data.destination);
            }
        }
        catch
        {
            Debug.LogWarning("Failed to parse structured response from Gemini.");
        }

        return ("Sorry, I can't help right now.", null);
    }

    [System.Serializable]
    private class GeminiResponseWrapper
    {
        public Candidate[] candidates;
    }

    [System.Serializable]
    private class Candidate
    {
        public Content content;
    }

    [System.Serializable]
    private class Content
    {
        public Part[] parts;
    }

    [System.Serializable]
    private class Part
    {
        public string text;
    }

    [System.Serializable]
    private class GeminiStructuredResponse
    {
        public string response;
        public string destination;
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
                    yield return new WaitForSeconds(0.01f);
                }
            }
        }
    }

    // Navigates the agent to the specified destination if needed
    private IEnumerator HandleAgentResponse(string reply, string destination)
    {
        yield return StartCoroutine(TypeText(reply));

        if (!string.IsNullOrEmpty(destination))
        {
            Debug.Log("User wants to go to: " + destination);

            // Disable dialog buttons
            foreach (var button in dialogButtons)
            {
                if (button != null)
                {
                    Button btnComponent = button.GetComponent<Button>();
                    if (btnComponent != null)
                    {
                        btnComponent.interactable = false;
                    }
                }
            }

            yield return new WaitForSeconds(2f);

            GameObject destinationMarker = null;

            switch (destination)
            {
                case "Entrance":
                    destinationMarker = GameObject.Find("Entrance Marker");
                    break;
                case "LivingRoom":
                    destinationMarker = GameObject.Find("Living Room Marker");
                    break;
                case "Bedroom1":
                    destinationMarker = GameObject.Find("Bedroom1 Marker");
                    break;
                case "Bedroom2":
                    destinationMarker = GameObject.Find("Bedroom2 Marker");
                    break;
                case "MasterBedroom":
                    destinationMarker = GameObject.Find("BedroomMaster Marker");
                    break;
                case "Office":
                    destinationMarker = GameObject.Find("Office Marker");
                    break;
                case "Garage":
                    destinationMarker = GameObject.Find("Garage Marker");
                    break;
                case "Bathroom":
                    destinationMarker = GameObject.Find("BathroomCommon Marker");
                    break;
            }

            if (destinationMarker != null)
            {
                var agentNavigate = GetComponent<Navigate>();
                if (agentNavigate != null)
                    agentNavigate.current_room = destinationMarker;
            }

            CloseDialog();
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
        isRecording = false;
        SetButton2Text("Ask Agent...");
        var speechTMP = currentDialog?.transform?.Find("SpeechToTextTMP")?.GetComponent<TextMeshProUGUI>()?.gameObject;
        if (speechTMP != null)
            speechTMP.SetActive(false);
    }

    // When the player enters the trigger area, set the detected player and camera
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Character"))
        {
            isPlayerDetected = true;
            detectedPlayer = other.transform;

            Transform[] allChildren = detectedPlayer.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in allChildren)
            {
                if (child.name == "AIToolTip")
                {
                    tooltip = child.gameObject;
                    Debug.Log("Tooltip found!");
                    break;
                }
            }

            if (tooltip != null)
                tooltip.SetActive(true);
        }
    }

    // When the player exits the trigger area, reset the dialog and player state
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Character"))
        {
            isPlayerDetected = false;
            detectedPlayer = null;
            playerCamera = null;
            hasSentRequest = false;

            if (tooltip != null)
                tooltip.SetActive(false);

            if (currentDialog != null)
            {
                CloseDialog();
            }
        }
    }
}