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
    public float[] kinect_rotation;
    public float[] kinect_anchor_offset;
    public float[] kinect_bounds_pos;
    public float[] kinect_bounds_scale;
    public Box hmd_active_area;
    public Box[] buttons;

    [System.Serializable]
    public struct Box
    {
        public float[] pos;
        public float[] scale;
        public float[] rot;

        //Getters
        public Vector3 Position
        {
            get { return new Vector3(pos[0], pos[1], pos[2]); }
        }
        public Vector3 Scale
        {
            get { return new Vector3(scale[0], scale[1], scale[2]); }
        }
        public Vector3 Rotation
        {
            get { return new Vector3(rot[0], rot[1], rot[2]); }
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
        get { return new Vector3(kinect_rotation[0], kinect_rotation[1], kinect_rotation[2]); }
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
