using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ConfigPane : MonoBehaviour {

	// Use this for initialization
	void Start ()
    {
        InputField ip = getInputFieldChild("ServerIP");
        bool setIP = false;
        if (PlayerPrefs.HasKey("serverIP"))
        {
            string savedIP = PlayerPrefs.GetString("serverIP");
            if (savedIP != null && savedIP.Length > 0)
            {
                ip.text = savedIP;
                setIP = true;
            }
        }
        if (!setIP)
        {
            ip.text = NetworkManager.singleton.networkAddress;
        }

        if (PlayerPrefs.HasKey("hlAuth"))
        {
            getInputFieldChild("HL User").transform.Find("Placeholder").GetComponent<Text>().text = "Loaded";
            getInputFieldChild("HL Pass").transform.Find("Placeholder").GetComponent<Text>().text = "Loaded";
        }
	}

    private InputField getInputFieldChild(string name)
    {
        return transform.Find("Pane").Find(name).Find("InputField").GetComponent<InputField>();
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
        auth = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(auth));
        auth = "Basic " + auth;
        return auth;
    }

    public void SetHololensLogin()
    {
        Debug.Log("[ConfigPane.SetHololensLogin] set login");
        InputField userField = getInputFieldChild("HL User");
        InputField passField = getInputFieldChild("HL Pass");
        string auth = authenticate(userField.text, passField.text);
        userField.text = "";
        passField.text = "";

        PlayerPrefs.SetString("hlAuth", auth);
        PlayerPrefs.Save();
    }
}
