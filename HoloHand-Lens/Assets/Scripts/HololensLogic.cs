using HoloToolkit.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HololensLogic : MonoBehaviour {

    public GameObject worldLabel;
    private List<GameObject> anchors = new List<GameObject>();
    public static HololensLogic Instance;

	// Use this for initialization
	void Start () {
		
	}

    private void Awake()
    {
        Instance = this;
    }

    public List<GameObject> getAnchors()
    {
        return anchors;
    }

    public void AddAnchor()
    {
        string name = "Anchor" + anchors.Count;
        GameObject goAnchor = new GameObject(name);
        anchors.Add(goAnchor);
        //place 1m in front of you
        goAnchor.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 1;

        Vector3 targetPostition = new Vector3(Camera.main.transform.position.x,
                                   goAnchor.transform.position.y,
                                   Camera.main.transform.position.z);

        goAnchor.transform.LookAt(targetPostition);
        goAnchor.transform.Rotate(new Vector3(0, 180, 0));
#if !UNITY_EDITOR
        //attach to a world anchor
        if (WorldAnchorManager.Instance != null)
        {
            WorldAnchorManager.Instance.AttachAnchor(goAnchor, name);
        }
        else
        {
            Debug.LogWarning("[HololensLogic:Start] WorldAnchorManager.Instance is null");
        }
#endif
        if (HololensAvatarLogic.myAvatar != null)
        {
            HololensAvatarLogic.myAvatar.CreateWorldLabel(name);
        }
    }

    //remove the last anchor
    public void RemoveAnchor()
    {
        if (anchors.Count > 0)
        {
            int removeIndex = anchors.Count - 1;
            GameObject goAnchor = anchors[removeIndex];
            string removeName = goAnchor.name;
            anchors.RemoveAt(removeIndex);
#if !UNITY_EDITOR
            if (WorldAnchorManager.Instance != null)
            {
                WorldAnchorManager.Instance.RemoveAnchor(goAnchor);
            }
#endif
            GameObject.Destroy(goAnchor);
            if (HololensAvatarLogic.myAvatar != null)
            {
                HololensAvatarLogic.myAvatar.DestroyWorldLabel(removeName);
            }
        }
    }

	
	// Update is called once per frame
	void Update () {
		
	}
}
