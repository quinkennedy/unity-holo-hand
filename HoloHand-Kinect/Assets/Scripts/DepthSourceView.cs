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
    public List<BoxCollider> buttons;

    private List<List<Vector3>> collidingPoints;

    private Mesh _Mesh;
    MeshCollider _Collider;
    
    private DepthSourceManager _DepthManager;

    private float deltaTime = 0;//used in update as a throttle
    private float updateFrequency = 0.03f;//how often to update in seconds

    private Color32[] colors;

    void Start()
    {
        collidingPoints = new List<List<Vector3>>();
        for(int i = 0; i < buttons.Count; i++)
        {
            collidingPoints.Add(new List<Vector3>());
        }
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
        button.GetComponent<MeshRenderer>().enabled = false;
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
        public float maxDistance = float.MinValue;
        public int numPoints = 0;
    }
    
    void ShowInBounds( MeshData d)
    {

        if (d.Vertices != null && d.Triangles != null)
        {
            //Initialize!
            _Mesh.Clear();

            List<int> tris = new List<int>();

            //foreach(List<Vector3> collidingPointList in collidingPoints)
            //{
            //    collidingPointList.Clear();
            //}

            FurthestPointBucket[] pointBucket = new FurthestPointBucket[buttons.Count];
            for(int i = 0; i < pointBucket.Length; i++)
            {
                pointBucket[i] = new FurthestPointBucket();
            }

            Vector2 HMDposition = Vector2.zero;
            if (KinectRegistration.closestHMD != null)
            {
                HMDposition = new Vector2(KinectRegistration.closestHMD.position.x, KinectRegistration.closestHMD.position.z);
            }

            //DO IT
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
                            if (PointInOABB(vL, button))
                            {
                                pointBucket[j].numPoints++;

                                //relative to currently active HoloLens
                                float currDistance = Vector2.Distance(HMDposition, new Vector2(vL.x, vL.z));
                                if (currDistance > pointBucket[j].maxDistance)
                                {
                                    pointBucket[j].furthestPoint = vL;
                                    pointBucket[j].maxDistance = currDistance;
                                }
                                //collidingPoints[j].Add(vL);
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

            //set reference point based on furthest point
            bool setPoint = false;
            foreach(FurthestPointBucket button in pointBucket)
            {
                if (button.numPoints > 10)
                {
                    KinectAvatarLogic.MyAvatar.transform.position = button.furthestPoint;
                    setPoint = true;
                    break;
                }
            }
            if (!setPoint)
            {
                //set the point to the kinect's location for "safe keeping" 
                //(keep it out of the way, and provides a way to verify kinect alignment)
                KinectAvatarLogic.MyAvatar.transform.position = transform.position;
            }
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
