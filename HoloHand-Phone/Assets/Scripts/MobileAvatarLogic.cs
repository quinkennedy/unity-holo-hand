using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class MobileAvatarLogic : NetworkBehaviour {

#if UNITY_ANDROID
    private bool _localPlayer;
    // Use this for initialization
    void Start ()
    {
        _localPlayer = isLocalPlayer;
        if (isLocalPlayer)
        {
            ConfigPane.instance.OnConnectedToServer();
        }
	}
    
    private void OnDestroy()
    {
        //we use the cached version of isLocalPlayer
        //because once the server disconnects, isLocalPlayer is false
        if (_localPlayer)
        {
            ConfigPane.instance.OnDisconnectedFromServer();
        }
    }

    // Update is called once per frame
    void Update () {

    }
#endif
}
