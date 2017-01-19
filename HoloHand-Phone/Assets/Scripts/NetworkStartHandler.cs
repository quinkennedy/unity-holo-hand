using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.VR;

public class NetworkStartHandler : MonoBehaviour {

    #region Inspector properties
    
    public GameObject hololensPrefab = null;
    public GameObject kinectPrefab = null;
    public GameObject mobilePrefab = null;

    #endregion

    private INetStartLogic startLogic;
    private bool _triedOnce = false;
    private bool _needCam = true;

    // Use this for initialization
    void Start ()
    {
        Debug.Log("[NetworkStartHandler:Start]");
        RegisterSpawnablePrefabs();
        RegisterPlayer();
        InstantiateStartLogic();
        startLogic.LoadConfig();
    }

    private void RegisterSpawnablePrefabs()
    {
        NetworkManager.singleton.spawnPrefabs.Add(hololensPrefab);
        NetworkManager.singleton.spawnPrefabs.Add(kinectPrefab);
        NetworkManager.singleton.spawnPrefabs.Add(mobilePrefab);
    }

    private void RegisterPlayer()
    {
#if UNITY_WSA_10_0
        NetworkManager.singleton.playerPrefab = hololensPrefab;
#elif UNITY_ANDROID || DOCENT_UI
        NetworkManager.singleton.playerPrefab = mobilePrefab;
#elif UNITY_STANDALONE
        NetworkManager.singleton.playerPrefab = kinectPrefab;
#endif
    }

    private void InstantiateStartLogic()
    {
#if UNITY_WSA_10_0
        startLogic = new HololensNetStart();
#elif UNITY_ANDROID || DOCENT_UI
        startLogic = new DocentNetStart();
#elif UNITY_STANDALONE
        startLogic = new KinectNetStart();
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
        if (!NetworkManager.singleton.isNetworkActive &&
            startLogic.ShouldStartNetwork())
        {
            startLogic.StartNetwork();
        }
        /*
        //if we have a main camera, and no active network,
        //  then let's start up the network!
        // - we have to wait for a main camera so the avatar will get placed correctly.
        if (!_triedOnce &&
            NetworkManager.singleton != null && 
            !NetworkManager.singleton.isNetworkActive && 
            (!_needCam || Camera.main != null) &&
            (!_needConfig || HololensConfig.instance != null))
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
            if (_needConfig)
            {
                NetworkManager.singleton.networkAddress = HololensConfig.instance.server;
            }
            else
            {
                NetworkManager.singleton.networkAddress = "172.16.0.130";
            }
            NetworkManager.singleton.StartClient();
#endif

            if (_saveConfig)
            {
                HololensConfig.instance.SaveToUnityStorage();
                _saveConfig = false;
            }
        }
        */
    }

}
