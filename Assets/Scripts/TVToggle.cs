using Photon.Pun;
using UnityEngine;
using UnityEngine.Video;

// Handles TV
public class TVToggle : MonoBehaviour
{
    public Renderer screenRenderer;
    public Material tvOnMaterial;
    public Material tvOffMaterial;
    public VideoPlayer videoPlayer;
    private PhotonView photonView;
    private bool isOn = false;

    void Start()
    {
        photonView = GetComponent<PhotonView>();
    }

    public void ToggleTV()
    {
        photonView.RPC(nameof(RPC_ToggleTV), RpcTarget.All);
    }

    [PunRPC]
    public void RPC_ToggleTV()
    {
        isOn = !isOn;

        if (screenRenderer != null)
        {
            screenRenderer.material = isOn ? tvOnMaterial : tvOffMaterial;
        }

        // Video playback
        if (videoPlayer != null)
        {
            if (isOn) videoPlayer.Play();
            else videoPlayer.Pause();
        }
    }
}
