using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ConfigPane : MonoBehaviour {

    public List<Text> authPlaceholderText;
    public InputField ServerIP;
    public InputField HLUser, HLPass;
    public Toggle ServerConnectedToggle;
    public static ConfigPane instance;
    private bool _needConfig = true;
    public bool needConfig
    {
        get
        {
            return _needConfig;
        }
    }

	// Use this for initialization
	void Start ()
    {
        instance = this;
        bool setIP = false;
        if (PlayerPrefs.HasKey("serverIP"))
        {
            string savedIP = PlayerPrefs.GetString("serverIP");
            if (savedIP != null && savedIP.Length > 0)
            {
                ServerIP.text = savedIP;
                setIP = true;
            }
        }
        if (!setIP)
        {
            ServerIP.text = NetworkManager.singleton.networkAddress;
        }

        if (PlayerPrefs.HasKey("hlAuth"))
        {
            foreach(Text pt in authPlaceholderText)
            {
                pt.text = "Loaded";
            }
            //and bring the overview to front
            GetComponent<RectTransform>().SetAsFirstSibling();
        }
	}

    public void OnConnectedToServer()
    {
        Debug.Log("[ConfigPane:OnConnectedToServer]");
        ServerConnectedToggle.isOn = true;
    }

    public void OnDisconnectedFromServer()
    {
        Debug.Log("[ConfigPane:OnDisconnectedFromServer]");
        ServerConnectedToggle.isOn = false;
    }

    public void OnDisconnectedFromServer(NetworkDisconnection info)
    {
        OnDisconnectedFromServer();
    }

    // Update is called once per frame
    void Update () {
	}

    public void SetServerIP(string ip)
    {
        Debug.Log("[ConfigPane.SetServerIP] set server IP to " + ip);
        NetworkManager.singleton.networkAddress = ip;
        PlayerPrefs.SetString("serverIP", ip);
        PlayerPrefs.Save();
    }

    private static string authenticate(string username, string password)
    {
        string auth = username + ":" + password;
        auth = System.Convert.ToBase64String(
            System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(auth));
        auth = "Basic " + auth;
        return auth;
    }

    public void SetHololensLogin()
    {
        Debug.Log("[ConfigPane.SetHololensLogin] set login");
        string auth = authenticate(HLUser.text, HLPass.text);
        HLUser.text = "";
        HLPass.text = "";

        PlayerPrefs.SetString("hlAuth", auth);
        PlayerPrefs.Save();
    }
}
