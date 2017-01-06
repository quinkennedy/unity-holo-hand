using UnityEngine;
using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class MeshSerializer
{

	private static readonly byte[] marker = Encoding.ASCII.GetBytes("MESHDATA");
    public byte[] packet;
    internal int packetSize = 0;

    int[] triangles;
    int trianglesCount;

    int vertexCount;
    int positionCount;
    int colorCount;
    int indexCount;
    int headerDataByteCount;
    int positionDataByteCount;
    int colorDataByteCount;
    int indexDataByteCount;

    public bool Serialize(int [] triangles, Vector3[] vertices,  Color32[] colors )
	{

        trianglesCount = triangles.Length;
        vertexCount = vertices.Length;
        colorCount = colors.Length;

        positionCount = vertexCount;
        indexCount = trianglesCount;

        Debug.LogFormat("triangles {0} vertex {1} color {2}", trianglesCount, vertexCount, colorCount);
        
        if (trianglesCount < 10)
        {
            return false;
        }
        
        // We should check here to see if we need to update the byte array size and whatnot
        //if (packetSize == 0)
        //{
            headerDataByteCount = 16;
            positionDataByteCount = positionCount * 3 * 4;
            colorDataByteCount = colorCount * 3;
            indexDataByteCount = indexCount * 3 * 2;

            packetSize = headerDataByteCount + positionDataByteCount + colorDataByteCount + indexDataByteCount;

            packet = new byte[packetSize];
            Debug.LogFormat("Made packet {0}", packetSize );
        //}


        Array.Copy(marker, 0, packet, 0, marker.Length);

		int endOfMarker = marker.Length;
		putInt16(packet, endOfMarker, positionCount);
		putInt16(packet, endOfMarker + 2, colorCount);
		putInt16(packet, endOfMarker + 4, indexCount / 3);

		int positionDataStart = 16;
		int colorDataStart = positionDataStart + positionDataByteCount;
		int indexDataStart = colorDataStart + colorDataByteCount;
        
		for (int i = 0; i < vertexCount; i++)
		{
            // Encode and inject color data
            Vector3 position = vertices[i];
			putFloat(packet, positionDataStart + (i * 12), position.x);
			putFloat(packet, positionDataStart + (i * 12) + 4, position.y);
			putFloat(packet, positionDataStart + (i * 12) + 8, position.z);
            
            // Encode and inject color data
            Color32 vertexColor = colors[i];

            byte r = (vertexColor.r);
			byte g = (vertexColor.g);
			byte b = (vertexColor.b);

			packet[colorDataStart + (i * 3)] = r;
			packet[colorDataStart + (i * 3) + 1] = g;
			packet[colorDataStart + (i * 3) + 2] = b;
		}
        
		for (int i = 0; i < triangles.Length; i++)
		{
			putInt16(packet, indexDataStart + (i * 2), triangles[i]);
		}
        //	File.WriteAllBytes("./test.mesh", packet);

        return true;
	}

	void putInt16(byte[] destination, int offset, int value)
	{
		byte firstByte = (byte)(value & 0xff);
		byte secondByte = (byte)((value >> 8) & 0xff);

		destination[offset] = firstByte;
		destination[offset + 1] = secondByte;
	}

	void putFloat(byte[] destination, int offset, float value)
	{

		byte[] bytes = BitConverter.GetBytes(value);

		destination[offset] = bytes[0];
		destination[offset + 1] = bytes[1];
		destination[offset + 2] = bytes[2];
		destination[offset + 3] = bytes[3];
	}
}
