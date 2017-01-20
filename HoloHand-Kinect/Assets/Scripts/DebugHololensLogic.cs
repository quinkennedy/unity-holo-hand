using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugHololensLogic : MonoBehaviour {

	// Use this for initialization
	void Start () {
        KinectRegistration.activeHMD = transform;
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
