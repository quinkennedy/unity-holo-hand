using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Windows.Kinect;
using System.Threading;
using System.Linq;

public struct MeshData
{
    public int[] Triangles;
    public Vector3[] Vertices;
}


public class DepthSourceManager : MonoBehaviour
{
    public float maxZ = 4500.0f;
    public float minAngle = 92.0f;
    public float maxMagnitude = 25.0f;
    
    private KinectSensor _Sensor;
    private DepthFrameReader _Reader;
    CoordinateMapper mapper;

    private ushort[] _Data;
    private CameraSpacePoint[] _DataCamera;
    bool newData = false;

    object _DataLock = new object();
    private Vector3[] _Vertices;

    private int[] _Triangles;

    protected System.Threading.Timer updateDataTimer;

    //max size of mesh vertices is 64k. 512x424 depth points generates 200k vertices. need to down size
    //TODO: figure out submeshes so that you can use native resolution
    private const int DownsampleSize = 4;

    public MeshData GetData()
    {
        MeshData result = new MeshData();
        if (Monitor.TryEnter(_DataLock, 15))
        { 
            try
            {
                if (_Vertices != null)
                {
                    result.Vertices = new Vector3[_Vertices.Length];
                    _Vertices.CopyTo(result.Vertices, 0);
                }

                if (_Triangles != null)
                {
                    result.Triangles = new int[_Triangles.Length];
                    _Triangles.CopyTo(result.Triangles, 0);
                }
            }
            finally
            {
                Monitor.Exit(_DataLock);
            }
        }
        return result;
    }
    
    public void Init()
    {

        _Sensor = KinectSensor.GetDefault();

        if (_Sensor != null)
        {
            _Reader = _Sensor.DepthFrameSource.OpenReader();

            _Data = new ushort[_Sensor.DepthFrameSource.FrameDescription.LengthInPixels];
            _DataCamera = new CameraSpacePoint[_Sensor.DepthFrameSource.FrameDescription.LengthInPixels];

            mapper = _Sensor.CoordinateMapper;

            if (!_Sensor.IsOpen)
            {
                _Sensor.Open();
            }

        }else
        {
            Debug.LogError("DepthSourceManager: No Kinect Sensor");
        }


        updateDataTimer = new System.Threading.Timer(UpdateData, null, 1000, 60);
    }

    public bool IsConnected()
    {
        return (_Sensor != null);
    }
   
    void Start () 
    {
        
    }
    
    void Update()
    {        
        if (_Reader != null)
        {
            var frame = _Reader.AcquireLatestFrame();
            ProcessFrame(frame);            
        }
    }

    void ProcessFrame( DepthFrame frame)
    {
        if (frame != null)
        {
            if (Monitor.TryEnter(_DataLock, 15))
            {
                try
                {
                    frame.CopyFrameDataToArray(_Data);
                    newData = true;
                }
                finally
                {
                    Monitor.Exit(_DataLock);
                }
            }

            frame.Dispose();
            frame = null;
        }
    }

    void OnApplicationQuit()
    {
        if (updateDataTimer != null) updateDataTimer.Dispose();
        
        if (_Reader != null)
        {
            _Reader.Dispose();
            _Reader = null;
        }
        
        if (_Sensor != null)
        {
            if (_Sensor.IsOpen)
            {
                _Sensor.Close();
            }
            
            _Sensor = null;
        }
    }



    //thread from here on

    private void UpdateData(System.Object stateInfo)
    {

        if (Monitor.TryEnter(_DataLock, 15))
        {
            try
            {
                if( newData) { 
                    RefreshDataCamera();
                    newData = false;
                }
            }
            catch
            {
                Debug.Log("Exception in data processing");
            }
            finally
            {
                Monitor.Exit(_DataLock);
            }
        }
    }

    private void PopulateMeshArray(int width, int height)
    {
        _Vertices = new Vector3[width * height];
        _Triangles = new int[6 * ((width - 1) * (height - 1))];

        int triangleIndex = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;

                _Vertices[index] = new Vector3(0, 0, 4.5f);

                // Skip the last row/col
                if (x != (width - 1) && y != (height - 1))
                {
                    int topLeft = index;
                    int topRight = topLeft + 1;
                    int bottomLeft = topLeft + width;
                    int bottomRight = bottomLeft + 1;

                    _Triangles[triangleIndex++] = topLeft;
                    _Triangles[triangleIndex++] = topRight;
                    _Triangles[triangleIndex++] = bottomLeft;
                    _Triangles[triangleIndex++] = bottomLeft;
                    _Triangles[triangleIndex++] = topRight;
                    _Triangles[triangleIndex++] = bottomRight;
                }
            }
        }
    }


    private void RefreshDataCamera()
    {
        var frameDesc = _Sensor.DepthFrameSource.FrameDescription;

        //map the depth to the camera coordinate system
        mapper.MapDepthFrameToCameraSpace(_Data, _DataCamera);
        
        PopulateMeshArray(frameDesc.Width / DownsampleSize, frameDesc.Height / DownsampleSize);
        
        for (int y = 0; y < frameDesc.Height; y += DownsampleSize)
        {
            for (int x = 0; x < frameDesc.Width; x += DownsampleSize)
            {
                int indexX = x / DownsampleSize;
                int indexY = y / DownsampleSize;
                int smallIndex = (indexY * (frameDesc.Width / DownsampleSize)) + indexX;

                int fullIndex = (y * frameDesc.Width) + x;
                Vector3 pos = new Vector3();
                CameraSpacePoint raw = _DataCamera[fullIndex];

                bool invalidFound = false;
                if (!float.IsInfinity( raw.X))
                {
                    pos.x = raw.X;
                }
                else
                {
                    invalidFound = true;
                }

                if (!float.IsInfinity(raw.Y))
                {
                    pos.y = raw.Y;
                }
                else
                {
                    invalidFound = true;
                }


                if (!float.IsInfinity(raw.Z))
                {
                    pos.z = raw.Z;
                }
                else
                {
                    invalidFound = true;
                }

                
                if (!invalidFound) {
                    _Vertices[smallIndex] = pos;
                }
            }
        }


        List<int> tris = new List<int>();
        for (int i = 0; i < _Triangles.Length; i += 3)
        {
            Vector3 A = _Vertices[_Triangles[i]];
            Vector3 B = _Vertices[_Triangles[i + 1]];
            Vector3 C = _Vertices[_Triangles[i + 2]];

            Vector3 V = Vector3.Cross(A - B, A - C);

            //figure out the direction the triangle is pointing in
            Vector3 mN = V.normalized;
            float an = Vector3.Angle(mN, Vector3.forward);

            float maxDepth = maxZ / 1000;
       
            if ( an > minAngle && A.z < maxDepth && B.z < maxDepth && C.z < maxDepth)
            {
                tris.Add(_Triangles[i]);
                tris.Add(_Triangles[i + 1]);
                tris.Add(_Triangles[i + 2]);
            }
        }
        
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
                verts.Add(_Vertices[vertIndex]);
                lookup.Add(vertIndex, verts.Count-1);
                tris[i] = verts.Count-1;
            }
        }
        
        _Vertices = verts.ToArray();
        _Triangles = tris.ToArray();
        
        //Debug.LogFormat("Vertice count {0}, Triangle count {1} ", _Vertices.Length, tris.Count );
    }
    
}
