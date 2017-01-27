using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AndroidBackHandler : MonoBehaviour {

    public Transform OverviewPane;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                //if the OverviewPane is in focus, suspend the app
                if (OverviewPane.GetSiblingIndex() == (OverviewPane.parent.childCount - 1))
                {
                    AndroidJavaObject activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
                    activity.Call<bool>("moveTaskToBack", true);
                } else
                {
                    //otherwise, go "back" to the overview pane
                    OverviewPane.SetAsLastSibling();
                }
            }
            else
            {
                Application.Quit();
            }
        }
    }
}
