using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ThermalStageResponse {
    public int CurrentStage;

    public static ThermalStageResponse CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<ThermalStageResponse>(jsonString);
    }
}
