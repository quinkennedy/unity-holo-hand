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
    private static Transform MyCalibration;

    // Use this for initialization
    void Start()
    {
        CalibrationPlane = transform.Find("KinectCalibrationPlane");
        CalibrationModel = CalibrationPlane.Find("Model").gameObject;
        //CalibrationModel.SetActive(false);

        //CalibrationModel.GetComponent<MeshRenderer>().enabled = false;

#if UNITY_WSA_10_0
        if (isLocalPlayer)
        {
            MyCalibration = CalibrationPlane;
            HMD = transform.Find("HMD");

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
        } else
        {
            GameObject wrapper = new GameObject("RemoteLensWrapper");
            transform.SetParent(wrapper.transform);
        }
#endif
    }

#if UNITY_WSA_10_0
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
        } else
        {
            PlaceRelativeTo(MyCalibration);
        }
    }

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
            Placing = false;
        }
    }
#endif

    public void PlaceRelativeTo(Transform TargetCalibrationPlane)
    {
        //// adapted from http://answers.unity3d.com/questions/460064/align-parent-object-using-child-object-as-point-of.html
        //// where tr1P = transform.parent
        ////       tr1C = CalibrationPlane
        ////       tr2P = world
        ////       tr2C = OtherCalibrationPlane
        //Vector3 v1 = -CalibrationPlane.localPosition;
        //Vector3 v2 = OtherCalibrationPlane.position;
        //transform.parent.rotation =  Quaternion.FromToRotation(v1, v2);
        //transform.parent.position = OtherCalibrationPlane.position + v2.normalized * v1.magnitude;


        //transform.parent.position = (OtherCalibrationPlane.position - CalibrationPlane.localPosition);
        // or ??
        //Polar polar = CartesianToPolar(new Vector2(CalibrationPlane.localPosition.x, CalibrationPlane.localPosition.z));
        //polar.radius += transform.parent.rotation.eulerAngles.y / 180 * Mathf.PI;
        //Vector2 rotatedPosition = PolarToCartesian(polar);
        //transform.parent.position = (TargetCalibrationPlane.position - new Vector3(rotatedPosition.x, CalibrationPlane.localPosition.y, rotatedPosition.y));

        //transform.parent.rotation = Quaternion.Euler(0, (TargetCalibrationPlane.rotation.eulerAngles.y - CalibrationPlane.localRotation.eulerAngles.y), 0);

        Matrix4x4 offset = Matrix4x4.TRS(-CalibrationPlane.localPosition, Quaternion.identity, Vector3.one);
        Matrix4x4 rotate = Matrix4x4.TRS(Vector3.zero, Quaternion.Inverse(CalibrationPlane.localRotation) * TargetCalibrationPlane.rotation, Vector3.one);
        Matrix4x4 offsetBack = Matrix4x4.TRS(CalibrationPlane.localPosition, Quaternion.identity, Vector3.one);
        Matrix4x4 translate = Matrix4x4.TRS(TargetCalibrationPlane.position - CalibrationPlane.localPosition, Quaternion.identity, Vector3.one);
        Matrix4x4 mat = Matrix4x4.identity;
        mat *= translate;
        mat *= offsetBack;
        mat *= rotate;
        mat *= offset;

        transform.parent.rotation = Quaternion.LookRotation(mat.GetColumn(2), mat.GetColumn(1));
        transform.parent.position = mat.GetColumn(3);
    }

    struct Polar
    {
        public float radius;
        public float angle;
    }

    private Polar CartesianToPolar(Vector2 point)
    {
        Polar polar;

        polar.radius = Vector2.Distance(Vector2.zero, point);
        polar.angle = Mathf.Atan2(point.y, point.x);
 
        return polar;
    }


    private Vector2 PolarToCartesian(Polar polar)
    {
        return new Vector2(polar.radius * Mathf.Cos(polar.angle),
                           polar.radius * Mathf.Sin(polar.angle));
    }
}
