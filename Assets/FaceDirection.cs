using UnityEngine;
using Photon.Pun;

public class FaceDirection : MonoBehaviourPun
{
    public Transform cameraTransform;

    void Update()
    {
        if (!photonView.IsMine) return;

        // Only update rotation based on local player's camera
        Vector3 lookForward = cameraTransform.forward;
        lookForward.y = 0;
        lookForward.Normalize();

        if (lookForward.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(lookForward);
        }
    }
}
