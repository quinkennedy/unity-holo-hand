using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RunningProcessesResponse {
    public Process[] Processes;

    [System.Serializable]
    public class Process
    {
        public float CPUUsage;
        public string ImageName;
        public int PageFileUsage;
        public int PrivateWorkingSet;
        public int ProcessId;
        public int SessionId;
        public int TotalCommit;
        public string UserName;
        public int VirtualSize;
        public int WorkingSetSize;
    }

    public static RunningProcessesResponse CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<RunningProcessesResponse>(jsonString);
    }
}
