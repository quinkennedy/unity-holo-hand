using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class KinectAvatarLogic : NetworkBehaviour {

    public static KinectAvatarLogic MyAvatar;

	// Use this for initialization
	void Start () {
		if (isLocalPlayer)
        {
            MyAvatar = this;
        }
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
