using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KinectRegistration : MonoBehaviour {

    public static Transform activeHMD;
    public KinectDebug kinect;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (KinectCalibrationPlane.calibrationPlanes != null && 
            KinectCalibrationPlane.calibrationPlanes.Count > 0)
        {

            //select the Hololens which is inside the "active" zone
            // relative to its calibration point
            activeHMD = null;
            foreach (KinectCalibrationPlane plane in KinectCalibrationPlane.calibrationPlanes)
            {
                Transform hmd = plane.getHMD();
                if (plane.activeBounds.Contains(plane.transform.InverseTransformPoint(hmd.position)))//DepthSourceView.PointInOABB(hmd.position, plane.activeBounds))// 
                {
                    //once we found the active headset
                    //align the kinect to the headset's calibration point
                    activeHMD = hmd;

                    Matrix4x4 anchorOffset = Matrix4x4.TRS(-KinectDebug.configuration.KinectAnchor, 
                                                           Quaternion.identity, 
                                                           Vector3.one);
                    Matrix4x4 rotate = Matrix4x4.TRS(Vector3.zero, plane.transform.rotation, Vector3.one);
                    Matrix4x4 translate = Matrix4x4.TRS(plane.transform.position, Quaternion.identity, Vector3.one);
                    Matrix4x4 mat = Matrix4x4.identity;
                    mat = anchorOffset * mat;
                    mat = rotate * mat;
                    mat = translate * mat;

                    transform.rotation = Quaternion.LookRotation(mat.GetColumn(2), mat.GetColumn(1));
                    transform.position = mat.GetColumn(3);
                    break;
                }
            }

            ////align the kinect with the closest HoloLens
            //KinectCalibrationPlane closestPlane = KinectCalibrationPlane.calibrationPlanes[0];
            //float HMDtoPlaneDistance, closestHMDtoPlaneDistance = closestPlane.getDistanceToHMD();
            //for (int i = 1; i < KinectCalibrationPlane.calibrationPlanes.Count; i++)
            //{
            //    HMDtoPlaneDistance = KinectCalibrationPlane.calibrationPlanes[i].getDistanceToHMD();
            //    if (HMDtoPlaneDistance < closestHMDtoPlaneDistance)
            //    {
            //        closestPlane = KinectCalibrationPlane.calibrationPlanes[i];
            //        closestHMDtoPlaneDistance = HMDtoPlaneDistance;
            //    }
            //}

            //activeHMD = closestPlane.getHMD();

            //transform.position = closestPlane.transform.position;
            //transform.rotation = closestPlane.transform.rotation;
        }
	}
}
