using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MeshFilter), typeof(MeshSenderHTTP))]
public class MeshFilterStreamer : MonoBehaviour {

    public bool useLocal = true;
    public string localServerUrl = "localhost:8080";
    public string remoteServerUrl = "172.16.0.115:8080";
    public string authorName = "Joe Shmoe";
    public string title = "Really broken proc mesh";
    internal MeshFilter _filter;
    internal MeshSenderHTTP _sender;

    void Awake () {
        _filter = GetComponent<MeshFilter>();
        _sender = GetComponent<MeshSenderHTTP>();
        _sender.Construct(useLocal ? localServerUrl : remoteServerUrl, authorName, title);
        _sender.Register();

        InvokeRepeating("SendMesh", 0.2f, 0.05f);
    }

    void SendMesh()
    {
        if(_sender.wwwcall != null && !_sender.wwwcall.IsDone) return;

        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        //Debug.Log("SendMesh " + meshFilters.Length);

        CombineInstance[] combine = new CombineInstance[meshFilters.Length];
        int i = 0;
        while (i < meshFilters.Length)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
          
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            //meshFilters[i].gameObject.SetActive( false );
            i++;
        }

        Mesh combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combine, true, true);
        combinedMesh.RecalculateBounds();
        combinedMesh.RecalculateNormals();
        
        Color32[] colors = new Color32[combinedMesh.vertexCount];
        for(  i = 0; i < combinedMesh.vertexCount; i++)
        {
            colors[i] = new Color32(255, 255, 255, 255);
        }
        combinedMesh.colors32 = colors;
       
        _sender.SetMesh(combinedMesh);
    }

     void Update () {

        
	}

}
