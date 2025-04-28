using UnityEngine;
using Photon.Pun;

public class PhotonPlayerSetup : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject playerModel; // Drag visible model here in the prefab inspector

    private void Start()
    {
        if (photonView.IsMine)
        {
            SetLayerRecursively(playerModel, LayerMask.NameToLayer("LocalPlayer"));
        }
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            if (child == null) continue;
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}
