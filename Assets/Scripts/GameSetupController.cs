using UnityEngine;
using Photon.Pun;
using System.IO;

public class GameSetupController : MonoBehaviour
{
    public GameObject lightMenuCanvas;
    public GameObject heavyMenuCanvas;

    private void Start()
    {
        StartCoroutine(WaitAndSpawn());
    }

    private System.Collections.IEnumerator WaitAndSpawn()
    {
        while (!PhotonNetwork.IsConnectedAndReady || PhotonNetwork.CurrentRoom == null)
            yield return null;

        Debug.Log("Joined Room. Spawning Player.");
        Vector3 spawnPosition = new Vector3(1.0f, 1.31f, -34.13f);

        PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PhotonPlayer"), spawnPosition, Quaternion.identity);
    }
}
