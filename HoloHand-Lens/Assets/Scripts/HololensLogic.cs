using HoloToolkit.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HololensLogic : MonoBehaviour {

    public GameObject worldLabel;
    private List<GameObject> anchors = new List<GameObject>();

	// Use this for initialization
	void Start () {
		
	}

    void AddAnchor()
    {

        if (WorldAnchorManager.Instance != null)
        {
            WorldAnchorManager.Instance.AttachAnchor(CalibrationPlane.gameObject, ObjectAnchorStoreName);
        }
        else
        {
            Debug.LogWarning("[HololensAvatarLogic:Start] WorldAnchorManager.Instance is null");
        }
    }

	
	// Update is called once per frame
	void Update () {
		
	}
}
