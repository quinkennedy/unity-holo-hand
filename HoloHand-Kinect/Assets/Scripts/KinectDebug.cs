using UnityEngine;
using System.Collections;

public class KinectDebug : MonoBehaviour {

    Config configuration;

    public DepthSourceView KinectDepth;
    private MovingAverage depthMeshTriangles;

    void Start()
    {
        depthMeshTriangles = new MovingAverage();
        depthMeshTriangles.Period = 10;
    }

    // Use this for initialization
    void Awake () {
        configuration = new Config("config.json");

        KinectDepth.Init(configuration.kinect_pos, configuration.kinect_rot, configuration.depthDistance, configuration.buttons);

        GameObject kinectBounds = GameObject.Find("Cube_001");
        Vector3 pos = configuration.kinect_bounds_pos;
        //pos.x += 0.04f;
        //pos.y += 1.0f;
        //pos.z += (configuration.kinect_bounds_scale.z / 2.0f);
        //pos.z *= -1;


        kinectBounds.transform.localScale = configuration.kinect_bounds_scale;
        kinectBounds.transform.localPosition = pos;
        
    }
    
    // Update is called once per frame
    void Update () {        
        //Debug.LogFormat("{0},{1},{2}", KinectMic.EnergyAchieved(), KinectMic.MaxEnergyAchieved(), KinectMic.Listening() );
	}
}
