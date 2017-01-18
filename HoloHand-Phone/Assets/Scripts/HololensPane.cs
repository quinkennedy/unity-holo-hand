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
    public HololensTab tab;
    private HololensAvatarLogic linkedHololens;
    public static string HololensAppId = null;// = "HoloHand-Lens_pzq3xp76mxafg!App";
    public static string HololensPackageName = null;// = "HoloHand-Lens_1.0.0.0_x86__pzq3xp76mxafg";
    //UTF8, ASCII, and ISO-8859-1 encodings all work
    public Toggle ConnectedToServerToggle;
    public Toggle AppRunningToggle;
    public Slider BatterySlider;
    public Text Title;
    public Dropdown StateSelection;
    public Dropdown ThermalState;
    public Image ThermalBG;
    private bool _changingState = false;
    private bool _apiErrors = false;

    public enum Warning
    {
        UnresponsiveAPI
    }
    
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
                Title.text = value;
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
        //hide/show icon in overview screen
        tab.SetConnected(connected);
    }

    public void linkHololens(HololensAvatarLogic hololens)
    {
        IP = hololens.IP;
        linkedHololens = hololens;
        setConnectedToServer(true);
        linkedHololens.OnDestroyListeners.Add(HololensDisconnected);
        string[] states = new string[linkedHololens.StateNames.Count];
        for (int i = 0; i < states.Length; i++) {
            states[i] = linkedHololens.StateNames[i];
        }
        StatesChanged(states);
        linkedHololens.OnStateListListeners.Add(StatesChanged);
    }

    public void HololensDisconnected()
    {
        linkedHololens = null;
        setConnectedToServer(false);
        RefocusApp();
    }

    public void StatesChanged(string[] states)
    {
        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>(states.Length);
        foreach(string state in states)
        {
            options.Add(new Dropdown.OptionData(state));
        }
        int selected = StateSelection.value;
        StateSelection.options = options;
        StateSelection.value = selected;
    }

    public void SetState()
    {
        Debug.Log("[HololensPane:SetState] triggered");
        if ((!_changingState) && hasLinkedHololens())
        {
            Debug.Log("[HololensPane:SetState] setting state to " + StateSelection.value);
            MobileAvatarLogic.myself.CmdChangeClientState(linkedHololens.netId, StateSelection.value);
        }
    }

    public bool hasLinkedHololens()
    {
        return (linkedHololens != null);
    }

    private void setBatteryLevel(BatteryResponse data)
    {
        BatterySlider.value = data.GetRemainingCharge();
        //show charge on overview screen
        tab.SetCharge(data.GetRemainingCharge());
    }

    public void queryStatus()
    {
        if (PlayerPrefs.HasKey("hlAuth"))
        {
            String auth = getAuth();
            if (string.IsNullOrEmpty(HololensPackageName))
            {
                StartCoroutine(coGetPackageList(auth));
            } else
            {
                StartCoroutine(coGetRunningProcesses(auth));
            }
            StartCoroutine(coGetBatteryCharge(auth));
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

    private void RefocusApp()
    {
        String auth = getAuth();
        string appID = Hex64Encode(HololensAppId); //"SG9sb0hhbmQtTGVuc19wenEzeHA3Nm14YWZnIUFwcA==";// WWW.EscapeURL();// 
        string packageName = Hex64Encode(HololensPackageName); //"SG9sb0hhbmQtTGVuc18xLjAuMC4wX3g4Nl9fcHpxM3hwNzZteGFmZw==";// WWW.EscapeURL();// 
        StartCoroutine(coRefocusApp(auth, appID, packageName));
    }

    public static string Base64Encode(string s)
    {
        return System.Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(s));
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
        string appID = Hex64Encode(HololensAppId); //"SG9sb0hhbmQtTGVuc19wenEzeHA3Nm14YWZnIUFwcA==";// WWW.EscapeURL();// 
        string packageName = Hex64Encode(HololensPackageName); //"SG9sb0hhbmQtTGVuc18xLjAuMC4wX3g4Nl9fcHpxM3hwNzZteGFmZw==";// WWW.EscapeURL();// 
        StartCoroutine(coRestartApp(auth, appID, packageName));
    }

    public void ShutdownApp()
    {
        string auth = getAuth();
        string packageName = Hex64Encode(HololensPackageName); //WWW.EscapeURL("SG9sb0hhbmQtTGVuc18xLjAuMC4wX3g4Nl9fcHpxM3hwNzZteGFmZw==");// 
        StartCoroutine(coShutdownApp(auth, packageName));
    }

    public IEnumerator coGetPackageList(string auth)
    {
        string endpoint = "/api/app/packagemanager/packages";
        string address = "http://" + IP + endpoint;

        yield return coGetResponse(
            UnityWebRequest.Get(address),
            auth,
            (res) =>
            {
                InstalledPackagesResponse parsed =
                    InstalledPackagesResponse.CreateFromJSON(res.downloadHandler.text);
                //search for package that matches
                bool matched = false;
                foreach(InstalledPackagesResponse.Package package in parsed.InstalledPackages)
                {
                    if (package.PackageFamilyName.ToLower().Equals(ConfigPane.instance.PackageName.ToLower()))
                    {
                        HololensPackageName = package.PackageFullName;
                        HololensAppId = package.PackageRelativeId;
                        Debug.Log("[HololensPane:coGetPackageList] found family name match");
                        Debug.Log("[HololensPane:coGetPackageList] pkg/app: " + HololensPackageName + " / " + HololensAppId);
                        matched = true;
                        break;
                    }
                }

                if (matched)
                {
                    StartCoroutine(coGetRunningProcesses(auth));
                } else {
                    Debug.Log("[HololensPane:coGetPackageList] didn't find app: " + res.downloadHandler.text);
                }
            });
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

    public IEnumerator coRefocusApp(
        string auth, string encodedAppId, string encodedPackageName)
    {
        yield return coGetRunningProcesses(auth);
        if (IsAppRunning)
        {
            Debug.Log("[HololensPane:coRefocusApp] app running in background, sending start message to push it to front");
            yield return coStartApp(auth, encodedAppId, encodedPackageName);
        } else
        {
            Debug.Log("[HololensPane:coRefocusApp] app not running, keep it off");
        }
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
            (res) => { });
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
            (res) => { });
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
            if (req.responseCode == 200)
            {
                Debug.Log("[HololensPane:coGetResponse] success from " + req.url);
            } else
            {
                Debug.LogWarning("[HololensPane:coGetResponse] code " + req.responseCode + " from " + req.url);
                //TODO: limit queries or notify user based on return code
                //code 401 for wrong auth - set warning and prompt user to set auth. Wait for "sync API"
                //code 343 for temporarily blocked IP (due to repeated bad access attempts) - set warning. Wait for "sync API"
                //code 307 redirect to https if https is forced - set warning and prompt user to change security settings. Wait for "sync API"
            }
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
                ThermalStageResponse.CreateFromJSON(res.downloadHandler.text);
            ThermalState.value = parsed.CurrentStage - 1;
            if (parsed.CurrentStage >= 3)
            {
                ThermalBG.color = Color.red;
            } else
            {
                ThermalBG.color = Color.white;
            }
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
                       res.downloadHandler.text);
            bool found = false;
            foreach(RunningProcessesResponse.Process process in parsed.Processes)
            {
                if (process.ImageName.ToLower().StartsWith(ConfigPane.instance.PackageName.ToLower()))
                {
                    Debug.Log(
                        "[HololensPane:coGetRunningProcesses] "+
                        ConfigPane.instance.PackageName + " is running");
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
                BatteryResponse.CreateFromJSON(res.downloadHandler.text);
            setBatteryLevel(parsed);
        });
    }
    
	// Update is called once per frame
	void Update () {
		if (hasLinkedHololens())
        {
            if (StateSelection.value != linkedHololens.StateIndex)
            {
                Debug.Log("[HololensPane:Update] lens changed state to " + linkedHololens.StateIndex);
                _changingState = true;
                StateSelection.value = linkedHololens.StateIndex;
                _changingState = false;
                Debug.Log("[HololensPane:Update] updated lens state");

                ////disable callbacks for this call
                //Dropdown.DropdownEvent backup = StateSelection.onValueChanged;
                //StateSelection.onValueChanged = new Dropdown.DropdownEvent();
                ////change the value
                //StateSelection.value = linkedHololens.StateIndex;
                //StateSelection.RefreshShownValue();
                ////restore callbacks
                //StateSelection.onValueChanged = backup;
            }
        }
	}
}
