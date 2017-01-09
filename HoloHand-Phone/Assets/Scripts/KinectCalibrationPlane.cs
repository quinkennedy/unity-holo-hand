using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class KinectCalibrationPlane : MonoBehaviour {

    public static List<KinectCalibrationPlane> calibrationPlanes;
    private GameObject HMD;

#if UNITY_WSA_10_0
    private bool Placing = false;
#endif

    // Use this for initialization
    void Start() {

        HMD = transform.parent.Find("HMD").gameObject;

        if (calibrationPlanes == null)
        {
            calibrationPlanes = new List<KinectCalibrationPlane>();
        }
        calibrationPlanes.Add(this);

    }

    private void OnDestroy()
    {
        calibrationPlanes.Remove(this);
    }

    public float getDistanceToHMD()
    {
        return Vector3.Distance(transform.position, HMD.transform.position);
    }

    public Transform getHMD()
    {
        return HMD.transform;
    }
}
