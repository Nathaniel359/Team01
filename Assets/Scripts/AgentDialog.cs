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

    // Menu navigation variables
    private int buttonIndex = 0;
    private float lastNavTime = 0f;
    private float navDelay = 0.5f;
    private GameObject[] dialogButtons;
    private Selectable currentUISelection;
    private float previousSpeed = -1f;

    void Update()
    {
        if (isPlayerDetected && Input.GetKeyDown(KeyCode.E))
        {
            if (!hasSentRequest)
            {
                ShowThinkingDialog();
                StartCoroutine(GetGeminiResponse());
                hasSentRequest = true;
            }
        }

        if (currentDialog != null && playerCamera != null)
        {
            PositionDialog();
            HandleDialogNavigation();
        }
    }

    private void ShowThinkingDialog()
    {
        CreateEmptyDialog();
        UpdateDialogText("Thinking...");
    }

    private IEnumerator GetGeminiResponse()
    {
        string url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key=" + Environment.GEMINI_API_KEY;
        string prompt = "You are a real estate agent. Act professionally and describe the home's features briefly. Keep responses short and engaging. The home is 1 floor and has 3 bedrooms and 2 bathrooms. For the first interaction, ask if the user wants to do a guided tour. Never use emojis and never generate a response more than 250 characters long. Respond to: ";

        string jsonRequestBody = @"{
            ""contents"": [
                {
                    ""role"": ""user"",
                    ""parts"": [
                        {
                            ""text"": """ + prompt + "Hi" + @"""
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
                UpdateDialogText("Sorry, I can't help right now.");
            }
            else
            {
                string responseText = request.downloadHandler.text;
                string extractedText = ExtractTextFromResponse(responseText);
                StartCoroutine(TypeText(extractedText));
            }
        }
    }

    private string ExtractTextFromResponse(string json)
    {
        int textIndex = json.IndexOf("\"text\":");
        if (textIndex != -1)
        {
            int start = json.IndexOf("\"", textIndex + 7) + 1;
            int end = json.IndexOf("\"", start);
            if (start != -1 && end != -1)
            {
                return json.Substring(start, end - start);
            }
        }
        return "Sorry, I can't help right now.";
    }

    private void CreateEmptyDialog()
    {
        currentDialog = Instantiate(dialogTemplate, dialogBox.transform);
        currentDialog.transform.SetParent(dialogBox.transform, false);

        if (detectedPlayer != null)
        {
            playerCamera = detectedPlayer.GetComponentInChildren<Camera>();
        }

        currentDialog.SetActive(true);

        // Disable character movement
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

        dialogButtons = new GameObject[]
        {
            GameObject.FindGameObjectWithTag("Button1"),
            GameObject.FindGameObjectWithTag("Button2"),
            GameObject.FindGameObjectWithTag("Button3")
        };

        buttonIndex = 0;
        HighlightButton(buttonIndex);
    }

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
            Debug.Log($"Selected: {dialogButtons[buttonIndex].tag}");

            if (dialogButtons[buttonIndex].tag == "Button3")
            {
                // Close the dialog menu
                if (currentDialog != null)
                {
                    Destroy(currentDialog);
                    currentDialog = null;
                }

                // Restore the player's previous speed
                GameObject character = GameObject.FindGameObjectWithTag("Character");
                if (character != null)
                {
                    var movement = character.GetComponent<CharacterMovement>();
                    if (movement != null && previousSpeed >= 0)
                    {
                        movement.speed = previousSpeed; // Restore the stored speed
                        previousSpeed = -1f; // Reset the stored speed
                    }
                }
            }
        }
    }

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

    private void OnTriggerEnter(Collider other)
    {
        if (other.name == "Character")
        {
            isPlayerDetected = true;
            detectedPlayer = other.transform;
        }
    }

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
