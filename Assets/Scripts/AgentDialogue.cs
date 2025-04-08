using UnityEngine;

public class AgentDialogue : MonoBehaviour
{
    bool isPlayerDetected = false;

    void Update()
    {
        if (isPlayerDetected)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("Hello, I am an agent!");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.name == "Character")
        {
            isPlayerDetected = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.name == "Character")
        {
            isPlayerDetected = false;
        }
    }
}
