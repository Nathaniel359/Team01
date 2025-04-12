using UnityEngine;
using System.Collections;

public class InteractableObject : MonoBehaviour
{
    public bool isGrabbed = false;
    private Camera mainCamera;
    private static InteractableObject grabbedObject = null;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (isGrabbed)
        {
            if (Input.GetButtonDown(InputMappings.ButtonA) || Input.GetKeyDown(KeyCode.A))
            {
                ReleaseObject();
            }
            else
            {
                FollowCamera();
            }
        }
    }

    // Grab logic
    public void Grab()
    {
        isGrabbed = true;
        grabbedObject = this;
        GetComponent<Rigidbody>().isKinematic = true;
    }

    private void ReleaseObject()
    {
        isGrabbed = false;
        grabbedObject = null;
        GetComponent<Rigidbody>().isKinematic = false;
    }

    private void FollowCamera()
    {
        transform.position = mainCamera.transform.position + mainCamera.transform.forward * 2f;
    }

    // Exit logic
    public void Exit()
    {
        GameObject menu = GameObject.FindGameObjectWithTag("ObjectMenu");
        if (menu != null)
        {
            menu.SetActive(false);
        }
    }
}