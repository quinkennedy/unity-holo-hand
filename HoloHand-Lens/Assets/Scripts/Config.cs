using UnityEngine;

[System.Serializable]
public class Config
{
    public string server;
    public string id;

    public static Config instance;

    public override string ToString()
    {
        return "Server: " + server + " ID:" + id;
    }

    public static Config CreateFromJSON(string jsonString)
    {
        Config.instance = JsonUtility.FromJson<Config>(jsonString);
        return Config.instance;
    }

    public static Config CreateFromUnityStorage()
    {
        if (!PlayerPrefs.HasKey("ConfigId") || !PlayerPrefs.HasKey("ConfigServer"))
        {
            return null;
        }
        else
        {
            Config config = new global::Config();
            config.id = PlayerPrefs.GetString("ConfigId");
            config.server = PlayerPrefs.GetString("ConfigServer");
            Debug.Log("[Config:CreateFromUnityStorage] id: " + config.id + " server: " + config.server);
            return config;
        }
    }
    
    public void SaveToUnityStorage()
    {
        Debug.Log("[Config:SaveToUnityStorage]");
        PlayerPrefs.SetString("ConfigId", id);
        PlayerPrefs.SetString("ConfigServer", server);
        PlayerPrefs.Save();
    }

    // Given JSON input:
    // {"server":"127.0.0.1", "id":"one"}
    // this example will return a PlayerInfo object with
    // server == "127.0.0.1", id == "one"

}