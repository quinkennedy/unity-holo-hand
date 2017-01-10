using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.VR;

public class NetworkStartLogic : MonoBehaviour {

    #region Inspector properties
    
    public GameObject hololensPrefab = null;
    public GameObject kinectPrefab = null;
    public GameObject mobilePrefab = null;

    #endregion

    private bool _triedOnce = false;
    private bool _needCam = true;

    // Use this for initialization
    void Start () {
        Debug.Log("[NetworkStartLogic:Start]");
#if UNITY_WSA_10_0
        //If on Hololens
        Debug.Log("[NetworkStartLogic:Start] running on Hololens");
        if (hololensPrefab != null){
            NetworkManager.singleton.playerPrefab = hololensPrefab;
        }
#elif UNITY_ANDROID
        Debug.Log("[NetworkStartLogic:Start] running on Android");
        NetworkManager.singleton.playerPrefab = mobilePrefab;
        //The android app doesn't use a camera
        _needCam = false;
#else
        //on PC
        Debug.Log("[NetworkStartLogic:Start] running on PC");
        if (kinectPrefab != null)
        {
            NetworkManager.singleton.playerPrefab = kinectPrefab;
        }
#endif
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        Debug.Log("[NetworkStartLogic:OnApplicationPause] - " + pauseStatus);
        _triedOnce = false;
    }

    // Update is called once per frame
    void Update ()
    {
        //if we have a main camera, and no active network,
        //  then let's start up the network!
        // - we have to wait for a main camera so the avatar will get placed correctly.
        if (!_triedOnce &&
            NetworkManager.singleton != null && 
            !NetworkManager.singleton.isNetworkActive && 
            (!_needCam || Camera.main != null))
        {
            _triedOnce = true;
            Debug.Log("[NetworkStartLogic:Update] starting up network");
#if UNITY_STANDALONE
            //aka if on PC
            Debug.Log("[NetworkStartLogic:Update] starting network host");
            NetworkManager.singleton.serverBindToIP = false;
            NetworkManager.singleton.StartHost();
#else
            Debug.Log("[NetworkStartLogic:Update] attempting to connect");
            //otherwise on a HoloLens or mobile device

            //if we have already tried connecting to the server once, don't try again.
            //  the user can use the HUD to set the correct IP and attempt connecting again.
            NetworkManager.singleton.networkAddress = "172.16.0.130";
            NetworkManager.singleton.StartClient();
#endif
        }
    }
}
