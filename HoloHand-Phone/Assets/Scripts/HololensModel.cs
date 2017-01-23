using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

public class HololensModel : MonoBehaviour {

    private float _batteryLevel;
    private bool _charging;
    private int _sceneIndex;
    private string _ip;
    private string _id;
    private bool _connected;
    private bool _running;
    private ThermalLevel _thermalState;
    private bool _tracking;
    private string[] _scenes = new string[0];
    private const float _batteryWarningLevel = 0.35f;
    private HololensAvatarLogic _avatar;
    private Dictionary<Warning, string> _warnings;
    private string _appId = null;// = "HoloHand-Lens_pzq3xp76mxafg!App";
    private string _packageName = null;// = "HoloHand-Lens_1.0.0.0_x86__pzq3xp76mxafg";
    private string _version = "X.X.X.X";
    private int _dataIndex = -1;
    public enum StateItem
    {
        //when we get a response from the battery API
        Battery,
        //when the hololens changes scenes
        Scene,
        //when we get an update from the thermal API
        Thermal,
        //
        App,
        //when a warning is added or removed
        Warnings,
        //when the list of possible scenes is updates
        SceneList
    }
    public enum Warning
    {
        //general API errors, mostly caused by return error codes
        API,
        //when the battery gets below _batteryWarningLevel
        Battery,
        //when we can't find the specified package
        Package
    }
    public enum ThermalLevel
    {
        Normal, Warm, Critical
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
        set
        {
            if (_connected)
            {
                MobileAvatarLogic.myself.CmdChangeClientState(_avatar.netId, value);
            } else
            {
                _sceneIndex = value;
            }
        }
    }
    public string ID
    {
        get { return _id; }
    }
    public string IP
    {
        get { return _ip; }
    }
    public string AppVersion
    {
        get
        {
            return _version;
        }
    }
    public bool IsAppRunning
    {
        get { return _running; }
    }
    public ThermalLevel ThermalState
    {
        get { return _thermalState; }
    }
    public bool Tracking
    {
        get { return _tracking; }
    }
    public bool IsConnected
    {
        get { return _connected; }
    }
    public float GetCharge
    {
        get { return _batteryLevel; }
    }
    public bool IsCharging
    {
        get { return _charging; }
    }
    public string[] SceneList
    {
        get { return _scenes; }
    }
    public Dictionary<Warning, string> Warnings
    {
        get { return _warnings; }
    }

    public void Start()
    {
    }

    public void Update()
    {
        if (_connected)
        {
            if (_sceneIndex != _avatar.StateIndex)
            {
                _sceneIndex = _avatar.StateIndex;
                NotifyStateChange(StateItem.Scene);
            }
        }
    }

    public static HololensModel CreateModel(int index)
    {
        GameObject go = new GameObject();
        HololensModel model = go.AddComponent<HololensModel>();
        string ip = PlayerPrefs.GetString("hl" + index + "IP");
        string id = PlayerPrefs.GetString("hl" + index + "ID");
        string version = PlayerPrefs.GetString("hl" + index + "Version");
        Debug.Log(
            "[HololensModel:CreateModel] loaded #" + index + " " + id + "@" + ip + " v" + version);
        model.init(id, ip);
        model._dataIndex = index;
        model._version = version;
        return model;
    }

    public static HololensModel CreateModel(HololensAvatarLogic hololens)
    {
        GameObject go = new GameObject("HololensModel");
        HololensModel model = go.AddComponent<HololensModel>();
        model.init(hololens.ID, hololens.IP);
        model.linkHololens(hololens);
        return model;
    }

    private void init(string id, string ip)
    {
        _id = id;
        _ip = ip;
        name = (_id + "_model");
        InvokeRepeating("queryStatus", 1, 60);
        _warnings = new Dictionary<Warning, string>();
    }

