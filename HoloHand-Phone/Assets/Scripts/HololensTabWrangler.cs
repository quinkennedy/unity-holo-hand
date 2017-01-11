using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HololensTabWrangler : MonoBehaviour {

    public static HololensTabWrangler Instance;
    private List<HololensPane> hlTabs;
    private List<HololensAvatarLogic> hololenses;
    public GameObject tabPrefab, hololensPanePrefab;
    private TabLogic activeTab;
    public TabLogic configTab;

	// Use this for initialization
	void Start () {
        Instance = this;
        hlTabs = new List<HololensPane>();
        hololenses = new List<HololensAvatarLogic>();
        //load stored tabs
        loadTabs();
	}

    public void setActiveTab(TabLogic tab)
    {
        if (activeTab != null)
        {
            activeTab.backgrounded();
        }
        activeTab = tab;
    }

    public void deleteTab(HololensPane pane)
    {
        int tabToRemove = -1;
        for(int i = 0; i < hlTabs.Count; i++)
        {
            if (tabToRemove != -1)
            {
                hlTabs[i].SaveData(i - 1);
            } else if (hlTabs[i] == pane)
            {
                Debug.Log("[HololensTabWrangler:deleteTab] removing tab " + i);
                tabToRemove = i;
            }
        }

        if (tabToRemove != -1)
        {
            configTab.clicked();
            //remove the tab from our list
            hlTabs.RemoveAt(tabToRemove);
            //remove the tab from saved state
            HololensPane.DeleteData(hlTabs.Count);
            PlayerPrefs.SetInt("NumDevices", hlTabs.Count);
            PlayerPrefs.Save();
            //remove the tab from the scene
            GameObject.Destroy(pane.tab.gameObject);
            GameObject.Destroy(pane.gameObject);
        } else {
            Debug.LogWarning("[HololensTabWrangler:deletTab] couldn't find tab");
        }
    }

    private HololensPane createTab()
    {
        //create the tab and pane separately since they go in different containers
        GameObject tabGO = GameObject.Instantiate(tabPrefab, transform.Find("TabPanel"), false);
        TabLogic tab = tabGO.GetComponent<TabLogic>();
        GameObject paneGO = GameObject.Instantiate(hololensPanePrefab, transform.Find("ContentPanel"), false);
        HololensPane pane = paneGO.GetComponent<HololensPane>();

        //connect the tab and pane together
        pane.tab = tab;
        tab.pane = paneGO;

        //make sure the pane doesn't interrupt the current view
        paneGO.transform.SetAsFirstSibling();
        tab.backgrounded();

        hlTabs.Add(pane);
        return pane;
    }

    private void loadTabs()
    {
        int numDevices = PlayerPrefs.GetInt("NumDevices");
        for(int i = 0; i < numDevices; i++)
        {
            HololensPane pane = createTab();
            pane.LoadData(i);
        }
        //start with the config tab active
        configTab.clicked();
    }

    public void RegisterHololens(HololensAvatarLogic hololens)
    {
        bool foundTab = false;
        //match a Hololens to it's tab by ID
        for(int i = 0; i < hlTabs.Count && !foundTab; i++)
        {
            HololensPane pane = hlTabs[i];
            if (pane.ID.Equals(hololens.ID) && 
                !pane.hasLinkedHololens())
            {
                foundTab = true;
                pane.linkHololens(hololens);
                pane.SaveData(i);
                PlayerPrefs.Save();
            }
        }

        //if no matching tab was found, create a new one
        if (!foundTab)
        {
            //create a new tab
            HololensPane pane = createTab();
            //match it to this hololens
            pane.ID = hololens.ID;
            pane.linkHololens(hololens);
            //save so we load the tab next time
            pane.SaveData(hlTabs.Count - 1);
            PlayerPrefs.SetInt("NumDevices", hlTabs.Count);
            PlayerPrefs.Save();
        }
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
