using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BatteryResponse {
    public int AcOnline;
    public int BatteryPresent;
    public int Charging;
    public int DefaultAlert1;
    public int DefaultAlert2;
    public int EstimatedTime;
    public int MaximumCapacity;
    public int RemainingCapacity;

    public static BatteryResponse CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<BatteryResponse>(jsonString);
    }

    public float GetRemainingCharge()
    {
        return (((float)RemainingCapacity) / MaximumCapacity);
    }
}
