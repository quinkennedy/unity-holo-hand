using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KinectRegistration : MonoBehaviour {

    public static Transform closestHMD;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (KinectCalibrationPlane.calibrationPlanes != null && 
            KinectCalibrationPlane.calibrationPlanes.Count > 0)
        {

            //align the kinect with the closest HoloLens
            KinectCalibrationPlane closestPlane = KinectCalibrationPlane.calibrationPlanes[0];
            float HMDtoPlaneDistance, closestHMDtoPlaneDistance = closestPlane.getDistanceToHMD();
            for (int i = 1; i < KinectCalibrationPlane.calibrationPlanes.Count; i++)
            {
                HMDtoPlaneDistance = KinectCalibrationPlane.calibrationPlanes[i].getDistanceToHMD();
                if (HMDtoPlaneDistance < closestHMDtoPlaneDistance)
                {
                    closestPlane = KinectCalibrationPlane.calibrationPlanes[i];
                    closestHMDtoPlaneDistance = HMDtoPlaneDistance;
                }
            }

            closestHMD = closestPlane.getHMD();

            transform.position = closestPlane.transform.position;
            transform.rotation = closestPlane.transform.rotation;
        }
	}
}
