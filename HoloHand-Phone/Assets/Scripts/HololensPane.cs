using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HololensPane : MonoBehaviour {

    public InputField IPField;
    public TabLogic tab;
    private HololensAvatarLogic linkedHololens;
    
    public string ID
    {
        get
        {
            if (tab == null)
            {
                return null;
            } else
            {
                return tab.Title;
            }
        }
        set
        {
            if (tab != null)
            {
                tab.Title = value;
            }
        }
    }
    
    public string IP
    {
        get
        {
            return IPField.text;
        }
        private set
        {
            IPField.text = value;
        }
    }

	// Use this for initialization
	void Start () {
		
	}

    public void DeleteClicked()
    {
        HololensTabWrangler.Instance.deleteTab(this);
    }

    public static void DeleteData(int index)
    {
        PlayerPrefs.DeleteKey("Hololens" + index + "IP");
        PlayerPrefs.DeleteKey("Hololens" + index + "ID");
    }

    public void LoadData(int index)
    {
        IP = PlayerPrefs.GetString("Hololens" + index + "IP");
        ID = PlayerPrefs.GetString("Hololens" + index + "ID");
    }

    public void SaveData(int index)
    {
        PlayerPrefs.GetString("Hololens" + index + "IP", IP);
        PlayerPrefs.GetString("Hololens" + index + "ID", ID);
    }

    public void linkHololens(HololensAvatarLogic hololens)
    {
        IP = hololens.IP;
        linkedHololens = hololens;
    }

    public bool hasLinkedHololens()
    {
        return (linkedHololens != null);
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
