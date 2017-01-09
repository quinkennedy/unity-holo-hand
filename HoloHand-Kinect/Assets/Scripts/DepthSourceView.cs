using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Windows.Kinect;
using System.Linq;
using System;
public class DepthSourceView : MonoBehaviour
{ 
    
    public GameObject DepthSourceManager;
    public BoxCollider boxBounds;
    public Material buttonMaterial;

    //the distance around the "furthest point" to gather "hand" points from
    // used in populating FurthestPointBucket.furthestPoints
    public float pointBucketDistance;

    private List<BoxCollider> buttons = new List<BoxCollider>();

    private Mesh _Mesh;
    MeshCollider _Collider;
    
    private DepthSourceManager _DepthManager;

    private float deltaTime = 0;//used in update as a throttle
    private float updateFrequency = 0.03f;//how often to update in seconds

    private Color32[] colors;

    void Start()
    {
    }

    public void Init( Vector3 pos, Vector3 rot, float distance, List<Config.Box> buttons )
    {
        _Mesh = new Mesh();
        _Mesh.name = "DynamicKinectMesh";
        _Collider = GetComponent<MeshCollider>();

        colors = new Color32[54272];
        for (int i = 0; i < colors.Count(); i++)
        {
            colors[i] = new Color32(255, 255, 255, 255);
        }

        GetComponent<MeshFilter>().mesh = _Mesh;
        _DepthManager = DepthSourceManager.GetComponent<DepthSourceManager>();
        _DepthManager.maxZ = distance * 1000.0f;

        _DepthManager.Init();
        
        transform.position = pos;
        transform.rotation = Quaternion.Euler(rot);

        foreach(Config.Box buttonData in buttons)
        {
            createButton(buttonData);
        }
    }

