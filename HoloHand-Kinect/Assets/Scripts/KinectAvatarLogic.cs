using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class KinectAvatarLogic : NetworkBehaviour {

    public static KinectAvatarLogic MyAvatar;
    public bool robustTracking = false;
    public float confidence;
    public bool previousRobustTracking = false;
    public float jumpLimit;
    public int minHandPoints;

	// Use this for initialization
	void Start () {
		if (isLocalPlayer)
        {
            MyAvatar = this;
        }
	}

    struct sTransform
    {
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
    }

    public void PlaceAvatar(List<Vector3> handPoints, Vector3 kinectLocation)
    {
        Debug.Log("[KinectAvatarLogic:PlaceAvatar] provided " + handPoints.Count + " points");
        //if we don't have very many points, then it is just noise that we should ignore
        robustTracking = (handPoints != null && handPoints.Count >= minHandPoints);
        if (!robustTracking)
        {
            if (previousRobustTracking)
            {
                //if we have bad tracking this frame, but had good tracking last frame...
                //lets leave the point be for one frame in case this is a hiccup
                confidence = 0.5f;
            } else
            {
                //if we have had bad tracking for the last 2 frames...
                //set the point to the kinect's location for "safe keeping"
                //(keep it out of the way, and provides a way to verify kinect alignment)
                transform.position = kinectLocation;
                confidence = 0;
            }
        } else
        {
            sTransform computedPoint = getComputedPoint(handPoints);
            if (previousRobustTracking && 
                jumpLimit > 0 &&
                Vector3.Distance(transform.position, computedPoint.position) > jumpLimit)
            {
                //if we had good tracking last frame, but the point moved "a lot" between frames
                //we'll assume it is due to noise.
                //NOTE: this may be overkill, since single-frame noise would be likely to result
                //      in a small point list which would end up in the above if block...?
                robustTracking = false;
                confidence = 0.5f;
            } else
            {
                //if we have good tracking, track the point!
                transform.position = computedPoint.position;
                confidence = 1;
            }
        }
        
        previousRobustTracking = robustTracking;
    }

    /**
     * calculate the "interaction" point based on all "hand" points
     */
    private sTransform getComputedPoint(List<Vector3> points)
    {
        sTransform transform;
        transform.scale = Vector3.one;
        transform.position = Vector3.zero;
        transform.rotation = Vector3.zero;

        //position is a simple average of the points
        foreach(Vector3 point in points)
        {
            transform.position += point;
        }
        transform.position /= points.Count;

        //TODO: rotation will be based on a "best fit" plane for all the points
        // http://stackoverflow.com/questions/29356594/fitting-a-plane-to-a-set-of-points-using-singular-value-decomposition

        return transform;
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
