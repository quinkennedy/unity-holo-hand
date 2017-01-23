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
    }

    // Update is called once per frame
    void Update ()
    {
        if (!NetworkManager.singleton.isNetworkActive &&
            startLogic.ShouldStartNetwork())
        {
            startLogic.StartNetwork();
        }
    }

}
