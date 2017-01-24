using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class KinectCalibrationPlane : MonoBehaviour {

    public static List<KinectCalibrationPlane> calibrationPlanes;
    private GameObject HMD;
    public Material wireframe;
    private Bounds _activeBounds;
    public Bounds activeBounds
    {
        get
        {
            return _activeBounds;
        }
    }

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

#if UNITY_STANDALONE
        //if this is the Kinect server, we want to add a bounding box for testing
        // which Hololens is active
        GameObject activeBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //activeBox.GetComponent<MeshRenderer>().enabled = false;
        activeBox.GetComponent<MeshRenderer>().material = wireframe;
        activeBox.transform.SetParent(transform, false);
        activeBox.transform.localPosition = KinectDebug.configuration.HMD_active_area.Position;
        activeBox.transform.localRotation = Quaternion.Euler(KinectDebug.configuration.HMD_active_area.Rotation);
        activeBox.transform.localScale = KinectDebug.configuration.HMD_active_area.Scale;
        _activeBounds = activeBox.GetComponent<BoxCollider>().bounds;
#endif

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
