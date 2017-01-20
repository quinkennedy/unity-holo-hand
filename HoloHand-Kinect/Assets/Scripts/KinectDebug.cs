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
        configuration = new KinectConfig("config.json");

        KinectDepth.Init(configuration.kinect_pos, configuration.kinect_rot, configuration.depthDistance, configuration.buttons);

        GameObject kinectBounds = GameObject.Find("Cube_001");
        Vector3 pos = configuration.kinect_bounds_pos;
    
        kinectBounds.transform.localScale = configuration.kinect_bounds_scale;
        kinectBounds.transform.localPosition = pos;
        
    }

    // Update is called once per frame

    void Update()
    {

        if (Input.GetKey(KeyCode.A))
        {
            KinectDepth.DepthSourceManager.GetComponent<DepthSourceManager>().maxZ = 4500;

            GameObject kinectBounds = GameObject.Find("Cube_001");
            kinectBounds.transform.localScale = Vector3.one * 4.5f;
        }

        if (Input.GetKey(KeyCode.R))
        {
            KinectDepth.DepthSourceManager.GetComponent<DepthSourceManager>().maxZ = configuration.depthDistance * 1000.0f;

            GameObject kinectBounds = GameObject.Find("Cube_001");
            Vector3 pos = configuration.kinect_bounds_pos;
            
            kinectBounds.transform.localScale = configuration.kinect_bounds_scale;
            kinectBounds.transform.localPosition = pos;
        }
    }
}