    public void SaveData(int index)
    {
        Debug.Log(
            "[HololensModel:SaveData] saving #" + index + " " + ID + "@" + IP + " v" + AppVersion);
        _dataIndex = index;
        PlayerPrefs.SetString("hl" + index + "IP", IP);
        PlayerPrefs.SetString("hl" + index + "ID", ID);
        PlayerPrefs.SetString("hl" + index + "Version", AppVersion);
    }

    public static void DeleteData(int index)
    {
        Debug.Log("[HololensModel:DeleteData] deleting #" + index);
        PlayerPrefs.DeleteKey("hl" + index + "IP");
        PlayerPrefs.DeleteKey("hl" + index + "ID");
        PlayerPrefs.DeleteKey("hl" + index + "Version");
    }

    private void NotifyStateChange(StateItem item)
    {
        if (OnStateChange != null)
        {
            OnStateChange(item);
        }
    }

    public void linkHololens(HololensAvatarLogic hololens)
    {
        _avatar = hololens;
        _ip = _avatar.IP;
        _connected = true;
        _avatar.OnDestroyListeners.Add(HololensDisconnected);
        _scenes = new string[_avatar.StateNames.Count];
        _avatar.StateNames.CopyTo(_scenes, 0);
        NotifyStateChange(StateItem.App);
        NotifyStateChange(StateItem.SceneList);
        _avatar.OnStateListListeners.Add(ScenesChanged);
    }

    public void ScenesChanged(string[] scenes)
    {
        _scenes = scenes;
        NotifyStateChange(StateItem.SceneList);
    }

    public void HololensDisconnected()
    {
        _avatar = null;
        _connected = false;
        NotifyStateChange(StateItem.App);
        RefocusApp();
    }

    public void manualQueryStatus()
    {
        _warnings.Remove(Warning.API);
        queryStatus();
    }

    public void queryStatus()
    {
        if (PlayerPrefs.HasKey("hlAuth"))
        {
            string auth = getAuth();
            StartCoroutine(coFindPackage(auth));
            StartCoroutine(coGetBatteryCharge(auth));
            StartCoroutine(coGetThermalState(auth));
        }
        else
        {
            Debug.Log("[HololensModel:queryStatus] auth not set");
        }
    }

    public void RestartDevice()
    {
        _warnings.Remove(Warning.API);
        string auth = getAuth();
        StartCoroutine(coPostWithoutResponse("/api/control/restart", auth));
    }

    public void ShutdownDevice()
    {
        _warnings.Remove(Warning.API);
        string auth = getAuth();
        StartCoroutine(coPostWithoutResponse("/api/control/shutdown", auth));
    }

    private void RefocusApp()
    {
        _warnings.Remove(Warning.API);
        string auth = getAuth();
        string appID = Hex64Encode(_appId); //"SG9sb0hhbmQtTGVuc19wenEzeHA3Nm14YWZnIUFwcA==";// WWW.EscapeURL();// 
        string packageName = Hex64Encode(_packageName); //"SG9sb0hhbmQtTGVuc18xLjAuMC4wX3g4Nl9fcHpxM3hwNzZteGFmZw==";// WWW.EscapeURL();// 
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
        _warnings.Remove(Warning.API);
        string auth = getAuth();
        string appID = Hex64Encode(_appId); //"SG9sb0hhbmQtTGVuc19wenEzeHA3Nm14YWZnIUFwcA==";// WWW.EscapeURL();// 
        string packageName = Hex64Encode(_packageName); //"SG9sb0hhbmQtTGVuc18xLjAuMC4wX3g4Nl9fcHpxM3hwNzZteGFmZw==";// WWW.EscapeURL();// 
        StartCoroutine(coRestartApp(auth, appID, packageName));
    }

    public void ShutdownApp()
    {
        _warnings.Remove(Warning.API);
        string auth = getAuth();
        string packageName = Hex64Encode(_packageName); //WWW.EscapeURL("SG9sb0hhbmQtTGVuc18xLjAuMC4wX3g4Nl9fcHpxM3hwNzZteGFmZw==");// 
        StartCoroutine(coShutdownApp(auth, packageName));
    }

