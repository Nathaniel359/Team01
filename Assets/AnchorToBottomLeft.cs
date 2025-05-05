using UnityEngine;
using Photon.Pun;

public class AnchorToBottomLeft : MonoBehaviourPun
{
    public Camera vrCamera;
    public Vector3 offset = new Vector3(0.1f, 0.1f, 2f);

    void Start()
    {
        // Disable the canvas if it's not this client's player
        if (!photonView.IsMine)
        {
            gameObject.SetActive(false);
            return;
        }

        if (vrCamera == null)
            vrCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (!photonView.IsMine) return;

        Vector3 corner = new Vector3(0f, 0f, offset.z);
        Vector3 targetPosition = vrCamera.ViewportToWorldPoint(corner);

        targetPosition += vrCamera.transform.right * offset.x;
        targetPosition += vrCamera.transform.up * offset.y;

        transform.position = targetPosition;
    }
}
