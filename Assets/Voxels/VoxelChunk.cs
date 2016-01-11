using UnityEngine;
using System.Collections;
using System.IO;
using System;

public class VoxelChunk : MonoBehaviour, MarchingCubes.ByteData
{
    public int size = 16;
    public float scale = 1;

    private byte[,,] data;

    private Mesh mesh;

    public Bounds bounds;

    private Vint3 logicPos;

    public int Length
    {
        get
        {
            return size;
        }
    }

    public byte this[int x, int y, int z]
    {
        get
        {
            return data[x, y, z];

            //var pos = new Vint3(x, y, z) / size;
            //if (pos == logicPos)
            //{
            //    return data[x, y, z];
            //}
            //else
            //{
            //    return 0;
            //}
        }
    }

    // Use this for initialization
    public void Initialize(int size, float scale, Vector3 position, Vint3 intPos)
    {
        this.size = size;
        this.scale = scale;
        logicPos = intPos;
        data = new byte[size, size, size];
        mesh = new Mesh();
        var filter = gameObject.AddComponent<MeshFilter>();
        filter.mesh = mesh;

        transform.localPosition = position;
        bounds = new Bounds(position + new Vector3(size, size, size) * 0.5f, new Vector3(size, size, size));

        //Draw(transform.position + new Vector3(7, 7, 7), 5, 1);
    }

    //void Update()
    //{
    //    for (int x = 0; x < data.GetLength(0); x++)
    //    {
    //        for (int y = 0; y < data.GetLength(1); y++)
    //        {
    //            for (int z = 0; z < data.GetLength(2); z++)
    //            {
    //                data[x, y, z] = (byte)(Random.value > 0.0001f ? data[x, y, z] : data[x, y, z] == 0 ? 1 : 0);
    //            }
    //        }
    //    }
    //    UpdateChunk();
    //}

    public void Draw(Vector3 position, float radius, byte color)
    {
        for (int x = 0; x < data.GetLength(0); x++)
        {
            for (int y = 0; y < data.GetLength(1); y++)
            {
                for (int z = 0; z < data.GetLength(2); z++)
                {
                    if (Vector3.Distance(position, transform.localPosition + new Vector3(x, y, z)*scale) < radius)
                    {
                        data[x, y, z] = color;
                    }
                }
            }
        }
        UpdateChunk();
    }

    public void UpdateChunk()
    {
        MarchingCubes.Builder.ProcessChunk(this, scale, mesh);
    }

    //void OnDrawGizmos()
    //{
    //    if (data == null)
    //    {
    //        return;
    //    }

    //    for (int x = 0; x < data.GetLength(0); x++)
    //    {
    //        for (int y = 0; y < data.GetLength(1); y++)
    //        {
    //            for (int z = 0; z < data.GetLength(2); z++)
    //            {
    //                Gizmos.color = data[x, y, z] == 0 ? Color.blue : Color.green;
    //                Gizmos.DrawSphere(transform.position + new Vector3(x, y, z), 0.05f);
    //            }
    //        }
    //    }
    //}

    public void SaveChunk(BinaryWriter writer)
    {
        for (int x = 0; x < data.GetLength(0); x++)
        {
            for (int y = 0; y < data.GetLength(1); y++)
            {
                for (int z = 0; z < data.GetLength(2); z++)
                {
                    writer.Write(data[x, y, z]);
                }
            }
        }
    }

    public void LoadChunk(BinaryReader reader)
    {
        for (int x = 0; x < data.GetLength(0); x++)
        {
            for (int y = 0; y < data.GetLength(1); y++)
            {
                for (int z = 0; z < data.GetLength(2); z++)
                {
                    data[x, y, z] = (byte)reader.ReadByte();
                }
            }
        }
        UpdateChunk();
    }
}
