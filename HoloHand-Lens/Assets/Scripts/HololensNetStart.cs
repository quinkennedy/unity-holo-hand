using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;

#if UNITY_EDITOR && UNITY_WSA_10_0
using UnityEditor.Callbacks;
using UnityEditor;
#endif

#if UNITY_WSA_10_0 && !UNITY_EDITOR
using System.Threading.Tasks;
#endif

public class HololensNetStart : INetStartLogic
{
    private bool _saveConfig = false;
    private bool _loadingConfig = false;
    private static string configName = "config.json";

    public override void LoadConfig()
    {
        //LoadConfig(configName, HololensConfig.CreateFromUnityStorage());
    }

    public override bool ShouldStartNetwork()
    {
        if (_saveConfig)
        {
            HololensConfig.instance.SaveToUnityStorage();
            _saveConfig = false;
        }
        if (HololensConfig.instance != null)
        {
#if UNITY_EDITOR
            return false;
#else
            return true;
#endif
        }
        else
        {
            if (!_loadingConfig)
            {
                LoadConfig(configName, HololensConfig.CreateFromUnityStorage());
            }
            return false;
        }
        //return (HololensConfig.instance != null);
    }

    public override void StartNetwork()
    {
        NetworkManager.singleton.networkAddress = HololensConfig.instance.server;
        NetworkManager.singleton.StartClient();
    }

    /**********************
     * Config Loading Mess
     **********************/

    void ParseConfig(string json)
    {
        HololensConfig.CreateFromJSON(json);
        Debug.Log("[NetworkStartLogic:ParseConfig] Config " + HololensConfig.instance);
        _loadingConfig = false;
    }

#if UNITY_EDITOR

    public void LoadConfig(string jsonFile, HololensConfig storedConfig)
    {
        _loadingConfig = true;
        string configJson = File.ReadAllText(jsonFile);
        ParseConfig(configJson);
    }
#if UNITY_WSA_10_0
    [PostProcessBuild]
    public static void copyConfigToWSAAssets(BuildTarget target, string pathToBuiltProject)
    {
        Debug.Log("[NetworkStartLogic:copyConfigToWSAAssets]");
        string destination = pathToBuiltProject + "\\HoloHand-Lens\\Assets\\" + configName;
        FileUtil.DeleteFileOrDirectory(destination);
        FileUtil.CopyFileOrDirectory(configName, destination);
    }
        
#endif

#elif UNITY_WSA_10_0

    public async void LoadConfig(string jsonFile, HololensConfig storedConfig)
    {
        _loadingConfig = true;
        //first choice is load from user-uploaded file
        //second choice is load from Unity storage
        //final choice is load from json included with installation package
        string json = await this.configJson(jsonFile, storedConfig == null);
        if (json == null || json.Length == 0){
            if (storedConfig != null)
            {
                Debug.Log("[NetworkStartLogic:LoadConfig] using config from Unity storage");
                HololensConfig.instance = storedConfig;
            } else
            {
                Debug.LogWarning("[NetworkStartLogic:LoadConfig] failed to load any config");
                HololensConfig.instance = new HololensConfig();
                HololensConfig.instance.server = "172.16.0.130";
                HololensConfig.instance.id = "DE4D B3A7";
                IReadOnlyList<Windows.Networking.HostName> hostNames = Windows.Networking.Connectivity.NetworkInformation.GetHostNames();
                foreach(Windows.Networking.HostName hostName in hostNames)
                {
                    if (hostName.Type == Windows.Networking.HostNameType.DomainName)
                    {
                        HololensConfig.instance.id = hostName.RawName;
                        break;
                    }
                }
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
