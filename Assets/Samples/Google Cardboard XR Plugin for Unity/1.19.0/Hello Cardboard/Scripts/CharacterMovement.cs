using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(CharacterController))]
public class CharacterMovement : MonoBehaviourPunCallbacks
{
    private CharacterController charCntrl;
    [Tooltip("The speed at which the character will move.")]
    public float speed = 5f;
    [Tooltip("The camera representing where the character is looking.")]
    public GameObject cameraObj;
    [Tooltip("Should be checked if using the Bluetooth Controller to move. If using keyboard, leave this unchecked.")]
    public bool joyStickMode;

    [Header("Footstep Settings")]
    public AudioSource footstepSource;
    public AudioClip footstepClip;

    public Transform characterMesh; // Assign in the inspector
    private Vector3 lastSentForward;


    private float stepTimer = 0f;

    void Start()
    {
        charCntrl = GetComponent<CharacterController>();

        // Disable this script and the camera for remote players
        if (!photonView.IsMine)
        {
            if (cameraObj != null && cameraObj.GetComponent<Camera>() != null)
            {
                cameraObj.GetComponent<Camera>().enabled = false;
            }
            enabled = false;
        }
    }

    void Update()
    {
        // Only allow movement for the local player
        if (!photonView.IsMine)
        {
            return;
        }

        float horComp = Input.GetAxis("Horizontal");
        float vertComp = Input.GetAxis("Vertical");

        if (joyStickMode)
        {
            horComp = Input.GetAxis("Vertical");
            vertComp = Input.GetAxis("Horizontal") * -1;
        }

        Vector3 moveVect = Vector3.zero;

        Vector3 cameraLook = cameraObj.transform.forward;
        cameraLook.y = 0f;
        cameraLook = cameraLook.normalized;

        Vector3 forwardVect = cameraLook;
        Vector3 rightVect = Vector3.Cross(forwardVect, Vector3.up).normalized * -1;

        moveVect += rightVect * horComp;
        moveVect += forwardVect * vertComp;

        moveVect *= speed;

        charCntrl.SimpleMove(moveVect);

        // Footstep Sound Logic (only for the local player)
        if (charCntrl.velocity.magnitude > 0.1f && charCntrl.isGrounded)
        {
            float adjustedInterval = Mathf.Clamp(3f / speed, 0.2f, 1f);

            stepTimer += Time.deltaTime;
            if (stepTimer >= adjustedInterval)
            {
                PlayFootstep();
                stepTimer = 0f;
            }
        }
        else
        {
            stepTimer = 0f;
        }

        if (photonView.IsMine)
        {
            Vector3 forwardDir = cameraObj.transform.forward;
            forwardDir.y = 0;
            forwardDir.Normalize();

            if (Vector3.Distance(forwardDir, lastSentForward) > 0.05f)
            {
                photonView.RPC("UpdateFacing", RpcTarget.Others, forwardDir);
                lastSentForward = forwardDir;
            }
        }

    }

    [PunRPC]
    public void UpdateFacing(Vector3 forward)
    {
        if (characterMesh == null) return;

        forward.y = 0;
        if (forward.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(forward);
            characterMesh.rotation = Quaternion.Slerp(characterMesh.rotation, targetRot, 0.2f); // Smooth
        }
    }


    void PlayFootstep()
    {
        if (footstepSource != null && footstepClip != null && photonView.IsMine)
        {
            footstepSource.pitch = Random.Range(0.5f, 1.5f);
            footstepSource.PlayOneShot(footstepClip);
        }
    }
}