using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public class KinectConfig
{
    public float kinect_height;
    public int kinect_threshold = 3000;
    public float kinect_distance = 2.5f;
    public float[] kinect_rot;
    public float[] kinect_anchor_offset;
    public float[] kinect_bounds_pos;
    public float[] kinect_bounds_scale;
    public Box HMD_active_area;
    public List<Box> buttons;

    [System.Serializable]
    public struct Box
    {
        public float[] position;
        public float[] scale;
        public float[] rotation;

        //Getters
        public Vector3 Position
        {
            get { return new Vector3(position[0], position[1], position[2]); }
        }
        public Vector3 Scale
        {
            get { return new Vector3(scale[0], scale[1], scale[2]); }
        }
        public Vector3 Rotation
        {
            get { return new Vector3(rotation[0], rotation[1], rotation[2]); }
        }
    }

    //Getters
    public float KinectHeight
    {
        get { return kinect_height; }
    }
    public int KinectDepthThreshold
    {
        get { return kinect_threshold; }
    }
    public float KinectDepthDistance
    {
        get { return kinect_distance; }
    }
    public Vector3 KinectRotation
    {
        get { return new Vector3(kinect_rot[0], kinect_rot[1], kinect_rot[2]); }
    }
    public Vector3 KinectAnchor
    {
        get { return new Vector3(kinect_anchor_offset[0], kinect_anchor_offset[1], kinect_anchor_offset[2]); }
    }
    public Vector3 KinectBoundsPosition
    {
        get { return new Vector3(kinect_bounds_pos[0], kinect_bounds_pos[1], kinect_bounds_pos[2]); }
    }
    public Vector3 KinectBoundsScale
    {
        get { return new Vector3(kinect_bounds_scale[0], kinect_bounds_scale[1], kinect_bounds_scale[2]); }
    }

    public static KinectConfig CreateFromJSON(string path)
    {
        try
        {
            string configJson = File.ReadAllText(path);
            return JsonUtility.FromJson<KinectConfig>(configJson);

        }
        catch (System.Exception e)
        {
            Debug.LogError("Error loading config.json -- " + e.Message);
            Debug.LogError(e.StackTrace);
            return null;
        }
    }
}
