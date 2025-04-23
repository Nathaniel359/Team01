using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;

public class NetworkController : MonoBehaviourPunCallbacks
{
    private void Start()
    {
        PhotonNetwork.ConnectUsingSettings(); //Connects to Photon Master server
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("We are now connected to the " + PhotonNetwork.CloudRegion + " server!");
    }
}
