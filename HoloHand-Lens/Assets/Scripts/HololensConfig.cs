using UnityEngine;

[System.Serializable]
public class HololensConfig
{
    public string server;
    public string id;

    public static HololensConfig instance;

    public override string ToString()
    {
        return "Server: " + server + " ID:" + id;
    }

    public static HololensConfig CreateFromJSON(string jsonString)
    {
        HololensConfig.instance = JsonUtility.FromJson<HololensConfig>(jsonString);
        return HololensConfig.instance;
    }

    public static HololensConfig CreateFromUnityStorage()
    {
        if (!PlayerPrefs.HasKey("ConfigId") || !PlayerPrefs.HasKey("ConfigServer"))
        {
            return null;
        }
        else
        {
            HololensConfig config = new global::HololensConfig();
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