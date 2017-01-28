using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class WorldLabelLogic : NetworkBehaviour {

    GameObject linkedAnchor;
    public Transform model;
    [SyncVar]
    public string anchorName;
    [SyncVar]
    public NetworkInstanceId ownerNetId;

    // Use this for initialization
    void Start () {
        //if this is a World Label from a remote client
        // child it to that Hololenses wrapper so it maintains the correct relative position
        if (!hasAuthority && isClient)
        {
            Debug.Log("[WorldLabelLogic:Start] ownerNetId: " + ownerNetId);
            Transform owner = ClientScene.FindLocalObject(ownerNetId).transform;
            Debug.Log("[WorldLabelLogic:Start] owned by " + owner.GetComponent<HololensAvatarLogic>().ID);
            transform.SetParent(owner.parent, false);
        }
	}

#if UNITY_WSA_10_0
    public override void OnStartAuthority()
    {
        foreach (GameObject goAnchor in HololensLogic.Instance.getAnchors())
        {
            if (goAnchor.name.Equals(anchorName))
            {
                Debug.Log("[WorldLabelLogic:OnStartAuthority] linking anchor " + anchorName);
                linkedAnchor = goAnchor;
                break;
            }
        }
        if ( linkedAnchor == null )
        {
            Debug.LogWarning("[WorldLabelLogic:OnStartAuthority] couldn't find matching anchor for " + anchorName);
        }
    }
#endif

    // Update is called once per frame
    void Update () {
        if (hasAuthority && linkedAnchor != null)
        {
            model.position = linkedAnchor.transform.position;
            model.rotation = linkedAnchor.transform.rotation;
        } else
        {
            //Debug.Log("[WorldLabelLogic:Update] " + anchorName + " on " + ownerNetId + " at " + transform.localPosition + "=>" + transform.position);
        }
	}
}
