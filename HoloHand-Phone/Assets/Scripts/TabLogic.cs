using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TabLogic : MonoBehaviour {

    public GameObject pane;

    public string Title
    {
        get
        {
            return transform.Find("Text").GetComponent<Text>().text;
        }
        set
        {
            transform.Find("Text").GetComponent<Text>().text = value;
        }
    }

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(clicked);
    }

    public void clicked()
    {
        //notify the tab wrangler
        HololensTabWrangler.Instance.setActiveTab(this);
        //brighten the tab
        Button button = GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        button.colors = colors;
        //bring the associated content pane to the front
        pane.transform.SetAsLastSibling();
    }

    public void backgrounded()
    {
        //fade back
        Button button = GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.gray;
        button.colors = colors;
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
