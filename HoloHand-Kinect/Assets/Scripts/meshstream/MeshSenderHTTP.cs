using UnityEngine;
using MiniJSON;
using System;
using System.Collections;
using System.Text;
using System.Collections.Generic;

using UnityEngine.Networking;
using System.Net;

public class RequestWWW
{
	public bool IsDone
	{
		get;
		private set;
	}

	public string Text
	{
		get;
		private set;
	}

	public string Error
	{
		get;
		private set;
	}

    Dictionary<string, string> headers;

	public IEnumerator doHttpPost(string url, byte[] payload, int meshKey)
	{
        Debug.LogFormat("Posting at {0}", Time.frameCount);
        // Create or clear a dictionary for the headers
		if(headers == null)
        {
            headers = new Dictionary<string, string>();
        } else
        {
            headers.Clear();
        }

		headers.Add("Content-Type", "application/octet-stream");
		headers.Add("Content-Length", payload.Length.ToString());

		if (meshKey != -1)
		{
			headers.Add("slot-key", meshKey.ToString());
		}

		WWW www = new WWW(url, payload, headers);
		yield return www;
		IsDone = true;
		Error = www.error;
		Text = www.text;
        
	}
}

public class MeshSenderHTTP : MonoBehaviour {
	private MeshSerializer serializer = new MeshSerializer();
    public RequestWWW wwwcall;

	private static string rootServerUrl = "";
	private static string authorName = "";
	private static string title = "";

	private static bool isRegistered = false;

	private static bool needsToSend = true;

	private static int meshSlot = -1;
	private static int meshKey = 0;

    //private static Mesh meshToSend;
    private object meshDataLock;

    private static int[] meshTriangles;
    private static Vector3[] meshVerts;
    private static Color32[] meshColor;

    protected System.Threading.Timer sendDataTimer;

    public void SetMesh(Mesh _mesh)
    {
        //meshToSend = _mesh;
        if (meshDataLock == null) meshDataLock = new object();

        //we only need the data, not the full mesh.
        //also we want to access this data in a seperate thread to serialize and upload it
        lock (meshDataLock) { 
            meshTriangles = new int[_mesh.triangles.Length];
            _mesh.triangles.CopyTo(meshTriangles, 0);

            meshVerts = new Vector3[_mesh.vertices.Length];
            _mesh.vertices.CopyTo(meshVerts, 0);

            meshColor = new Color32[_mesh.colors32.Length];
            _mesh.colors32.CopyTo(meshColor, 0);
        }
    }

	public void Construct(string _rootServerUrl, string _authorName, string _title, Mesh _mesh = null)
	{
		rootServerUrl = _rootServerUrl;
		authorName = _authorName;
		title = _title;
		//meshToSend = _mesh;
	}

	public void Register() {
		var regMsg = new Dictionary<string, object>();

		regMsg.Add("author", authorName);
		regMsg.Add("title", title);
		regMsg.Add("platform", "Unity3D");

        byte[] registration = Encoding.UTF8.GetBytes(Json.Serialize(regMsg));

		wwwcall = new RequestWWW();
		StartCoroutine(wwwcall.doHttpPost(rootServerUrl + "/mesh/register", registration, -1));
	}

	void Update () {

		if (wwwcall != null && wwwcall.IsDone && meshKey == 0 && isRegistered == false)
		{
			if (wwwcall.Error != null)
			{
				Debug.Log("error registering: " + wwwcall.Error);
			}
			else
			{
                
				var result = Json.Deserialize(wwwcall.Text) as Dictionary<string, object>;

				if ((bool)result["result"])
				{
					isRegistered = true;
					meshKey = (int)((long)result["key"]);
					meshSlot = (int)((long)result["index"]);

                    if( sendDataTimer == null) { 
                        sendDataTimer = new System.Threading.Timer(SendData, null, 1000, 100);
                    }
                }
				else
				{
					isRegistered = false;
					Debug.Log("Unable to register: " + (string)result["error"]);
				}
                
			}
			wwwcall = null;
		}
	
        //Debug.LogFormat("meshKey {0} isRegistered {1} needsToSend {2} meshToSend {3} wwwcall{4}", meshKey, isRegistered, needsToSend, meshToSend, wwwcall);
        
	}

    void OnApplicationQuit()
    {
        if (sendDataTimer != null) sendDataTimer.Dispose();
    }

    //thread from here on, much faster to upload data in a thread than on the Update cycle
    void UploadMesh(string url, byte[] payload, int meshKey)
    {
        Debug.Log("UploadMesh "+ url );
        
        WebClient client = new WebClient();
        client.Encoding = Encoding.UTF8;

        client.Headers.Set(HttpRequestHeader.ContentType, "application/octet-stream");
        //client.Headers.Set(HttpRequestHeader.ContentLength, payload.Length.ToString());
        client.Headers.Add("slot-key", meshKey.ToString());

        try
        {
            //UploadData implicitly sets HTTP POST as the request method.
            client.UploadData("http://" + url, payload);
        }
        catch(WebException e)
        {
            Debug.LogError("Exception " + e );
        }
    }

    private void SendData(object state)
    {
        if (meshKey != 0 && isRegistered == true && needsToSend == true ) {
            
            bool send = false;

            lock (meshDataLock)
            {
                send = serializer.Serialize(meshTriangles, meshVerts, meshColor);
            }            
            if ( send )
            {
                UploadMesh(rootServerUrl + "/mesh/" + meshSlot + "/frame", serializer.packet, meshKey);
            }
        }
    }
}
