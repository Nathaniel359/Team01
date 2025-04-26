using UnityEngine;
using Photon.Pun; // Add Photon namespace

public class SitTarget : MonoBehaviourPunCallbacks
{
    private GameObject player;
    private CharacterController controller;
    private CharacterMovement movementScript;

    public Transform sitPoint;

    public AudioSource audioSource;
    public AudioClip audioClip;

    private bool isSitting = false;
    private float originalHeight;
    private Vector3 originalCenter;

    private PhotonView photonView; // PhotonView of this SitTarget (optional if you want it)

    void Start()
    {
        photonView = GetComponent<PhotonView>();
    }

    public void ToggleSit(GameObject playerParam)
    {
        PhotonView playerView = playerParam.GetComponent<PhotonView>();

        if (playerView == null)
        {
            Debug.LogError("No PhotonView on player!");
            return;
        }

        // Only the local player sends the RPC
        if (playerView.IsMine)
        {
            photonView.RPC(nameof(RPC_ToggleSit), RpcTarget.All, playerView.ViewID);
        }
    }

    [PunRPC]
    void RPC_ToggleSit(int playerViewID)
    {
        PhotonView targetView = PhotonView.Find(playerViewID);

        if (targetView == null)
        {
            Debug.LogError("Couldn't find PhotonView with ID: " + playerViewID);
            return;
        }

        player = targetView.gameObject;
        controller = player.GetComponent<CharacterController>();
        movementScript = player.GetComponent<CharacterMovement>();

        if (controller == null || movementScript == null)
        {
            Debug.LogError("Missing CharacterController or CharacterMovement!");
            return;
        }

        // Store the original settings if first time sitting
        if (!isSitting)
        {
            originalHeight = controller.height;
            originalCenter = controller.center;
            Sit();
        }
        else
        {
            Stand();
        }
    }

    void Sit()
    {
        isSitting = true;

        if (audioSource != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(audioClip);
        }

        player.transform.position = sitPoint.position;

        controller.height = 0.6f;
        controller.center = new Vector3(originalCenter.x, 0.3f, originalCenter.z);
    }

    void Stand()
    {
        isSitting = false;

        controller.height = originalHeight;
        controller.center = originalCenter;
    }

    void Update()
    {
        if (isSitting && movementScript != null)
        {
            // Only the local player should check input
            PhotonView playerView = player.GetComponent<PhotonView>();
            if (playerView != null && playerView.IsMine)
            {
                float inputX = Input.GetAxis("Horizontal");
                float inputY = Input.GetAxis("Vertical");

                if (Mathf.Abs(inputX) > 0.1f || Mathf.Abs(inputY) > 0.1f)
                {
                    ToggleSit(player);
                }
            }
        }
    }
}
