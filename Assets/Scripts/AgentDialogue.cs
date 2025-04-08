using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using System.Collections;

public class AgentDialogue : MonoBehaviour
{
    public GameObject dialogueCanvas;
    public GameObject dialogueTemplate;
    private bool isPlayerDetected = false;
    private Transform detectedPlayer;
    private GameObject currentDialogue;
    private Camera playerCamera;
    private bool hasSentRequest = false;

    void Update()
    {
        if (isPlayerDetected && Input.GetKeyDown(KeyCode.E))
        {
            if (!hasSentRequest)
            {
                ShowThinkingDialogue();
                StartCoroutine(GetGeminiResponse());
                hasSentRequest = true;
            }
        }

        if (currentDialogue != null && playerCamera != null)
        {
            PositionDialogue();
        }
    }

    private void ShowThinkingDialogue()
    {
        CreateEmptyDialogue();
        UpdateDialogueText("Thinking...");
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
                UpdateDialogueText("Sorry, I can't help right now.");
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

    private void CreateEmptyDialogue()
    {
        currentDialogue = Instantiate(dialogueTemplate, dialogueCanvas.transform);
        currentDialogue.transform.SetParent(dialogueCanvas.transform, false);

        if (detectedPlayer != null)
        {
            playerCamera = detectedPlayer.GetComponentInChildren<Camera>();
            if (playerCamera == null)
            {
                Debug.LogWarning("MainCamera not found in detected player's hierarchy.");
            }
        }

        currentDialogue.SetActive(true);
    }

    private void UpdateDialogueText(string text)
    {
        if (currentDialogue != null)
        {
            var textComponent = currentDialogue.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = text;
            }
        }
    }

    private IEnumerator TypeText(string fullText)
    {
        if (currentDialogue != null)
        {
            var textComponent = currentDialogue.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
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

    private void PositionDialogue()
    {
        if (playerCamera != null)
        {
            Vector3 cameraForward = playerCamera.transform.forward;
            Vector3 cameraDown = -playerCamera.transform.up;
            Vector3 dialoguePosition = playerCamera.transform.position + (cameraForward * 5f) + (cameraDown * 1.25f);

            dialogueCanvas.transform.position = dialoguePosition;
            dialogueCanvas.transform.rotation = playerCamera.transform.rotation;
        }
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

            if (currentDialogue != null)
            {
                Destroy(currentDialogue);
                currentDialogue = null;
            }
        }
    }
}
