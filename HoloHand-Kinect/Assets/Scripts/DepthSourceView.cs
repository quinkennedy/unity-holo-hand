using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Windows.Kinect;
using System.Linq;
using System;
public class DepthSourceView : MonoBehaviour
{ 
    
    public GameObject DepthSourceManager;
    public BoxCollider boxBoounds;


    private Mesh _Mesh;
    MeshCollider _Collider;
    
    private DepthSourceManager _DepthManager;

    private float deltaTime = 0;//used in update as a throttle
    private float updateFrequency = 0.03f;//how often to update in seconds

    private Color32[] colors;

    void Start()
    {
    }

    public void Init( Vector3 pos, Vector3 rot, float distance )
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
            PositionExtent(d);
        }

    }

    void PositionExtent(MeshData d)
    {
        float maxDistance = float.MinValue;
        Vector3 furthestPoint = Vector3.zero;

        //finding the point furthest (in plan view) from the HoloLens.
        if (KinectRegistration.closestHMD != null)
        {
            Vector2 HMDposition = new Vector2(KinectRegistration.closestHMD.position.x, KinectRegistration.closestHMD.position.z);

            foreach (Vector3 vertex in d.Vertices)
            {
                Vector3 vL = transform.TransformPoint(vertex);
                if (PointInOABB(vL, boxBoounds))
                {
                    float currDistance = Vector2.Distance(HMDposition, new Vector2(vL.x, vL.z));
                    if (currDistance > maxDistance)
                    {
                        furthestPoint = vL;
                        maxDistance = currDistance;
                    }
                }
            }
        }

        if (Vector3.Distance(KinectAvatarLogic.MyAvatar.transform.position, furthestPoint) > .4)
        {
            Debug.Log("moved to " + maxDistance + " away");
        }
        KinectAvatarLogic.MyAvatar.transform.position = furthestPoint;
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
    
    void ShowInBounds( MeshData d)
    {
        if (d.Vertices != null && d.Triangles != null)
        {
            _Mesh.Clear();

            List<int> tris = new List<int>();
            for (int i = 0; i < d.Triangles.Length; i += 3)
            {

                Vector3 A = d.Vertices[d.Triangles[i]];
                Vector3 B = d.Vertices[d.Triangles[i + 1]];
                Vector3 C = d.Vertices[d.Triangles[i + 2]];

                //Vector3 v = Vector3.Cross(A - B, A - C);

                Vector3 vL = transform.TransformPoint(A);

                if (PointInOABB(vL, boxBoounds))
                {
                    tris.Add(d.Triangles[i]);
                    tris.Add(d.Triangles[i + 1]);
                    tris.Add(d.Triangles[i + 2]);
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
        foreach (ContactPoint contact in collision.contacts)
        {
            print(contact.thisCollider.name + " hit " + contact.otherCollider.name);
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
