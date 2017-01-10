using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.VR;

#if UNITY_EDITOR && UNITY_WSA_10_0
using UnityEditor.Callbacks;
using UnityEditor;
#endif

#if UNITY_WSA_10_0 && !UNITY_EDITOR
using System.Threading.Tasks;
#endif


public class NetworkStartLogic : MonoBehaviour {

    #region Inspector properties
    
    public GameObject hololensPrefab = null;
    public GameObject kinectPrefab = null;
    public GameObject mobilePrefab = null;

    #endregion

    private bool _triedOnce = false;
    private bool _needCam = true;
    private bool _needConfig = false;
    private bool _saveConfig = false;
    private static string configName = "config.json";

    // Use this for initialization
    void Start () {
        Debug.Log("[NetworkStartLogic:Start]");
#if UNITY_WSA_10_0
        //If on Hololens
        Debug.Log("[NetworkStartLogic:Start] running on Hololens");
        if (hololensPrefab != null){
            NetworkManager.singleton.playerPrefab = hololensPrefab;
        }
        _needConfig = true;
        LoadConfig(configName, Config.CreateFromUnityStorage());
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
            (!_needCam || Camera.main != null) &&
            (!_needConfig || Config.instance != null))
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
                NetworkManager.singleton.networkAddress = Config.instance.server;
            }
            else
            {
                NetworkManager.singleton.networkAddress = "172.16.0.130";
            }
            NetworkManager.singleton.StartClient();
#endif

            if (_saveConfig)
            {
                Config.instance.SaveToUnityStorage();
                _saveConfig = false;
            }
        }
    }

    void ParseConfig(string json)
    {
        Config.CreateFromJSON(json);
        Debug.Log("[NetworkStartLogic:ParseConfig] Config " + Config.instance);
    }

#if UNITY_EDITOR

    public void LoadConfig( string jsonFile, Config storedConfig)
    {
        string configJson = File.ReadAllText(jsonFile);
        ParseConfig(configJson);
    }
#if UNITY_WSA_10_0
/*    [PostProcessBuild]
    public static void copyConfigToWSAAssets(BuildTarget target, string pathToBuiltProject)
    {
        Debug.Log("[NetworkStartLogic:copyConfigToWSAAssets]");
        string destination = pathToBuiltProject + "\\HoloHand-Lens\\Assets\\" + configName;
        FileUtil.DeleteFileOrDirectory(destination);
        FileUtil.CopyFileOrDirectory(configName, destination);
    }
    */
#endif

#elif UNITY_WSA_10_0

    public async void LoadConfig(string jsonFile, Config storedConfig)
    {
        //first choice is load from user-uploaded file
        //second choice is load from Unity storage
        //final choice is load from json included with installation package
        string json = await this.configJson(jsonFile, storedConfig == null);
        if (json == null || json.Length == 0){
            if (storedConfig != null)
            {
                Debug.Log("[NetworkStartLogic:LoadConfig] using config from Unity storage");
                Config.instance = storedConfig;
            } else
            {
                Debug.LogWarning("[NetworkStartLogic:LoadConfig] failed to load any config");
            }
        } else {
            ParseConfig(json);
            _saveConfig = true;
        }
    }

    public async Task<string> configJson(string jsonFile, bool useBackup)
    {
        //try to load file from a user-accessible location
        //store config.json in LocalState oF the deployed app: http://127.0.0.1:10080/FileExplorer.htm
        Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
        Windows.Storage.StorageFile configFile = null;
        try
        {
            configFile = await storageFolder.GetFileAsync(jsonFile);
        }
        catch (FileNotFoundException){}

        if (configFile != null){
            Debug.Log("[NetworkStartLogic:configJson] loading config from AppData");
        } else if (useBackup) {
            //fall back to a built-in config
            storageFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            try
            {
                Windows.Storage.StorageFolder assetsFolder = await storageFolder.GetFolderAsync("assets");
                configFile = await assetsFolder.GetFileAsync(jsonFile);
            } catch (FileNotFoundException) { 
                return null;
            }

            if (configFile != null){
                Debug.Log("[NetworkStartLogic:configJson] loading config from installation directory");
            }
        }
        
        if (configFile != null)
        {
            try
            {
                var buffer = await Windows.Storage.FileIO.ReadBufferAsync(configFile);

                using (var dataReader = Windows.Storage.Streams.DataReader.FromBuffer(buffer))
                {
                    string text = dataReader.ReadString(buffer.Length);
                    Debug.Log("[NetworkStartLogic:configJson] config.json reads: " + text);
                    return text;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[NetworkStartLogic:configJson] Error reading config.json");
                return "";
            }
        } else
        {
            return null;
        }
        
    }



#endif

}
