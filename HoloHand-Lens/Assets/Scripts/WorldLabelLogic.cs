using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class WorldLabelLogic : NetworkBehaviour {

    GameObject linkedAnchor;

	// Use this for initialization
	void Start () {
		
	}

    public override void OnStartAuthority()
    {
        foreach (GameObject goAnchor in HololensLogic.Instance.getAnchors())
        {
            if (goAnchor.name.Equals(name))
            {
                Debug.Log("[WorldLabelLogic:OnStartAuthority] linking anchor " + name);
                linkedAnchor = goAnchor;
                break;
            }
        }
        if ( linkedAnchor == null )
        {
            Debug.LogWarning("[WorldLabelLogic:OnStartAuthority] couldn't find matching anchor for " + name);
        }
    }

    // Update is called once per frame
    void Update () {
        if (hasAuthority)
        {
            transform.position = linkedAnchor.transform.position;
            transform.rotation = linkedAnchor.transform.rotation;
        }
	}
}
