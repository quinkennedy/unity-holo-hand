using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ConfigPane : MonoBehaviour {

    public List<Text> authPlaceholderText;
    public InputField ServerIP;
    public InputField HLUserField, HLPassField;
    public InputField PkgNameField, AppIdField;
    public Toggle ServerConnectedToggle;
    public static ConfigPane instance;
    public TabLogic configTab;
    public Text WarningOutput;
    private bool _needConfig = true;
    Dictionary<Warning, string> warnings;
    public bool needConfig
    {
        get
        {
            return _needConfig;
        }
    }
    public string PackageName
    {
        get
        {
            return PkgNameField.text;
        }
        set
        {
            PkgNameField.text = value;
        }
    }
    public string AppId
    {
        get
        {
            return AppIdField.text;
        }
        set
        {
            AppIdField.text = value;
        }
    }

    public enum Warning
    {
        Auth, Server
    }

	// Use this for initialization
	void Start ()
    {
        instance = this;
        warnings = new Dictionary<Warning, string>();
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
        } else
        {
            warnings.Add(Warning.Auth, "enter authication credentials");
            UpdateWarnings();
        }

        if (PlayerPrefs.HasKey("hlPkgName"))
        {
            PackageName = PlayerPrefs.GetString("hlPkgName");
        } else
        {
            PackageName = "HoloHand-Lens";
        }
        if (PlayerPrefs.HasKey("hlAppId"))
        {
            AppId = PlayerPrefs.GetString("hlAppId");
        } else
        {
            AppId = "App";
        }
	}

    private void UpdateWarnings()
    {
        string displayText = string.Empty;
        Dictionary<Warning, string>.ValueCollection values = warnings.Values;
        foreach (string value in values)
        {
            if (!string.IsNullOrEmpty(displayText))
            {
                displayText += System.Environment.NewLine;
            }
            displayText += value;
        }
        WarningOutput.text = displayText;

        configTab.WarningImage.enabled = (warnings.Count > 0);
    }

    public void OnConnectedToServer()
    {
        Debug.Log("[ConfigPane:OnConnectedToServer]");
        ServerConnectedToggle.isOn = true;
        configTab.SetConnected(true);
    }

    public void OnDisconnectedFromServer()
    {
        Debug.Log("[ConfigPane:OnDisconnectedFromServer]");
        ServerConnectedToggle.isOn = false;
        configTab.SetConnected(false);
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

    public void SavePkgAppData()
    {
        PlayerPrefs.SetString("hlPkgName", PackageName);
        PlayerPrefs.SetString("hlAppId", AppId);
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
        string auth = authenticate(HLUserField.text, HLPassField.text);
        foreach (Text pt in authPlaceholderText)
        {
            pt.text = "Loaded";
        }
        HLUserField.text = "";
        HLPassField.text = "";
        warnings.Remove(Warning.Auth);
        UpdateWarnings();

        PlayerPrefs.SetString("hlAuth", auth);
        PlayerPrefs.Save();
    }
}
