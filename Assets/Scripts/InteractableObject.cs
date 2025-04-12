using UnityEngine;
using System.Collections;

// Used to indicate an interactable object in the scene
public class InteractableObject : MonoBehaviour
{
    // Exit object menu
    public void Exit()
    {
        GameObject menu = GameObject.FindGameObjectWithTag("ObjectMenu");
        if (menu != null)
        {
            menu.SetActive(false);
        }
    }
}