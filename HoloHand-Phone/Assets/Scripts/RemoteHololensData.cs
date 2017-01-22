using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

public class RemoteHololensData : MonoBehaviour {

    private float _batteryLevel;
    private bool _charging;
    private int _stateIndex;
    private string _ip;
    private string _id;
    private bool _connected;
    private bool _running;
    private int _thermalState;
    private HololensAvatarLogic _avatar;
    private WarningBucket warnings;
    private static string HololensAppId = null;// = "HoloHand-Lens_pzq3xp76mxafg!App";
    private static string HololensPackageName = null;// = "HoloHand-Lens_1.0.0.0_x86__pzq3xp76mxafg";
    public enum StateItem
    {
        Battery, Scene, Thermal, App, Warnings
    }
    public enum Warning
    {
        API, Battery, Package
    }
    public delegate void StateUpdated(StateItem item);
    public event StateUpdated OnStateChange;

    public int SceneIndex
    {
        get
        {
            if (_avatar != null)
            {
                return _avatar.StateIndex;
            } else
            {
                return 0;
            }
        }
    }
    public string ID
    {
        get
        {
            return _id;
        }
    }
    public string IP
    {
        get
        {
            return _ip;
        }
    }
    public string AppVersion
    {
        get
        {
            if (HololensPackageName != null)
            {
                string pattern = "\\d\\.\\d\\.\\d\\.\\d";
                return Regex.Match(HololensPackageName, pattern).Value;
            } else
            {
                return "X.X.X.X";
            }
        }
    }
    public bool IsAppRunning
    {
        get
        {
            return _running;
        }
    }
    public int ThermalState
    {
        get
        {
            return _thermalState;
        }
    }

    public RemoteHololensData(int index)
    {
        string ip = PlayerPrefs.GetString("hl" + index + "IP");
        string id = PlayerPrefs.GetString("hl" + index + "ID");
        Debug.Log(
            "[HololensPane:LoadData] loaded #" + index + " " + id + "@" + ip);
        _ip = ip;
        _id = id;

        init();
    }

    public RemoteHololensData(HololensAvatarLogic hololens, int index)
    {
        _id = hololens.ID;
        _ip = hololens.IP;

        init();
    }

    private void init()
    {
        InvokeRepeating("queryStatus", 1, 60);
        warnings = new WarningBucket();
    }

    public void SaveData(int index)
    {
        Debug.Log(
            "[HololensPane:SaveData] saving #" + index + " " + ID + "@" + IP);
        PlayerPrefs.SetString("hl" + index + "IP", IP);
        PlayerPrefs.SetString("hl" + index + "ID", ID);
    }

    public static void DeleteData(int index)
    {
        Debug.Log("[HololensPane:DeleteData] deleting #" + index);
        PlayerPrefs.DeleteKey("hl" + index + "IP");
        PlayerPrefs.DeleteKey("hl" + index + "ID");
    }

    public void linkHololens(HololensAvatarLogic hololens)
    {
        _ip = hololens.IP;
        _avatar = hololens;
        _connected = true;
        _avatar.OnDestroyListeners.Add(HololensDisconnected);
        string[] states = new string[_avatar.StateNames.Count];
        for (int i = 0; i < states.Length; i++)
        {
            states[i] = _avatar.StateNames[i];
        }
        StatesChanged(states);
        _avatar.OnStateListListeners.Add(StatesChanged);
    }

    public void HololensDisconnected()
    {
        _avatar = null;
        _connected = false;
        RefocusApp();
    }

    public void manualQueryStatus()
    {
        warnings.removeWarning(Warning.API);
        queryStatus();
    }

    public void queryStatus()
    {
        if (PlayerPrefs.HasKey("hlAuth"))
        {
            string auth = getAuth();
            if (string.IsNullOrEmpty(HololensPackageName))
            {
                StartCoroutine(coGetPackageList(auth));
            }
            else
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
        string auth = getAuth();
        StartCoroutine(coPostWithoutResponse("/api/control/restart", auth));
    }

    public void ShutdownDevice()
    {
        string auth = getAuth();
        StartCoroutine(coPostWithoutResponse("/api/control/shutdown", auth));
    }

    private void RefocusApp()
    {
        string auth = getAuth();
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
                string compareTo = ConfigPane.instance.PackageName.ToLower();
                foreach (InstalledPackagesResponse.Package package in parsed.InstalledPackages)
                {
                    if (package.Name.ToLower().Equals(compareTo) ||
                        package.PackageFamilyName.ToLower().Equals(compareTo))
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
                    warnings.removeWarning(Warning.Package);
                    if (OnStateChange != null)
                    {
                        OnStateChange(StateItem.Warnings);
                    }
                }
                else
                {
                    Debug.Log("[HololensPane:coGetPackageList] didn't find app: " + res.downloadHandler.text);
                    warnings.addWarning(Warning.Package, "package not found");
                    if (OnStateChange != null)
                    {
                        OnStateChange(StateItem.Warnings);
                    }
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
        //wait a short amount of time to avoid a race condition with coShutdownApp
        yield return new WaitForSeconds(2);
        yield return coGetRunningProcesses(auth);
        if (IsAppRunning)
        {
            Debug.Log("[HololensPane:coRefocusApp] app running in background, sending start message to push it to front");
            yield return coStartApp(auth, encodedAppId, encodedPackageName);
        }
        else
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
        if (!warnings.hasWarning(Warning.API))
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
            }
            else
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
                if (req.responseCode >= 200 && req.responseCode < 300)
                {
                    Debug.Log("[HololensPane:coGetResponse] success from " + req.url);
                }
                else
                {
                    Debug.LogWarning("[HololensPane:coGetResponse] code " + req.responseCode + " from " + req.url);
                    warnings.addWarning(Warning.API, "API response " + req.responseCode);
                    if (OnStateChange != null)
                    {
                        OnStateChange(StateItem.Warnings);
                    }
                    //TODO: limit queries or notify user based on return code
                    //code 401 for wrong auth - set warning and prompt user to set auth. Wait for "sync API"
                    //code 343 for temporarily blocked IP (due to repeated bad access attempts) - set warning. Wait for "sync API"
                    //code 307 redirect to https if https is forced - set warning and prompt user to change security settings. Wait for "sync API"
                }
            }
            handler(req);
        }
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
            _thermalState = parsed.CurrentStage - 1;
            if (OnStateChange != null)
            {
                OnStateChange(StateItem.Thermal);
            }
            //ThermalState.value = parsed.CurrentStage - 1;
            //if (parsed.CurrentStage >= 3)
            //{
            //    ThermalBG.color = Color.red;
            //}
            //else
            //{
            //    ThermalBG.color = Color.white;
            //}
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
            foreach (RunningProcessesResponse.Process process in parsed.Processes)
            {
                if (process.ImageName.ToLower().StartsWith(ConfigPane.instance.PackageName.ToLower()))
                {
                    Debug.Log(
                        "[HololensPane:coGetRunningProcesses] " +
                        ConfigPane.instance.PackageName + " is running");
                    found = true;
                    break;
                }
            }
            _running = found;
            if (OnStateChange != null)
            {
                OnStateChange(StateItem.App);
            }
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
            _batteryLevel = parsed.GetRemainingCharge();
            _charging = parsed.Charging > 0;
            if (OnStateChange != null)
            {
                OnStateChange(StateItem.Battery);
            }
        });
    }
}