    private void createButton(Config.Box buttonData)
    {
        GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube);
        button.GetComponent<MeshRenderer>().enabled = true;
        button.GetComponent<MeshRenderer>().material = buttonMaterial;
        button.transform.SetParent(transform.parent, false);
        button.transform.localPosition = buttonData.position;
        button.transform.localScale = buttonData.scale;
        button.transform.localEulerAngles = buttonData.rotation;
        buttons.Add(button.GetComponent<BoxCollider>());
    }
    
    public int GetMeshTriangleCount()
    {
        return _Mesh.triangles.Count(); ;
    }
    
    void Update()
    {
        if (_DepthManager == null) return;

        //only process every 500 ms
        if ( deltaTime < updateFrequency)
        {
            deltaTime += Time.deltaTime;
            return;
        }
        else
        {
            deltaTime = 0;
        }

        MeshData d = _DepthManager.GetData();
        if( d.Triangles != null && d.Vertices != null ) {
            ShowInBounds(d);
        }
        else
        {
            //if the kinect doesn't see anything, 
            //just fall back to setting the point to the kinect's position
            KinectAvatarLogic.MyAvatar.PlaceAvatar(null, transform);
        }

    }

    void ShowRaw( MeshData d)
    {
        //Debug.Log("Raw " + d.Triangles.Length);
        _Mesh.Clear();

        _Mesh.vertices = d.Vertices;
        _Mesh.triangles = d.Triangles;

        _Mesh.RecalculateNormals();
        _Mesh.RecalculateBounds();

        _Collider.sharedMesh = null;
        _Collider.sharedMesh = _Mesh;

        Vector3 pos = _Collider.gameObject.transform.localPosition;
        _Collider.gameObject.transform.localPosition = pos;
        
    }

    class FurthestPointBucket
    {
        public Vector3 furthestPoint = Vector3.zero;
        public List<Vector3> furthestPoints = new List<Vector3>();
        public float maxDistance = float.MinValue;
        public int numPoints = 0;
    }

    /**
     * remove Vectors from the list of points which are too far from the target
     */
    private void filterPointsAround(List<Vector3> points, Vector3 target)
    {
        //move all point in-bounds to beginning of list
        int nextValid = 0;
        for(int i = 0; i < points.Count; i++)
        {
            if (Vector3.Distance(points[i], target) <= pointBucketDistance)
            {
                points[nextValid] = points[i];
                nextValid++;
            }
        }
        //chop off the end of the list (which either contains duplicates
        // or points which are too far away
        points.RemoveRange(nextValid, points.Count - nextValid);
    }
    
    void ShowInBounds( MeshData d)
    {

        if (d.Vertices != null && d.Triangles != null)
        {
            //
            //  Initialize!
            _Mesh.Clear();
            
            List<int> tris = new List<int>();

            FurthestPointBucket[] pointBucket = new FurthestPointBucket[buttons.Count];
            for(int i = 0; i < pointBucket.Length; i++)
            {
                pointBucket[i] = new FurthestPointBucket();
            }

            //get the currently active HoloLens position in plan view
            Vector2 HMDposition = Vector2.zero;
            if (KinectRegistration.closestHMD != null)
            {
                HMDposition = new Vector2(KinectRegistration.closestHMD.position.x, KinectRegistration.closestHMD.position.z);
            }

            //
            //  filter triangles based on a large "presence" bounding box
            for (int i = 0; i < d.Triangles.Length; i += 3)
            {

                Vector3 A = d.Vertices[d.Triangles[i]];
                Vector3 B = d.Vertices[d.Triangles[i + 1]];
                Vector3 C = d.Vertices[d.Triangles[i + 2]];

                //Vector3 v = Vector3.Cross(A - B, A - C);

                Vector3 vL = transform.TransformPoint(A);

                if (PointInOABB(vL, boxBounds))
                {
                    tris.Add(d.Triangles[i]);
                    tris.Add(d.Triangles[i + 1]);
                    tris.Add(d.Triangles[i + 2]);

                    //if we have a registered HoloLens,
                    // find the furthest point in plan view
                    // that is inside one of the defined buttons
                    if (KinectRegistration.closestHMD != null)
                    {
                        for (int j = 0; j < buttons.Count; j++)
                        {
                            BoxCollider button = buttons[j];
                            //if the point is in one of the buttons
                            if (PointInOABB(vL, button))
                            {
                                //count it
                                pointBucket[j].numPoints++;
                                //add all points in the button to the "furthestPoints" list
                                // we will filter this list later
                                pointBucket[j].furthestPoints.Add(vL);

                                //keep tabs on which individual point is furthest (in plan view) from the HoloLens
                                /**
                                 * distance in plan view (x,z) relative to currently active HoloLens
                                 */
                                float currDistance = Vector2.Distance(HMDposition, new Vector2(vL.x, vL.z));
                                if (currDistance > pointBucket[j].maxDistance)
                                {
                                    pointBucket[j].furthestPoint = vL;
                                    pointBucket[j].maxDistance = currDistance;
                                }
                                break;
                            }
                        }
                    }
                }
            }

            //Debug.Log("Tris " + tris.Count +  " " +  d.Triangles.Length + " " + (d.Triangles.Length - tris.Count) );

            //map all the valid vertices
            Dictionary<int, int> lookup = new Dictionary<int, int>();
            List<Vector3> verts = new List<Vector3>();

            //reindex the triangles
            for (int i = 0; i < tris.Count; i++)
            {
                int vertIndex = tris[i];

                int newIndex = -1;
                if (lookup.TryGetValue(vertIndex, out newIndex))
                {
                    tris[i] = newIndex;
                }
                else
                {
                    verts.Add(d.Vertices[vertIndex]);
                    lookup.Add(vertIndex, verts.Count - 1);
                    tris[i] = verts.Count - 1;
                }
            }


            //Debug.Log("Verts " + verts.Count + " " + d.Vertices.Length + " " + (d.Vertices.Length - verts.Count) );


            _Mesh.vertices = verts.ToArray();
            _Mesh.triangles = tris.ToArray();
            
            _Mesh.RecalculateNormals();
            _Mesh.RecalculateBounds();

            //_Collider.sharedMesh = null;
            //_Collider.sharedMesh = _Mesh;

            //Vector3 pos = _Collider.gameObject.transform.localPosition;
            //_Collider.gameObject.transform.localPosition = pos;

            //
            //  Set reference point based the button with the most points in it
            //first find which button has the most data in it
            FurthestPointBucket selectedButton = null;
            foreach(FurthestPointBucket button in pointBucket)
            {
                if (selectedButton == null || selectedButton.numPoints < button.numPoints)
                {
                    selectedButton = button;
                }
            }
            //now we actually filter what we think are "hand" points based on the furthest point
            if (selectedButton != null) {
                filterPointsAround(selectedButton.furthestPoints, selectedButton.furthestPoint);
            }
            //and pass the "hand" data to the networked avatar
            KinectAvatarLogic.MyAvatar.PlaceAvatar(selectedButton.furthestPoints, transform);
        }
    }

    bool PointInOABB(Vector3 point, BoxCollider box)
    {
        //is point in a bounding box
        point = box.transform.InverseTransformPoint(point) - box.center;
        
        float halfX = (box.size.x * 0.5f);
        float halfY = (box.size.y * 0.5f);
        float halfZ = (box.size.z * 0.5f);
        if (point.x < halfX && point.x > -halfX &&
           point.y < halfY && point.y > -halfY &&
           point.z < halfZ && point.z > -halfZ)
            return true;
        else
            return false;
    }

    private void OnCollisionStay(Collision collision)
    {
        Debug.Log("OnCollisionStay");
        foreach (ContactPoint contact in collision.contacts)
        {
            Debug.Log(contact.thisCollider.name + " hit " + contact.otherCollider.name);
            Debug.DrawRay(contact.point, contact.normal, Color.white);
        }
        
        //Vector3 local_vec = transform.InverseTransformPoint(contact.point);

    }


    void OnDrawGizmosSelected()
    {
        //draw the Kinect FOV, the fustrum is not acurate at all but helps give a general idea of what the kinect is looking at
        Gizmos.color = Color.yellow;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawFrustum(Vector3.zero, 60.0f, 3.0f, 0.4f, 1.2f);
    }
}
