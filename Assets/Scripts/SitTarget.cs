using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

public class SitTarget : MonoBehaviourPunCallbacks
{
    // List of sit points for multiple players
    public List<Transform> sitPoints = new List<Transform>();

    // Audio components
    public AudioSource audioSource;
    public AudioClip audioClip;

    // Dictionary to track which player occupies which sit point (synced via network)
    private Dictionary<int, int> occupiedSitPoints = new Dictionary<int, int>(); // Key: PhotonView ID, Value: sitPoint index

    // Structure to store player-specific data
    private class PlayerData
    {
        public GameObject player;
        public CharacterController controller;
        public CharacterMovement movementScript;
        public float originalHeight;
        public Vector3 originalCenter;
        public bool isSitting;
    }

    // Dictionary to store player data locally
    private Dictionary<int, PlayerData> playerData = new Dictionary<int, PlayerData>();

    private PhotonView photonView;

    void Start()
    {
        photonView = GetComponent<PhotonView>();
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
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
            // Check if the player is already sitting
            if (occupiedSitPoints.ContainsKey(playerView.ViewID))
            {
                // Request to stand
                photonView.RPC(nameof(RPC_ToggleSit), RpcTarget.All, playerView.ViewID, -1);
            }
            else
            {
                // Find an available sit point
                int sitPointIndex = GetAvailableSitPointIndex();
                if (sitPointIndex == -1)
                {
                    Debug.Log("No available sit points.");
                    return;
                }

                // Request to sit at the sit point
                photonView.RPC(nameof(RPC_ToggleSit), RpcTarget.All, playerView.ViewID, sitPointIndex);
            }
        }
    }

    [PunRPC]
    void RPC_ToggleSit(int playerViewID, int sitPointIndex)
    {
        PhotonView targetView = PhotonView.Find(playerViewID);

        if (targetView == null)
        {
            Debug.LogError("Couldn't find PhotonView with ID: " + playerViewID);
            return;
        }

        GameObject player = targetView.gameObject;
        CharacterController controller = player.GetComponent<CharacterController>();
        CharacterMovement movementScript = player.GetComponent<CharacterMovement>();

        if (controller == null || movementScript == null)
        {
            Debug.LogError("Missing CharacterController or CharacterMovement!");
            return;
        }

        // If sitPointIndex == -1, the player is standing up
        if (sitPointIndex == -1)
        {
            Stand(playerViewID);
            return;
        }

        // Initialize or update player data
        if (!playerData.ContainsKey(playerViewID))
        {
            playerData[playerViewID] = new PlayerData
            {
                player = player,
                controller = controller,
                movementScript = movementScript,
                originalHeight = controller.height,
                originalCenter = controller.center,
                isSitting = false
            };
        }

        PlayerData data = playerData[playerViewID];

        // If already sitting, do nothing (safety check)
        if (data.isSitting)
        {
            return;
        }

        // Update sit point occupancy
        occupiedSitPoints[playerViewID] = sitPointIndex;

        // Sit the player
        Sit(playerViewID, sitPointIndex);
    }

    void Sit(int playerViewID, int sitPointIndex)
    {
        if (!playerData.ContainsKey(playerViewID) || sitPointIndex >= sitPoints.Count)
        {
            return;
        }

        PlayerData data = playerData[playerViewID];
        data.isSitting = true;

        // Play audio
        if (PhotonNetwork.IsMasterClient && audioSource != null && audioClip != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(audioClip);
        }

        // Snap to sit position
        data.player.transform.position = sitPoints[sitPointIndex].position;

        // Adjust character controller for sitting
        data.controller.height = 0.4f;
        data.controller.center = new Vector3(data.originalCenter.x, 0.2f, data.originalCenter.z);
    }

    void Stand(int playerViewID)
    {
        if (!playerData.ContainsKey(playerViewID))
        {
            return;
        }

        PlayerData data = playerData[playerViewID];
        data.isSitting = false;

        // Restore original height and center
        data.controller.height = data.originalHeight;
        data.controller.center = data.originalCenter;

        // Clear sit point occupancy
        occupiedSitPoints.Remove(playerViewID);
    }

    void Update()
    {
        // Check for input to stand (only for local players)
        List<int> playersToStand = new List<int>();

        foreach (var playerViewID in playerData.Keys)
        {
            PlayerData data = playerData[playerViewID];
            if (data.isSitting && data.movementScript != null)
            {
                PhotonView playerView = data.player.GetComponent<PhotonView>();
                if (playerView != null && playerView.IsMine)
                {
                    float inputX = Input.GetAxis("Horizontal");
                    float inputY = Input.GetAxis("Vertical");

                    if (Mathf.Abs(inputX) > 0.1f || Mathf.Abs(inputY) > 0.1f)
                    {
                        playersToStand.Add(playerViewID);
                    }
                }
            }
        }

        // Send stand requests for local players
        foreach (var playerViewID in playersToStand)
        {
            PhotonView playerView = PhotonView.Find(playerViewID);
            if (playerView != null && playerView.IsMine)
            {
                ToggleSit(playerView.gameObject);
            }
        }
    }

    private int GetAvailableSitPointIndex()
    {
        for (int i = 0; i < sitPoints.Count; i++)
        {
            if (!occupiedSitPoints.ContainsValue(i))
            {
                return i;
            }
        }
        return -1;
    }

    // Clean up when a player disconnects
    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        // Find the PhotonView ID associated with the player who left
        foreach (var playerViewID in playerData.Keys)
        {
            PhotonView view = PhotonView.Find(playerViewID);
            if (view == null || view.Owner == null || view.Owner == otherPlayer)
            {
                Stand(playerViewID);
                playerData.Remove(playerViewID);
                occupiedSitPoints.Remove(playerViewID);
                break;
            }
        }
    }
}