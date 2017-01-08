using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.VR;

public class StartLogic : MonoBehaviour {

    #region Inspector properties

    public NetworkManagerHUD netMgrHUD = null;
    public GameObject hololensPrefab = null;
    public GameObject kinectPrefab = null;

    #endregion

    private bool _triedOnce = false;

    // Use this for initialization
    void Start () {
        Debug.Log("[StartLogic:Start]");
#if UNITY_WSA_10_0
        //If on Hololens
        Debug.Log("[StartLogic:Start] running on Hololens");
        if (hololensPrefab != null){
            netMgrHUD.manager.playerPrefab = hololensPrefab;
        }
#else
        //on PC
        Debug.Log("[StartLogic:Start] running on PC");
        if (kinectPrefab != null)
        {
            netMgrHUD.manager.playerPrefab = kinectPrefab;
        }
#endif
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        Debug.Log("[StartLogic:OnApplicationPause] - " + pauseStatus);
        _triedOnce = false;
    }

    // Update is called once per frame
    void Update ()
    {
        //if we have a main camera, and no active network,
        //  then let's start up the network!
        // - we have to wait for a main camera so the avatar will get placed correctly.
        if (netMgrHUD != null && !netMgrHUD.manager.isNetworkActive && Camera.main != null && !_triedOnce)
        {
            _triedOnce = true;
            Debug.Log("[StartLogic:Update] starting up network");
#if !UNITY_WSA_10_0
            //aka if HoloLens
            netMgrHUD.manager.serverBindToIP = false;
            netMgrHUD.manager.StartHost();
#else
            //otherwise standalone or in Editor

            //if we have already tried connecting to the server once, don't try again.
            //  the user can use the HUD to set the correct IP and attempt connecting again.
            netMgrHUD.manager.networkAddress = "192.168.1.122";
            netMgrHUD.manager.StartClient();
#endif
        }
    }
}
