using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HololensTab : TabLogic {

    public RectTransform chargeRect;
    public Image chargeImg;
    public Image PluggedImage;
    public Color BatteryOK, BatteryLow;
    public float lowLevel = 0.25f;

    public void SetCharge(float amt)
    {
        Vector2 parentSize = GetComponent<RectTransform>().sizeDelta;
        chargeRect.sizeDelta = new Vector2(parentSize.x * amt, parentSize.y);
        chargeImg.color = (amt <= lowLevel ? BatteryLow : BatteryOK);
    }

    public void SetPlugged(bool pluggedIn)
    {
        PluggedImage.enabled = pluggedIn;
    }
}
