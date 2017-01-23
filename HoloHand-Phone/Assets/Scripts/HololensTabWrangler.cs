using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HololensTabWrangler : MonoBehaviour {

    public static HololensTabWrangler Instance;
    private List<HololensOverView> hlTabs;
    public GameObject tabPrefab, hololensPanePrefab;
    public Transform tabContainer, paneContainer;
    private TabLogic activeTab;
    public TabLogic configTab;

	// Use this for initialization
	void Start () {
        Instance = this;
        hlTabs = new List<HololensOverView>();
        //load stored tabs
        loadTabs();
	}

    public void setActiveTab(TabLogic tab)
    {
        activeTab = tab;
    }

    public void deleteTab(HololensDetailView pane)
    {
        int tabToRemove = -1;
        for(int i = 0; i < hlTabs.Count; i++)
        {
            if (tabToRemove != -1)
            {
                hlTabs[i].Model.SaveData(i - 1);
            } else if (hlTabs[i].pane.GetComponent<HololensDetailView>() == pane)
            {
                Debug.Log("[HololensTabWrangler:deleteTab] removing tab " + i);
                tabToRemove = i;
            }
        }

        if (tabToRemove != -1)
        {
            //remove the tab from the scene
            GameObject.Destroy(hlTabs[tabToRemove].gameObject);
            GameObject.Destroy(pane.gameObject);
            //remove the tab from our list
            hlTabs.RemoveAt(tabToRemove);
            //remove the tab from saved state
            HololensModel.DeleteData(hlTabs.Count);
            PlayerPrefs.SetInt("NumDevices", hlTabs.Count);
            PlayerPrefs.Save();
        } else {
            Debug.LogWarning(
                "[HololensTabWrangler:deletTab] couldn't find tab");
        }
    }

    private HololensDetailView createTab(HololensModel model)
    {
        //create the tab and pane separately 
        //since they go in different containers
        GameObject tabGO = 
            GameObject.Instantiate(
                tabPrefab, 
                tabContainer, 
                false);
        HololensOverView tab = tabGO.GetComponent<HololensOverView>();
        tab.Model = model;
        GameObject paneGO = 
            GameObject.Instantiate(
                hololensPanePrefab, 
                paneContainer, 
                false);
        HololensDetailView pane = paneGO.GetComponent<HololensDetailView>();
        pane.Model = model;

        //connect the tab and pane together
        tab.pane = paneGO;

        //make sure the pane doesn't interrupt the current view
        paneGO.transform.SetAsFirstSibling();
        //put the tabs above the "Config" tab
        Debug.Log("[HololensTabWrangler:createTab] " + model.ID + " moving to position " + (tabContainer.childCount - 2));
        tabGO.transform.SetSiblingIndex(tabContainer.childCount - 2);

        hlTabs.Add(tab);
        return pane;
    }

    private void loadTabs()
    {
        int numDevices = PlayerPrefs.GetInt("NumDevices");
        for(int i = 0; i < numDevices; i++)
        {
            HololensModel model = HololensModel.CreateModel(i);
            createTab(model);
        }
    }

    public void RegisterHololens(HololensAvatarLogic hololens)
    {
        bool foundTab = false;
        //match a Hololens to it's tab by ID
        for(int i = 0; i < hlTabs.Count && !foundTab; i++)
        {
            HololensOverView tab = hlTabs[i];
            if (tab.Model.ID.Equals(hololens.ID) && 
                !tab.Model.IsConnected)
            {
                foundTab = true;
                tab.Model.linkHololens(hololens);
                tab.Model.SaveData(i);
                PlayerPrefs.Save();
            }
        }

        //if no matching tab was found, create a new one
        if (!foundTab)
        {
            HololensModel model = HololensModel.CreateModel(hololens);
            //create a new tab
            createTab(model);
            //save so we load the tab next time
            model.SaveData(hlTabs.Count - 1);
            PlayerPrefs.SetInt("NumDevices", hlTabs.Count);
            PlayerPrefs.Save();
        }
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
