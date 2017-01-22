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
    //UTF8, ASCII, and ISO-8859-1 encodings all work
    public Toggle ConnectedToServerToggle;
    public Toggle AppRunningToggle;
    public Slider BatterySlider;
    public Text BatteryText;
    public Image BatteryPlugged;
    public Text Title;
    public Dropdown StateSelection;
    public Dropdown ThermalState;
    public Image ThermalBG;
    public Text WarningOutput = null;
    private bool _changingState = false;
    private bool _apiErrors = false;
    private RemoteHololensData hololensData;
    
    public string ID
    {
        get
        {
            return hololensData.ID;
        }
    }
    
    public string IP
    {
        get
        {
            return hololensData.IP;
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
    }

    public void DeleteClicked()
    {
        HololensTabWrangler.Instance.deleteTab(this);
    }

    private void setConnectedToServer(bool connected)
    {
        ConnectedToServerToggle.isOn = connected;
        //hide/show icon in overview screen
        tab.SetConnected(connected);
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
        BatteryText.text = ((int)(data.GetRemainingCharge() * 100)) + "%";
        //show charge on overview screen
        tab.SetCharge(data.GetRemainingCharge());
        bool isPluggedIn = data.Charging > 0;
        tab.SetPlugged(isPluggedIn);
        BatteryPlugged.enabled = isPluggedIn;
        if (data.GetRemainingCharge() <= tab.lowLevel && data.Charging == 0)
        {
            warnings.addWarning(Warning.Battery, "plug in device");
        } else
        {
            warnings.removeWarning(Warning.Battery);
        }
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
