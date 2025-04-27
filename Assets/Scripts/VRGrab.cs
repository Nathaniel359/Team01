using UnityEngine;
using Photon.Pun;

public class VRGrab : MonoBehaviourPun
{
    public Camera mainCamera;
    public float rayLength = 10f;
    [HideInInspector] public GameObject grabbedObject = null; // Make this non-public for better control
    private Rigidbody grabbedRigidbody = null;
    private string grabbedTag = null;

    private float grabDistance = 5f;
    private float smoothSpeed = 10f;

    private LineRenderer lineRenderer;

    public static bool isGrabbing;
    public static VRGrab instance;
    private PhotonTransformView photonTransformView;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    void Update()
    {
        // Only allow input if this is the local player's avatar
        if (photonView.IsMine)
        {
            if (Input.GetKeyDown(KeyCode.Y) || Input.GetButtonDown(InputMappings.ButtonY))
            {
                if (grabbedObject != null)
                {
                    Debug.Log("Dropping");
                    ReleaseObject();
                }
            }

            // Continuously update and sync the grabbed object's position when holding it
            if (grabbedObject != null)
            {
                UpdateGrabbedObjectPosition();
            }
        }

        isGrabbing = (grabbedObject != null && photonView.IsMine); // Only true if we are grabbing and it's our hand
    }

    void UpdateGrabbedObjectPosition()
    {
        if (!photonView.IsMine || grabbedObject == null) return;

        Vector3 targetPosition = mainCamera.transform.position + mainCamera.transform.forward * grabDistance;

        photonView.RPC("RpcUpdateGrabbedObjectPosition", RpcTarget.AllBuffered, targetPosition);
    }

    public void ForceGrab(GameObject targetObject)
    {
        if (targetObject != null)
        {
            PhotonView targetPV = targetObject.GetComponent<PhotonView>();
            if (targetPV != null)
            {
                photonView.RPC("RpcGrabObject", RpcTarget.AllBuffered, targetPV.ViewID);
            }
            else
            {
                Debug.LogError("Tried to ForceGrab an object without a PhotonView!");
            }
        }
    }

    public void TryGrabObject()
    {
        if (!photonView.IsMine) return; // Only the local player can initiate a grab

        Debug.Log("TryGrabObject called");
        Vector3 rayOrigin = lineRenderer.GetPosition(0);
        RaycastHit hit;

        if (Physics.Raycast(rayOrigin, mainCamera.transform.forward, out hit, rayLength))
        {
            if (hit.collider.CompareTag("Grab") || hit.collider.CompareTag("HeavyGrab"))
            {
                PhotonView targetPhotonView = hit.collider.GetComponent<PhotonView>();
                if (targetPhotonView != null)
                {
                    // Request ownership if needed
                    if (targetPhotonView.OwnerActorNr != PhotonNetwork.LocalPlayer.ActorNumber)
                    {
                        targetPhotonView.RequestOwnership();
                    }

                    // Proceed to grab
                    photonView.RPC("RpcGrabObject", RpcTarget.AllBuffered, targetPhotonView.ViewID);
                }
                else
                {
                    Debug.LogError("Grabbed object does not have a PhotonView!");
                }
            }
        }
    }

    public void TryGrabObject(GameObject targetObject)
    {
        if (!photonView.IsMine) return; // Only the local player can initiate a grab

        if (targetObject == null) return;
        PhotonView targetPhotonView = targetObject.GetComponent<PhotonView>();
        if (targetPhotonView != null)
        {
            if (targetPhotonView.OwnerActorNr != PhotonNetwork.LocalPlayer.ActorNumber)
            {
                targetPhotonView.RequestOwnership();
            }
            photonView.RPC("RpcGrabObject", RpcTarget.AllBuffered, targetPhotonView.ViewID);
        }
        else
        {
            Debug.LogError("Tried to grab an object without a PhotonView!");
        }
    }

    [PunRPC]
    void RpcGrabObject(int viewID)
    {
        GameObject targetObject = PhotonView.Find(viewID).gameObject;
        if (targetObject != null)
        {
            Debug.Log($"RpcGrabObject called for {targetObject.name} with viewID {viewID}.  My ownership: {photonView.IsMine}, Object Owner: {targetObject.GetPhotonView().Owner}");

            grabbedTag = targetObject.tag;
            grabbedObject = targetObject;
            grabbedRigidbody = grabbedObject.GetComponent<Rigidbody>();

            photonTransformView = grabbedObject.GetComponent<PhotonTransformView>();
            if (photonTransformView == null)
            {
                photonTransformView = grabbedObject.AddComponent<PhotonTransformView>();
            }

            if (grabbedRigidbody != null)
            {
                grabbedRigidbody.useGravity = false;
                grabbedRigidbody.isKinematic = true;
            }

            grabDistance = Vector3.Distance(mainCamera.transform.position, grabbedObject.transform.position);
        }
        else
        {
            Debug.LogError($"Could not find GameObject with viewID: {viewID} for grabbing.");
        }
    }

    [PunRPC]
    void RpcUpdateGrabbedObjectPosition(Vector3 targetPosition)
    {
        if (grabbedObject != null)
        {
            if (grabbedTag == "HeavyGrab")
            {
                targetPosition.y = grabbedObject.transform.position.y;
                if (grabbedRigidbody != null)
                {
                    grabbedRigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;
                }
                smoothSpeed = 1f;
            }
            else
            {
                if (grabbedRigidbody != null)
                {
                    grabbedRigidbody.constraints = RigidbodyConstraints.None;
                }
                smoothSpeed = 10f;
            }

            Vector3 smoothedPosition = Vector3.Lerp(grabbedObject.transform.position, targetPosition, smoothSpeed * Time.fixedDeltaTime);
            if (grabbedRigidbody != null)
            {
                grabbedRigidbody.MovePosition(smoothedPosition);
            }
        }
    }

    public void ReleaseObject()
    {
        if (!photonView.IsMine) return; // Only the local player can initiate a release

        if (grabbedObject != null)
        {
            PhotonView grabbedObjectPhotonView = grabbedObject.GetComponent<PhotonView>();
            if (grabbedObjectPhotonView != null)
            {
                photonView.RPC("RpcReleaseObject", RpcTarget.AllBuffered);
            }
        }
    }

    [PunRPC]
    void RpcReleaseObject()
    {
        if (grabbedObject != null)
        {
            if (grabbedRigidbody != null)
            {
                grabbedRigidbody.useGravity = true;
                grabbedRigidbody.isKinematic = false; // Re-enable physics for all clients
            }
            grabbedObject = null;
            grabbedRigidbody = null;
            grabbedTag = null;
        }
    }
}