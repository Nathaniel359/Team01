using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuFunctions : MonoBehaviour
{
    public GameObject character;
    public Camera mainCamera;
    public TextMeshProUGUI raycastTextComponent;
    public TextMeshProUGUI speedTextComponent;

    void Update()
    {
        // Update raycast text
        RaycastManager raycastManager = mainCamera.GetComponent<RaycastManager>();
        float rayDistance = raycastManager.rayDistance;
        raycastTextComponent.text = $"Raycast Length: {rayDistance}m";

        // Update speed text
        CharacterMovement characterMovement = character.GetComponent<CharacterMovement>();
        float speed = characterMovement.speed;
        string speedText = speed == 5 ? "Low" : speed == 10 ? "Medium" : speed == 20 ? "High" : "Unknown";
        speedTextComponent.text = $"Speed: {speedText}";
    }

    public void SetRaycastLength()
    {
        RaycastManager raycastManager = mainCamera.GetComponent<RaycastManager>();
        float rayDistance = raycastManager.rayDistance;
        if (rayDistance == 1f)
        {
            raycastManager.rayDistance = 10f;
        }
        else if (rayDistance == 10f)
        {
            raycastManager.rayDistance = 50f;
        }
        else
        {
            raycastManager.rayDistance = 1f;
        }
    }

    public void SetSpeed()
    {
        CharacterMovement characterMovement = character.GetComponent<CharacterMovement>();
        float speed = characterMovement.speed;
        if (speed == 5f)
        {
            characterMovement.speed = 10f;
        }
        else if (speed == 10f)
        {
            characterMovement.speed = 20f;
        }
        else
        {
            characterMovement.speed = 5f;
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
