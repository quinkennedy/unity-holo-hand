using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HololensOverView : TabLogic {

    public RectTransform chargeRect;
    public Image chargeImg;
    public Image PluggedImage;
    public Color BatteryOK, BatteryLow;
    public Text SceneOutput;
    public GameObject Details;
    public Text Version;
    public float lowLevel = 0.25f;
    private HololensModel _model;

    public HololensModel Model
    {
        get
        {
            return _model;
        }
        set
        {
            _model = value;
            Title = _model.ID;
            UpdateAll();
            _model.OnStateChange += OnStateChange;
        }
    }

    private void UpdateAll()
    {
        UpdatedAppState();
        UpdateBattery();
        UpdateWarnings();
        UpdateActiveScene();
    }

    public void OnStateChange(HololensModel.StateItem item)
    {
        Debug.Log("HololensOverView:OnStateChange] " + item);
        switch (item)
        {
            case HololensModel.StateItem.App:
                UpdatedAppState();
                break;
            case HololensModel.StateItem.Battery:
                UpdateBattery();
                break;
            case HololensModel.StateItem.Warnings:
                UpdateWarnings();
                break;
            case HololensModel.StateItem.SceneList:
            case HololensModel.StateItem.Scene:
                UpdateActiveScene();
                break;
            case HololensModel.StateItem.Thermal:
                break;
        }
    }

    private void UpdatedAppState()
    {
        if (_model.IsConnected)
        {
            ConnectedImage.enabled = true;
            //set large (fill available space)
            GetComponent<LayoutElement>().flexibleHeight = 1;
            Debug.Log("[HololensOverView:UpdatedAppState] " + _model.ID + " moving to top");
            //move to top of list
            transform.SetAsFirstSibling();
            Details.SetActive(true);
        } else
        {
            ConnectedImage.enabled = false;
            //set back to small size
            GetComponent<LayoutElement>().flexibleHeight = 0;
            //move to bottom of list (above Config)
            Debug.Log("[HololensOverView:UpdatedAppState] " + _model.ID + " moving to " + (transform.parent.childCount - 2));
            transform.SetSiblingIndex(transform.parent.childCount - 2);
            Details.SetActive(false);
        }
        Version.text = "v" + _model.AppVersion;
    }

    private void UpdateWarnings()
    {
        WarningImage.enabled = ((_model.Warnings.Count == 1 &&
                                 !_model.Warnings.ContainsKey(HololensModel.Warning.Battery)) ||
                                _model.Warnings.Count > 1);
    }

    private void UpdateBattery()
    {
        Vector2 parentSize = GetComponent<RectTransform>().sizeDelta;
        float height = GetComponent<LayoutElement>().preferredHeight;
        chargeRect.sizeDelta = new Vector2(parentSize.x * _model.GetCharge, Mathf.Min(parentSize.y, height));
        chargeImg.color = (_model.Warnings.ContainsKey(HololensModel.Warning.Battery) ? BatteryLow : BatteryOK);
        PluggedImage.enabled = _model.IsCharging;
    }

    private void UpdateActiveScene()
    {
        if (_model.SceneIndex < _model.SceneList.Length)
        {
            SceneOutput.text = "Scene: " + _model.SceneList[_model.SceneIndex];
        }
    }
}
