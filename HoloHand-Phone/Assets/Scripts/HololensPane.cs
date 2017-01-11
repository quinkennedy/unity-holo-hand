using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class HololensPane : MonoBehaviour {

    public InputField IPField;
    public TabLogic tab;
    private HololensAvatarLogic linkedHololens;
    public static string HololensAppId = "App";
    public static string HololensPackageName = "HoloHand-Lens";
    
    public string ID
    {
        get
        {
            if (tab == null)
            {
                return null;
            } else
            {
                return tab.Title;
            }
        }
        set
        {
            if (tab != null)
            {
                tab.Title = value;
            }
        }
    }
    
    public string IP
    {
        get
        {
            return IPField.text;
        }
        private set
        {
            IPField.text = value;
        }
    }

	// Use this for initialization
	void Start ()
    {
        InvokeRepeating("queryStatus", 10, 60);
	}

    public void DeleteClicked()
    {
        HololensTabWrangler.Instance.deleteTab(this);
    }

    public static void DeleteData(int index)
    {
        Debug.Log("[HololensPane:DeleteData] deleting #" + index);
        PlayerPrefs.DeleteKey("hl" + index + "IP");
        PlayerPrefs.DeleteKey("hl" + index + "ID");
    }

    public void LoadData(int index)
    {
        string ip =  PlayerPrefs.GetString("hl" + index + "IP");
        string id = PlayerPrefs.GetString("hl" + index + "ID");
        Debug.Log("[HololensPane:LoadData] loaded #" + index + " " + id + "@" + ip);
        IP = ip;
        ID = id;
    }

    public void SaveData(int index)
    {
        Debug.Log("[HololensPane:SaveData] saving #" + index + " " + ID + "@" + IP);
        PlayerPrefs.SetString("hl" + index + "IP", IP);
        PlayerPrefs.SetString("hl" + index + "ID", ID);
    }

    private void setConnectedToServer(bool connected)
    {
        Toggle toggle = transform.Find("Pane").Find("Connected To Server").Find("Value").GetComponent<Toggle>();
        toggle.isOn = connected;
        //TODO: icon in tab?
    }

    public void linkHololens(HololensAvatarLogic hololens)
    {
        IP = hololens.IP;
        linkedHololens = hololens;
        setConnectedToServer(true);
        linkedHololens.OnDestroyListeners.Add(HololensDisconnected);
    }

    public void HololensDisconnected()
    {
        linkedHololens = null;
        setConnectedToServer(false);
    }

    public bool hasLinkedHololens()
    {
        return (linkedHololens != null);
    }

    private void setBatteryLevel(BatteryResponse data)
    {
        Slider slider = transform.Find("Pane").Find("Battery Level").Find("Value").GetComponent<Slider>();
        slider.value = data.GetRemainingCharge();
        //TODO: warning on tab if too low
    }

    public void queryStatus()
    {
        if (PlayerPrefs.HasKey("hlAuth"))
        {
            String auth = getAuth();
            StartCoroutine(coGetBatteryCharge(auth));
            StartCoroutine(coGetRunningProcesses(auth));
            StartCoroutine(coGetThermalState(auth));
        }
        else
        {
            Debug.Log("[HololensPane:queryStatus] auth not set");
        }
    }

    public void RestartDevice()
    {
        String auth = getAuth();
        StartCoroutine(coPostWithoutResponse("/api/control/restart", auth));
    }

    public void ShutdownDevice()
    {
        String auth = getAuth();
        StartCoroutine(coPostWithoutResponse("/api/control/shutdown", auth));
    }

    public static string Base64Encode(string s)
    {
        return System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(s));
    }

    public static string Hex64Encode(string s)
    {
        return Base64Encode(s);
    }

    private static string getAuth()
    {
        return PlayerPrefs.GetString("hlAuth");
    }

    public void RestartApp()
    {
        string auth = getAuth();
        string appID = Hex64Encode(HololensAppId);
        string packageName = Hex64Encode(HololensPackageName);
        StartCoroutine(coRestartApp(auth, appID, packageName));
    }

    public void ShutdownApp()
    {
        string auth = getAuth();
        string packageName = Hex64Encode(HololensPackageName);
        StartCoroutine(coShutdownApp(auth, packageName));
    }

    public IEnumerator coRestartApp(string auth, string encodedAppId, string encodedPackageName)
    {
        yield return coShutdownApp(auth, encodedPackageName));
        yield return coStartApp(auth, encodedAppId, encodedPackageName);
    }

    public IEnumerator coShutdownApp(string auth, string encodedPackageName)
    {
        string endpoint = "/api/taskmanager/app";
        string Address = "http://" + IP + endpoint;
        //shutdown application
        string query = "?package=" + encodedPackageName;//optional 'forcestop=yes' possible
        return coSendWithoutResponse(UnityWebRequest.Delete(Address + query), auth);
    }

    public IEnumerator coStartApp(string auth, string encodedAppId, string encodedPackageName)
    {
        string endpoint = "/api/taskmanager/app";
        string Address = "http://" + IP + endpoint;
        //start application
        string query = "?appid=" + encodedAppId + "&package=" + encodedPackageName;
        return coPostWithoutResponse(Address + query, auth);
    }

    public IEnumerator coSendWithoutResponse(UnityWebRequest req, string auth)
    {
        req.SetRequestHeader("AUTHORIZATION", auth);
        Debug.Log("[HololensPane:coSendWithoutResponse] request to " + req.url);
        yield return req.Send();
        while (!req.downloadHandler.isDone)
        {
            yield return new WaitForEndOfFrame();
        }

        if (req.isError)
        {
            Debug.LogWarning("[HololensPane:coSendWithoutResponse] error: " + req.error);
        }
        else
        {
            Debug.Log("[HololensPane:coSendWithoutResponse] success: " + req.downloadHandler.text);
        }
    }

    public IEnumerator coPostWithoutResponse(string endpoint, string auth)
    {
        UnityEngine.Networking.UnityWebRequest req = UnityEngine.Networking.UnityWebRequest.Post("http://" + IP + endpoint, "");
        return coSendWithoutResponse(req, auth);
        /*req.SetRequestHeader("AUTHORIZATION", auth);
        Debug.Log("[HololensPane:coPostWithoutResponse] request to " + req.url);
        yield return req.Send();
        while (!req.downloadHandler.isDone)
        {
            yield return new WaitForEndOfFrame();
        }

        if (req.isError)
        {
            Debug.LogWarning("[HololensPane:coPostWithoutResponse] error: " + req.error);
        }
        else
        {
            Debug.Log("[HololensPane:coPostWithoutResponse] success: " + req.downloadHandler.text);
        }*/
    }

    public IEnumerator coGetThermalState(string auth)
    {
        UnityEngine.Networking.UnityWebRequest req = UnityEngine.Networking.UnityWebRequest.Get("http://" + IP + "/api/holographic");
        req.SetRequestHeader("AUTHORIZATION", auth);
        Debug.Log("[HololensPane:coGetThermalState] request to " + req.url);
        yield return req.Send();
        while (!req.downloadHandler.isDone)
        {
            yield return new WaitForEndOfFrame();
        }

        if (req.isError)
        {
            Debug.LogWarning("[HololensPane:coGetThermalState] error: " + req.error);
        }
        else
        {
            Debug.Log("[HololensPane:coGetThermalState] success: " + req.downloadHandler.text);
        }
    }
    
    public IEnumerator coGetRunningProcesses(string auth)
    {
        UnityEngine.Networking.UnityWebRequest req = UnityEngine.Networking.UnityWebRequest.Get("http://" + IP + "/api/resourcemanager/processes");
        req.SetRequestHeader("AUTHORIZATION", auth);
        Debug.Log("[HololensPane:coGetRunningProcesses] request to " + req.url);
        yield return req.Send();
        while (!req.downloadHandler.isDone)
        {
            yield return new WaitForEndOfFrame();
        }

        if (req.isError)
        {
            Debug.LogWarning("[HololensPane:coGetRunningProcesses] error: " + req.error);
        }
        else
        {
            Debug.Log("[HololensPane:coGetRunningProcesses] success: " + req.downloadHandler.text);
        }
    }

    public IEnumerator coGetBatteryCharge(string auth)
    {
        UnityEngine.Networking.UnityWebRequest req = UnityEngine.Networking.UnityWebRequest.Get("http://" + IP + "/api/power/battery");
        req.SetRequestHeader("AUTHORIZATION", auth);
        Debug.Log("[HololensPane:coGetBatteryCharge] request to " + req.url);
        yield return req.Send();
        while (!req.downloadHandler.isDone)
        {
            yield return new WaitForEndOfFrame();
        }

        if (req.isError)
        {
            Debug.LogWarning("[HololensPane:coGetBatteryCharge] error: " + req.error);
        } else
        {
            Debug.Log("[HololensPane:coGetBatteryCharge] success: " + req.downloadHandler.text);
            BatteryResponse parsed = BatteryResponse.CreateFromJSON(req.downloadHandler.text);
            setBatteryLevel(parsed);
        }
    }

	
	// Update is called once per frame
	void Update () {
		
	}
}
