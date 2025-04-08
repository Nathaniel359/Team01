using UnityEngine;
using TMPro;

public class AgentDialogue : MonoBehaviour
{
    public GameObject dialogueCanvas;
    public GameObject dialogueTemplate;
    private bool isPlayerDetected = false;
    private Transform detectedPlayer;
    private GameObject currentDialogue;
    private Camera playerCamera;

    void Update()
    {
        if (isPlayerDetected)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (currentDialogue == null)
                {
                    CreateDialogue("Hello, I am an agent!");
                }
            }
        }

        if (currentDialogue != null && playerCamera != null)
        {
            PositionDialogue();
        }
    }

    // Creates a dialogue canvas in front of the player
    private void CreateDialogue(string text)
    {
        currentDialogue = Instantiate(dialogueTemplate, dialogueCanvas.transform);
        currentDialogue.transform.SetParent(dialogueCanvas.transform, false);
        currentDialogue.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = text;

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

    // Positions the dialogue canvas to face the player
    private void PositionDialogue()
    {
        Vector3 agentPosition = transform.position;
        Vector3 dialoguePosition = agentPosition;
        dialogueCanvas.transform.position = dialoguePosition;
        dialogueCanvas.transform.LookAt(playerCamera.transform);
        dialogueCanvas.transform.Rotate(0, 180f, 0);
        dialogueCanvas.transform.rotation = Quaternion.Euler(0, dialogueCanvas.transform.rotation.eulerAngles.y, 0);
    }

    // Detects when the player is in range of the agent
    private void OnTriggerEnter(Collider other)
    {
        if (other.name == "Character")
        {
            isPlayerDetected = true;
            detectedPlayer = other.transform;
        }
    }

    // Detects when the player leaves the range of the agent
    private void OnTriggerExit(Collider other)
    {
        if (other.name == "Character")
        {
            isPlayerDetected = false;
            detectedPlayer = null;
            playerCamera = null;
            if (currentDialogue != null)
            {
                Destroy(currentDialogue);
                currentDialogue = null;
            }
        }
    }
}