using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class WorldLabelLogic : NetworkBehaviour {

    GameObject linkedAnchor;
    [SyncVar]
    public string anchorName;

    // Use this for initialization
    void Start () {
		
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
            transform.position = linkedAnchor.transform.position;
            transform.rotation = linkedAnchor.transform.rotation;
        }
	}
}
