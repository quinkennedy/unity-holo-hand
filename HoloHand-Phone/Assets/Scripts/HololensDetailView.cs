using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class HololensDetailView : MonoBehaviour {

    public InputField IPField;
    //UTF8, ASCII, and ISO-8859-1 encodings all work
    public Toggle ConnectedToServerToggle;
    public Toggle AppRunningToggle;
    public Text Version;
    public Slider BatterySlider;
    public Text BatteryText;
    public Image BatteryPlugged;
    public Text Title;
    public Dropdown StateSelection;
    public Dropdown ThermalState;
    public Image ThermalBG;
    public Text WarningOutput = null;
    private bool _changingState = false;
    private HololensModel _model;
    public Button ApiSync, ShutdownDevice, RestartDevice, ShutdownApp, RestartApp;

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
    public HololensModel Model
    {
        get
        {
            return _model;
        }
        set
        {
            _model = value;
            Title.text = _model.ID;
            UpdateAll();
            LinkEvents();
            _model.OnStateChange += OnStateChange;

        }
    }

    private void UpdateAll()
    {
        UpdatedAppState();
        UpdateBattery();
        UpdateWarnings();
        UpdateSceneList();
        UpdateActiveScene();
    }

    private void LinkEvents()
    {
        ApiSync.onClick.AddListener(_model.manualQueryStatus);
        ShutdownApp.onClick.AddListener(_model.ShutdownApp);
        RestartApp.onClick.AddListener(_model.RestartApp);
        ShutdownDevice.onClick.AddListener(_model.ShutdownDevice);
        RestartDevice.onClick.AddListener(_model.RestartDevice);
    }

	// Use this for initialization
	void Start ()
    {
    }

    private void UpdatedAppState()
    {
        ConnectedToServerToggle.isOn = _model.IsConnected;
        AppRunningToggle.isOn = _model.IsAppRunning;
        IPField.text = _model.IP;
        Version.text = _model.AppVersion;
    }

    private void UpdateWarnings()
    {
        string displayText = string.Empty;
        Dictionary<HololensModel.Warning, string>.ValueCollection values = _model.Warnings.Values;
        foreach (string value in values)
        {
            if (!string.IsNullOrEmpty(displayText))
            {
                displayText += System.Environment.NewLine;
            }
            displayText += value;
        }
        WarningOutput.text = displayText;
    }

    private void UpdateBattery()
    {
        BatterySlider.value = _model.GetCharge;
        BatteryText.text = ((int)(_model.GetCharge * 100)) + "%";
        BatteryPlugged.enabled = _model.IsCharging;
    }

    public void OnStateChange(HololensModel.StateItem item)
    {
        Debug.Log("HololensDetailView:OnStateChange] " + item);
        switch (item)
        {
            case HololensModel.StateItem.App:
                UpdatedAppState();
                break;
            case HololensModel.StateItem.Battery:
                UpdateBattery();
                break;
            case HololensModel.StateItem.Thermal:
                ThermalState.value = ((int)(_model.ThermalState));
                break;
            case HololensModel.StateItem.Warnings:
                UpdateWarnings();
                break;
            case HololensModel.StateItem.Scene:
                UpdateActiveScene();
                break;
            case HololensModel.StateItem.SceneList:
                UpdateSceneList();
                break;
        }
    }

    public void DeleteClicked()
    {
        HololensTabWrangler.Instance.deleteTab(this);
    }

    private void UpdateSceneList()
    {
        string[] scenes = _model.SceneList;
        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>(scenes.Length);
        foreach(string scene in scenes)
        {
            options.Add(new Dropdown.OptionData(scene));
        }
        int selected = StateSelection.value;
        StateSelection.options = options;
        StateSelection.value = selected;
    }

    private void UpdateActiveScene()
    {
        _changingState = true;
        StateSelection.value = _model.SceneIndex;
        _changingState = false;
    }

    public void SetState()
    {
        Debug.Log("[HololensDetailView:SetState] triggered");
        if (!_changingState)
        {
            Debug.Log("[HololensDetailView:SetState] setting state to " + StateSelection.value);
            _model.SceneIndex = StateSelection.value;
        }
    }
}
