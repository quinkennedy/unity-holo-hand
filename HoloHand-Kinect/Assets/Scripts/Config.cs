using UnityEngine;
using System.Collections;
using System.IO;

public class Config
{
    
    public Vector3 kinect_pos;
    public Vector3 kinect_rot;
    public Vector3 kinect_bounds_pos;
    public Vector3 kinect_bounds_scale;
    
    public int depthTheshold = 3000;

    public float depthDistance = 2.5f;


    public Config( string path)
    {
        try
        {
            string configJson = File.ReadAllText(path);
            processConfig(configJson);

        }
        catch ( System.Exception e)
        {
            Debug.LogError("Error loading config.json" + e.Message);
        }
    }

    public void processConfig(string jsondata)
    {
        JSONObject obj = new JSONObject(jsondata);

        JSONObject j = obj[0];
        
        j.GetField(ref depthTheshold, "kinect_threshold");

        j.GetField(ref depthDistance, "kinect_distance");
        

        kinect_pos = new Vector3( float.Parse(j["kinect_position"].list[0].ToString()),
                                    float.Parse(j["kinect_position"].list[1].ToString()),
                                    float.Parse(j["kinect_position"].list[2].ToString()));

        kinect_rot = new Vector3(float.Parse(j["kinect_rotation"].list[0].ToString()),
                                    float.Parse(j["kinect_rotation"].list[1].ToString()),
                                    float.Parse(j["kinect_rotation"].list[2].ToString()));

        kinect_bounds_pos = new Vector3(float.Parse(j["kinect_bounds_pos"].list[0].ToString()),
                                    float.Parse(j["kinect_bounds_pos"].list[1].ToString()),
                                    float.Parse(j["kinect_bounds_pos"].list[2].ToString()));

        kinect_bounds_scale = new Vector3(float.Parse(j["kinect_bounds_scale"].list[0].ToString()),
                                    float.Parse(j["kinect_bounds_scale"].list[1].ToString()),
                                    float.Parse(j["kinect_bounds_scale"].list[2].ToString()));

        
    }

}
