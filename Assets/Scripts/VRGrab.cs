using UnityEngine;

public class VRGrab : MonoBehaviour
{
    public Camera mainCamera;
    private GameObject grabbedObject = null;
    private Rigidbody grabbedRigidbody = null;
    private string grabbedTag = null;

    private float grabDistance = 4f; // distance from the camera where the object stays
    private float smoothSpeed = 10f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A) || Input.GetButtonDown("js10"))
        {
            if (grabbedObject == null)
            {
                TryGrabObject(); // try to grab an object
            }
            else
            {
                ReleaseObject(); // release the grabbed object
            }
        }
    }

    void FixedUpdate()
    {
        if (grabbedObject != null)
        {
            MoveGrabbedObject(); // smoothly move the object in front of the camera
        }
    }

    void TryGrabObject()
    {
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); // cast a ray from center of the view
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.CompareTag("Grab") || hit.collider.CompareTag("HeavyGrab"))
            {
                grabbedTag = hit.collider.tag;
                grabbedObject = hit.collider.gameObject;
                grabbedRigidbody = grabbedObject.GetComponent<Rigidbody>();

                if (grabbedRigidbody != null)
                {
                    grabbedRigidbody.useGravity = false;
                }
            }
        }
    }

    void MoveGrabbedObject()
    {
        if (grabbedObject != null)
        {
            // calculate smooth movement to target position in front of the camera
            Vector3 targetPosition = mainCamera.transform.position + mainCamera.transform.forward * grabDistance;
            if(grabbedTag == "HeavyGrab")
            {
                targetPosition.y = grabbedObject.transform.position.y; // Lock Y-axis movement
                smoothSpeed = 1f;
            } else
            {
                smoothSpeed = 10f;
            }
            grabbedObject.transform.position = Vector3.Lerp(grabbedObject.transform.position, targetPosition, smoothSpeed * Time.fixedDeltaTime);
        }
    }

    void ReleaseObject()
    {
        if (grabbedObject != null)
        {
            if (grabbedRigidbody != null)
            {
                grabbedRigidbody.useGravity = true;
            }

            grabbedObject = null; // clear the grabbed object
            grabbedRigidbody = null;
            grabbedTag = null;
        }
    }
}
