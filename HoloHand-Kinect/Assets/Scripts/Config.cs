using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;

public class Config
{

    public struct Box
    {
        public Vector3 position;
        public Vector3 scale;
        public Vector3 rotation;

        public Box(Vector3 position, Vector3 scale, Vector3 rotation)
        {
            this.position = position;
            this.scale = scale;
            this.rotation = rotation;
        }
    }
    
    public Vector3 kinect_pos;
    public Vector3 kinect_rot;
    public Vector3 kinect_bounds_pos;
    public Vector3 kinect_bounds_scale;
    public List<Box> buttons;
    
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
            Debug.LogError("Error loading config.json -- " + e.Message);
            Debug.LogError(e.StackTrace);
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

        buttons = new List<Box>();
        if (j.HasField("buttons")) {
            List<JSONObject> jButtons = j["buttons"].list;
            for (int i = 0; i < jButtons.Count; i++)
            {
                JSONObject button = jButtons[i];
                List<JSONObject> jPos = button["pos"].list;
                Vector3 pos = new Vector3(float.Parse(jPos[0].ToString()),
                                          float.Parse(jPos[1].ToString()),
                                          float.Parse(jPos[2].ToString()));
                List<JSONObject> jScale = button["scale"].list;
                Vector3 scale = new Vector3(float.Parse(jScale[0].ToString()),
                                            float.Parse(jScale[1].ToString()),
                                            float.Parse(jScale[2].ToString()));
                List<JSONObject> jRot = button["rot"].list;
                Vector3 rot = new Vector3(float.Parse(jRot[0].ToString()),
                                          float.Parse(jRot[1].ToString()),
                                          float.Parse(jRot[2].ToString()));
                buttons.Add(new global::Config.Box(pos, scale, rot));
            }
        }
    }

}
