using UnityEngine;
using System.Collections;

public class KinectDebug : MonoBehaviour {

    public static KinectConfig configuration;

    public DepthSourceView KinectDepth;
    private MovingAverage depthMeshTriangles;

    void Start()
    {
        depthMeshTriangles = new MovingAverage();
        depthMeshTriangles.Period = 10;
    }

    // Use this for initialization
    void Awake () {
        configuration = KinectConfig.CreateFromJSON("config.json");

        KinectDepth.Init(configuration.KinectHeight, configuration.KinectRotation, configuration.KinectDepthDistance, configuration.buttons);

        GameObject kinectBounds = GameObject.Find("Cube_001");
        Vector3 pos = configuration.KinectBoundsPosition;
    
        kinectBounds.transform.localScale = configuration.KinectBoundsScale;
        kinectBounds.transform.localPosition = pos;
        
    }

    // Update is called once per frame

    void Update()
    {
        // view All kinect data
        if (Input.GetKey(KeyCode.A))
        {
            KinectDepth.DepthSourceManager.GetComponent<DepthSourceManager>().maxZ = 4500;

            GameObject kinectBounds = GameObject.Find("Cube_001");
            kinectBounds.transform.localScale = Vector3.one * 4.5f;
        }
        // Reset to loaded file settings
        if (Input.GetKey(KeyCode.R))
        {
            KinectDepth.DepthSourceManager.GetComponent<DepthSourceManager>().maxZ = configuration.KinectDepthDistance * 1000.0f;

            GameObject kinectBounds = GameObject.Find("Cube_001");
            Vector3 pos = configuration.KinectBoundsPosition;
            
            kinectBounds.transform.localScale = configuration.KinectBoundsScale;
            kinectBounds.transform.localPosition = pos;
        }
    }
}
