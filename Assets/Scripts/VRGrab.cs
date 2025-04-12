using UnityEngine;

// Handles grabbing and releasing objects
public class VRGrab : MonoBehaviour
{
    public Camera mainCamera;
    public float rayLength = 10f;
    public GameObject grabbedObject = null;
    private Rigidbody grabbedRigidbody = null;
    private string grabbedTag = null;

    private float grabDistance = 5f; // distance from the camera where the object stays
    private float smoothSpeed = 10f;

    private LineRenderer lineRenderer;

    private void Start()
    {
        lineRenderer = transform.GetComponent<LineRenderer>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Y) || Input.GetButtonDown(InputMappings.ButtonY))
        {
            if (grabbedObject != null)
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

    public void TryGrabObject()
    {
        Debug.Log("TryGrabObject called");
        Vector3 rayOrigin = lineRenderer.GetPosition(0);//mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(rayOrigin, mainCamera.transform.forward, out hit, rayLength))
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

                grabDistance = Vector3.Distance(mainCamera.transform.position, hit.point);
            }
        }
    }

    public void TryGrabObject(GameObject targetObject)
    {
        // Check if target is valid and has appropriate tag
        if (targetObject == null) return;
        if (targetObject.CompareTag("Grab") || targetObject.CompareTag("HeavyGrab"))
        {
            grabbedTag = targetObject.tag;
            grabbedObject = targetObject;
            grabbedRigidbody = grabbedObject.GetComponent<Rigidbody>();
            if (grabbedRigidbody != null)
            {
                grabbedRigidbody.useGravity = false;
            }
            grabDistance = Vector3.Distance(mainCamera.transform.position, grabbedObject.transform.position);
        }
    }

    void MoveGrabbedObject()
    {
        if (grabbedObject != null)
        {
            // calculate smooth movement to target position in front of the camera
            Vector3 targetPosition = mainCamera.transform.position + mainCamera.transform.forward * grabDistance;
            if (grabbedTag == "HeavyGrab")
            {
                targetPosition.y = grabbedObject.transform.position.y; // Lock Y-axis movement
                grabbedRigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;
                smoothSpeed = 1f;
            }
            else
            {
                grabbedRigidbody.constraints = RigidbodyConstraints.None;
                smoothSpeed = 10f;
            }
            Vector3 smoothedPosition = Vector3.Lerp(grabbedObject.transform.position, targetPosition, smoothSpeed * Time.fixedDeltaTime);
            grabbedRigidbody.MovePosition(smoothedPosition);

        }
    }

    public void ReleaseObject()
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
