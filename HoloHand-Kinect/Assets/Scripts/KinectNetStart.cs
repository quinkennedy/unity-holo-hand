using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class KinectNetStart : INetStartLogic {

    private bool _triedOnce = false;

    public override void StartNetwork()
    {
        _triedOnce = true;
        NetworkManager.singleton.serverBindToIP = false;
        NetworkManager.singleton.StartHost();
    }

    public override bool ShouldStartNetwork()
    {
        //if we have already tried, then some error is probably preventing the host from starting
        // and the user should use the UI to start the network
        return !_triedOnce;
    }
}
