using UnityEngine;
using System.Collections;
using System.IO;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MeshStreaming : MonoBehaviour
{

	public int gridSize = 10;
	public string serverUrl = "localhost:8080";
	float scale = 1.0f;
	float noiseScale = 0.5f;
    public string authorName = "Joe Shmoe";
    public string title = "Really broken proc mesh";

	public Mesh mesh;
	private Vector3[] vertices;
	private Color[] colors;
	private byte[] testBytes;

	private MeshSenderHTTP meshSender;

	private void Start()
	{
		//GetComponent<MeshFilter>().mesh = mesh = new Mesh();
		meshSender = GetComponent<MeshSenderHTTP>();
		meshSender.Construct(serverUrl, authorName, title, mesh);
		meshSender.Register();
		//mesh.name = "Some Rad Meshy Thing";

		//StartCoroutine(Generate());
	}

	private IEnumerator Generate()
	{
		float timeScale = Time.deltaTime;

		vertices = new Vector3[(gridSize + 1) * (gridSize + 1)];
		colors = new Color[vertices.Length];
		Vector2[] uv = new Vector2[vertices.Length];
		Vector4[] tangents = new Vector4[vertices.Length];
		Vector4 tangent = new Vector4(1f, 0f, 0f, -1f);

		for (int i = 0, y = 0; y <= gridSize; y++)
		{
			for (int x = 0; x <= gridSize; x++, i++)
			{
				colors[i] = new Color(x / gridSize * 255, y / gridSize * 255, 128);
				vertices[i] = new Vector3((x * scale), Mathf.PerlinNoise((x * noiseScale), (y * timeScale)) * scale, y * scale);
				uv[i] = new Vector2((float)x / gridSize, (float)y / gridSize);
				tangents[i] = tangent;
			}
		}
		mesh.Clear();
		mesh.vertices = vertices;
		mesh.colors = colors;

		int[] triangles = new int[gridSize * gridSize * 6];
		for (int ti = 0, vi = 0, y = 0; y < gridSize; y++, vi++)
		{
			for (int x = 0; x < gridSize; x++, ti += 6, vi++)
			{
				triangles[ti] = vi;
				triangles[ti + 3] = triangles[ti + 2] = vi + 1;
				triangles[ti + 4] = triangles[ti + 1] = vi + gridSize + 1;
				triangles[ti + 5] = vi + gridSize + 2;
			}
		}
		mesh.triangles = triangles;

		yield return null;
	}
}