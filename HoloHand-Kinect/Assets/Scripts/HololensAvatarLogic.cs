using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

#if UNITY_WSA_10_0
using UnityEngine.VR.WSA.Persistence;
using UnityEngine.VR.WSA;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using UnityEngine.Events;
#endif

public class HololensAvatarLogic : NetworkBehaviour {

    Transform HMD;
    Transform CalibrationPlane;
    GameObject CalibrationModel;
    private string ObjectAnchorStoreName = "kinect_anchor";
    private bool Placing = false;

    // Use this for initialization
    void Start()
    {
        CalibrationPlane = transform.Find("KinectCalibrationPlane");
        CalibrationModel = CalibrationPlane.Find("Model").gameObject;
        //CalibrationModel.SetActive(false);

        //CalibrationModel.GetComponent<MeshRenderer>().enabled = false;

        if (isLocalPlayer)
        {
            HMD = transform.Find("HMD");
#if UNITY_WSA_10_0

            if (WorldAnchorManager.Instance != null)
            {
                WorldAnchorManager.Instance.AttachAnchor(CalibrationPlane.gameObject, ObjectAnchorStoreName);
            }
            else
            {
                Debug.LogWarning("[HololensAvatarLogic:Start] WorldAnchorManager.Instance is null");
            }

            KeywordManager keywordManager = Camera.main.GetComponent<KeywordManager>();
            if (keywordManager != null)
            {
                Debug.Log("[HololensAvatarLogic:Start] updating keyword listeners");
                keywordManager.KeywordsAndResponses[0].Response.AddListener(this.Reset);
                keywordManager.KeywordsAndResponses[1].Response.AddListener(this.Place);
            }
            else
            {
                Debug.LogWarning("[KinectCalibrationPlane:Start] Didn't locate KeywordManager");
            }
#endif
        }
    }
	
	// Update is called once per frame
	void Update () {
        if (isLocalPlayer)
        {
            HMD.position = Camera.main.transform.position;
            HMD.rotation = Camera.main.transform.rotation;

            if (Placing)
            {
                CalibrationPlane.position = Camera.main.transform.position + Camera.main.transform.forward * 1;

                Vector3 targetPostition = new Vector3(Camera.main.transform.position.x,
                                           CalibrationPlane.position.y,
                                           Camera.main.transform.position.z);

                CalibrationPlane.LookAt(targetPostition);
                CalibrationPlane.Rotate(new Vector3(0, 180, 0));
            }
        }
    }

#if UNITY_WSA_10_0
    public void Reset()
    {
        Debug.Log("[KinectCalibrationPlane:Reset]");

        try
        {
            WorldAnchorManager.Instance.RemoveAnchor(CalibrationPlane.gameObject);
        }
        catch (System.NullReferenceException nre)
        {
            Debug.LogError("[KinectCalibrationPlane:Reset] null reference: " + nre.Message);
        }

        CalibrationPlane.rotation = Quaternion.identity;
        CalibrationModel.SetActive(true);

        //CalibrationModel.GetComponent<MeshRenderer>().enabled = true;

        Placing = true;
    }

    public void Place()
    {
        Debug.Log("[KinectCalibrationPlane:Place] " + gameObject);

        if (Placing)
        {
            WorldAnchorManager.Instance.AttachAnchor(CalibrationPlane.gameObject, ObjectAnchorStoreName);
            //CalibrationModel.SetActive(false);

            //CalibrationModel.GetComponent<MeshRenderer>().enabled = false;
        }
        else
        {
            Reset();
        }

        Placing = !Placing;
    }
#endif
}
