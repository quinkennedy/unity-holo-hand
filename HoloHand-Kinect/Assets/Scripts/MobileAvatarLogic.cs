using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class MobileAvatarLogic : NetworkBehaviour {
    public static MobileAvatarLogic myself;

#if UNITY_ANDROID
    private bool _localPlayer;
    // Use this for initialization
    void Start ()
    {
        _localPlayer = isLocalPlayer;
        if (isLocalPlayer)
        {
            myself = this;
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
#endif

    [Command]
    public void CmdChangeClientState(NetworkInstanceId target, int index)
    {
        HololensAvatarLogic hololensTarget = NetworkServer.objects[target].GetComponent<HololensAvatarLogic>();
        Debug.Log("[MobileAvatarLogic:CmdChangeClientState] changing " + hololensTarget.ID + " to " + index);
        hololensTarget.TargetChangeState(hololensTarget.connectionToClient, index);
    }
}
