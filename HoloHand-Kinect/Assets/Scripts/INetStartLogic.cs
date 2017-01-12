using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public abstract class INetStartLogic {

    public virtual void LoadConfig() {
        //NO-OP
    }

    public virtual void StartNetwork()
    {
        NetworkManager.singleton.StartClient();
    }

    public virtual bool ShouldStartNetwork()
    {
        return !NetworkManager.singleton.isNetworkActive;
    }

    public virtual void OnApplicationPause(bool pauseStatus) {
        //NO-OP
    }
}
