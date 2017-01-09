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
    public bool enableRotation = true;

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

#if UNITY_STANDALONE
    public void PlaceAvatar(List<Vector3> handPoints, Transform kinectTransform)
    {
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
                transform.position = kinectTransform.position;
                transform.rotation = kinectTransform.rotation;
                //rotate so the plane is parallel to the front face of the Kinect
                transform.rotation *= Quaternion.Euler(90, 0, 0);
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
                transform.eulerAngles = computedPoint.rotation;
                confidence = 1;
            }
        }
        
        previousRobustTracking = robustTracking;
    }

    private Vector3 getCentroid(List<Vector3> points)
    {
        Vector3 centroid = new Vector3();
        //position is a simple average of the points
        foreach (Vector3 point in points)
        {
            centroid += point;
        }
        centroid /= points.Count;

        return centroid;
    }

    private double[,] subtractCentroid(List<Vector3> points, Vector3 centroid)
    {
        double[,] translated = new double[3, points.Count];

        for(int i = 0; i < points.Count; i++)
        {
            Vector3 tp = points[i] - centroid;
            translated[0, i] = tp.x;
            translated[1, i] = tp.y;
            translated[2, i] = tp.z;
        }

        return translated;
    }

    // http://stackoverflow.com/questions/29356594/fitting-a-plane-to-a-set-of-points-using-singular-value-decomposition
    private Vector3 getBestFitRotation(List<Vector3> points, Vector3 centroid)
    {
        double[,] dataMat = subtractCentroid(points, transform.position);
        double[] w = new double[3];
        double[,] u = new double[3, 3];
        double[,] vt = new double[1, 1];

        // arg1: points relative to centroid
        // arg2: rows in datMat (3 because we are dealing with 3d points)
        // arg3: columns in datMat
        // arg4: 1 because we want the "left singular vectors"
        // arg5: 0 because we don't care about the "right singular vectors"
        // arg6: 2 because we have extra memory and want maximum performance
        // arg7-9: references for return data
        bool a = alglib.svd.rmatrixsvd(dataMat, 3, points.Count, 1, 0, 2, ref w, ref u, ref vt);

        return new Vector3((float)u[0, 0] * 180 / Mathf.PI, 
                           (float)u[1, 0] * 180 / Mathf.PI, 
                           (float)u[2, 0] * 180 / Mathf.PI);
    }

    /**
     * calculate the "interaction" point based on all "hand" points
     */
    private sTransform getComputedPoint(List<Vector3> points)
    {
        sTransform transform;
        //don't worry about adjusting scale
        transform.scale = Vector3.one;
        //position is a simple average of the points
        transform.position = getCentroid(points);

        if (enableRotation)
        {
            //rotation will be based on a "best fit" plane for all the points
            transform.rotation = getBestFitRotation(points, transform.position);
        } else
        {
            transform.rotation = Vector3.zero;
        }

        return transform;
    }
#endif
	
	// Update is called once per frame
	void Update () {
		
	}
}