    public IEnumerator coFindPackage(string auth)
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
                        matched = true;
                        if (!package.PackageFullName.Equals(_packageName))
                        {
                            _packageName = package.PackageFullName;
                            _appId = package.PackageRelativeId;
                            string pattern = "\\d+\\.\\d+\\.\\d+\\.\\d+";
                            string capturedVersion = Regex.Match(_packageName, pattern).Value;
                            if (!capturedVersion.Equals(_version)) {
                                _version = capturedVersion;
                                SaveData(_dataIndex);
                                PlayerPrefs.Save();
                            }
                            Debug.Log("[HololensModel:coFindPackage] found family name match");
                            Debug.Log("[HololensModel:coFindPackage] pkg/app/version: " + _packageName + " / " + _appId +  " / " + _version);
                        }
                        break;
                    }
                }

                if (matched)
                {
                    StartCoroutine(coGetRunningProcesses(auth));
                    _warnings.Remove(Warning.Package);
                    NotifyStateChange(StateItem.Warnings);
                }
                else
                {
                    Debug.Log("[HololensModel:coFindPackage] didn't find app: " + res.downloadHandler.text);
                    _warnings.Add(Warning.Package, "package not found");
                    NotifyStateChange(StateItem.Warnings);
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
            Debug.Log("[HololensModel:coRefocusApp] app running in background, sending start message to push it to front");
            yield return coStartApp(auth, encodedAppId, encodedPackageName);
        }
        else
        {
            Debug.Log("[HololensModel:coRefocusApp] app not running, keep it off");
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
        if (!_warnings.ContainsKey(Warning.API))
        {
            req.SetRequestHeader("AUTHORIZATION", auth);
            Debug.Log("[HololensModel:coGetResponse] request to " + req.url);
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
                    "[HololensModel:coGetResponse] error: " + req.error);
            }
            else
            {
                if (req.responseCode >= 200 && req.responseCode < 300)
                {
                    Debug.Log("[HololensModel:coGetResponse] success from " + req.url);
                }
                else
                {
                    Debug.LogWarning("[HololensModel:coGetResponse] code " + req.responseCode + " from " + req.url);
                    _warnings.Add(Warning.API, "API response " + req.responseCode);
                    NotifyStateChange(StateItem.Warnings);
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
            "[HololensModel:debugResponse] code:" + res.responseCode +
            " bytes:" + res.downloadedBytes +
            " headers:" + res.GetResponseHeaders().Keys.Count);
        if (res.downloadHandler != null)
        {
            Debug.Log("[HololensModel:debugResponse] body: " + res.downloadHandler.text);
        }
        foreach (string key in res.GetResponseHeaders().Keys)
        {
            Debug.Log(
                "[HololensModel:debugResponse] header " +
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
            switch (parsed.CurrentStage)
            {
                case 1:
                    _thermalState = ThermalLevel.Normal;
                    break;
                case 2:
                    _thermalState = ThermalLevel.Warm;
                    break;
                case 3:
                    _thermalState = ThermalLevel.Critical;
                    break;
            }
            NotifyStateChange(StateItem.Thermal);
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
                        "[HololensModel:coGetRunningProcesses] " +
                        ConfigPane.instance.PackageName + " is running");
                    found = true;
                    break;
                }
            }
            _running = found;
            NotifyStateChange(StateItem.App);
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
            if (_batteryLevel < _batteryWarningLevel && !_warnings.ContainsKey(Warning.Battery))
            {
                _warnings.Add(Warning.Battery, "Low Battery");
                NotifyStateChange(StateItem.Battery);
            } else if (_batteryLevel > _batteryWarningLevel && _warnings.ContainsKey(Warning.Battery))
            {
                _warnings.Remove(Warning.Battery);
                NotifyStateChange(StateItem.Battery);
            }
            NotifyStateChange(StateItem.Battery);
        });
    }
}
