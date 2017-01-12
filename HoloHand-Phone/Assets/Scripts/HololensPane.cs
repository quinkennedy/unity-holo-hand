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
    public static string HololensAppId = "HoloHand-Lens_pzq3xp76mxafg!App";
    public static string HololensPackageName = "HoloHand-Lens_1.0.0.0_x86__pzq3xp76mxafg";
    public Toggle ConnectedToServerToggle;
    public Toggle AppRunningToggle;
    public Slider BatterySlider;
    
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

    private bool IsAppRunning
    {
        get
        {
            return AppRunningToggle.isOn;
        }
        set
        {
            AppRunningToggle.isOn = value;
        }
    }

	// Use this for initialization
	void Start ()
    {
        InvokeRepeating("queryStatus", 1, 60);
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
        Debug.Log(
            "[HololensPane:LoadData] loaded #" + index + " " + id + "@" + ip);
        IP = ip;
        ID = id;
    }

    public void SaveData(int index)
    {
        Debug.Log(
            "[HololensPane:SaveData] saving #" + index + " " + ID + "@" + IP);
        PlayerPrefs.SetString("hl" + index + "IP", IP);
        PlayerPrefs.SetString("hl" + index + "ID", ID);
    }

    private void setConnectedToServer(bool connected)
    {
        ConnectedToServerToggle.isOn = connected;
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
        BatterySlider.value = data.GetRemainingCharge();
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
        return System.Convert.ToBase64String(
            System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(s));
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
        string appID = "SG9sb0hhbmQtTGVuc19wenEzeHA3Nm14YWZnIUFwcA==";// WWW.EscapeURL();// Hex64Encode(HololensAppId));
        string packageName = "SG9sb0hhbmQtTGVuc18xLjAuMC4wX3g4Nl9fcHpxM3hwNzZteGFmZw==";// WWW.EscapeURL();// Hex64Encode(HololensPackageName));
        StartCoroutine(coRestartApp(auth, appID, packageName));
    }

    public void ShutdownApp()
    {
        string auth = getAuth();
        string packageName = WWW.EscapeURL("SG9sb0hhbmQtTGVuc18xLjAuMC4wX3g4Nl9fcHpxM3hwNzZteGFmZw==");// Hex64Encode(HololensPackageName));
        StartCoroutine(coShutdownApp(auth, packageName));
    }

    public IEnumerator coRestartApp(
        string auth, string encodedAppId, string encodedPackageName)
    {
        yield return coGetRunningProcesses(auth);
        if (IsAppRunning)
        {
            yield return coShutdownApp(auth, encodedPackageName);
        }
        yield return coStartApp(auth, encodedAppId, encodedPackageName);
        yield return new WaitForSeconds(5);
        yield return coGetRunningProcesses(auth);
    }

    public IEnumerator coShutdownApp(string auth, string encodedPackageName)
    {
        string endpoint = "/api/taskmanager/app";
        string Address = "http://" + IP + endpoint;
        //shutdown application
        string query = "?package=" + encodedPackageName;
        //optional 'forcestop=yes' allowed in querystring, 
        //but seemed to cause the HoloLens to ignore the command in testing

        yield return coGetResponse(
            UnityWebRequest.Delete(Address + query), 
            auth,
            debugResponse);
        yield return new WaitForSeconds(10);
        yield return coGetRunningProcesses(auth);
    }

    public IEnumerator coStartApp(
        string auth, string encodedAppId, string encodedPackageName)
    {
        string endpoint = "/api/taskmanager/app";
        string Address = "http://" + IP + endpoint;
        //start application
        string query = "?appid=" + encodedAppId + 
                       "&package=" + encodedPackageName;
        
        return coGetResponse(
            UnityWebRequest.Post(Address + query, ""),
            auth,
            debugResponse);
    }

    public IEnumerator coGetResponse(
        UnityWebRequest req, string auth, ResponseHandler handler)
    {
        req.SetRequestHeader("AUTHORIZATION", auth);
        Debug.Log("[HololensPane:coSendWithoutResponse] request to " + req.url);
        yield return req.Send();
        if (req.downloadHandler != null)
        {
            while (!req.downloadHandler.isDone)
            {
                yield return new WaitForEndOfFrame();
            }
        } else
        {
            while (!req.isDone)
            {
                yield return new WaitForEndOfFrame();
            }
        }

        if (req.isError)
        {
            Debug.LogWarning(
                "[HololensPane:coSendWithoutResponse] error: " + req.error);
        }
        else
        {
            Debug.Log(
                "[HololensPane:coSendWithoutResponse] success from " + req.url);
        }
        handler(req);
    }

    public void debugResponse(UnityWebRequest res)
    {
        Debug.Log(
            "[HololensPane:debugResponse] code:" + res.responseCode +
            " bytes:" + res.downloadedBytes +
            " headers:" + res.GetResponseHeaders().Keys.Count);
        if (res.downloadHandler != null)
        {
            Debug.Log("[HololensPane:debugResponse] body: " + res.downloadHandler.text);
        }
        foreach (string key in res.GetResponseHeaders().Keys)
        {
            Debug.Log(
                "[HololensPane:debugResponse] header " +
                key + ": " + res.GetResponseHeader(key));
        }
    }

    public IEnumerator coSendWithoutResponse(UnityWebRequest req, string auth)
    {
        return coGetResponse(req, auth, (res) => { });
    }

    public IEnumerator coPostWithoutResponse(string endpoint, string auth)
    {
        UnityWebRequest req = 
            UnityWebRequest.Post("http://" + IP + endpoint, "");
        return coSendWithoutResponse(req, auth);
    }

    public IEnumerator coGetThermalState(string auth)
    {
        UnityWebRequest req = 
            UnityWebRequest.Get("http://" + IP + 
                                "/api/holographic/thermal/stage");
        return coGetResponse(req, auth, (res) =>
        {
            ThermalStageResponse parsed = 
                ThermalStageResponse.CreateFromJSON(req.downloadHandler.text);
        });
    }

    public delegate void ResponseHandler(UnityWebRequest req);
    
    public IEnumerator coGetRunningProcesses(string auth)
    {
        UnityWebRequest req = 
            UnityWebRequest.Get(
                "http://" + IP + "/api/resourcemanager/processes");
        return coGetResponse(req, auth, (res) =>
        {
            RunningProcessesResponse parsed =
                   RunningProcessesResponse.CreateFromJSON(
                       req.downloadHandler.text);
            bool found = false;
            foreach(RunningProcessesResponse.Process process in parsed.Processes)
            {
                if (process.ImageName.ToLower().Equals("holohand-lens.exe"))
                {
                    Debug.Log(
                        "[HololensPane:coGetRunningProcesses]"+
                        " holohand-lens is running");
                    found = true;
                    break;
                }
            }
            IsAppRunning = found;
            /*if (!found)
            {
                foreach(RunningProcessesResponse.Process process in parsed.Processes)
                {
                    Debug.Log(process.ImageName);
                }
            }*/
        });
    }

    public IEnumerator coGetBatteryCharge(string auth)
    {
        UnityWebRequest req = 
            UnityWebRequest.Get("http://" + IP + "/api/power/battery");
        return coGetResponse(req, auth, (res) =>
        {
            BatteryResponse parsed =
                BatteryResponse.CreateFromJSON(req.downloadHandler.text);
            setBatteryLevel(parsed);
        });
    }
    
	// Update is called once per frame
	void Update () {
		
	}
}
