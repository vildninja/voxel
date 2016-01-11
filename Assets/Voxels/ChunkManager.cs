using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class ChunkManager : MonoBehaviour {

    private static ChunkManager instance = null;

    public static ChunkManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ChunkManager>();
            }

            return instance;
        }
    }

    public int size = 16;
    public Material material;

    private readonly Dictionary<Vint3, VoxelChunk> chunks = new Dictionary<Vint3, VoxelChunk>();

    private int[,] offset =
    {
        {-1, -1, -1}, {-1, -1, 0}, {-1, -1, 1},
        {-1, 0, -1}, {-1, 0, 0}, {-1, 0, 1},
        {-1, 1, -1}, {-1, 1, 0}, {-1, 1, 1},
        {0, -1, -1}, {0, -1, 0}, {0, -1, 1},
        {0, 0, -1}, {0, 0, 0}, {0, 0, 1},
        {0, 1, -1}, {0, 1, 0}, {0, 1, 1},
        {1, -1, -1}, {1, -1, 0}, {1, -1, 1},
        {1, 0, -1}, {1, 0, 0}, {1, 0, 1},
        {1, 1, -1}, {1, 1, 0}, {1, 1, 1}
    };
 
    public void Draw(Vector3 position, float radius, byte color)
    {
        position = transform.InverseTransformPoint(position);

        int x = Mathf.FloorToInt(position.x/size);
        int y = Mathf.FloorToInt(position.y/size);
        int z = Mathf.FloorToInt(position.z/size);

        var bounds = new Bounds(position, new Vector3(radius * 2, radius * 2, radius * 2));

        for (int i = 0; i < offset.GetLength(0); i++)
        {
            var v = new Vint3(offset[i, 0] + x, offset[i, 1] + y, offset[i, 2] + z);
            if (!chunks.ContainsKey(v))
            {
                var chunk = CreateChunk(v);

                chunks.Add(v, chunk);
            }

            if (chunks[v].bounds.Intersects(bounds))
            {
                chunks[v].Draw(position, radius, color);
            }
        }
    }

    private VoxelChunk CreateChunk(Vint3 intPos)
    {
        var go = new GameObject("Chunk" + intPos);
        var chunk = go.AddComponent<VoxelChunk>();
        chunk.Initialize(size, 1, intPos.Vector * (size - 1f), intPos);

        var rend = go.AddComponent<MeshRenderer>();
        rend.sharedMaterial = material;

        go.transform.SetParent(transform, false);

        return chunk;
    }

    public void Drag(Vector3 position, Vector3 move, Quaternion rotate)
    {
        transform.Translate(move, Space.World);
        Vector3 axis;
        float angle;
        rotate.ToAngleAxis(out angle, out axis);
        transform.RotateAround(position, axis, angle);
    }

    public void SaveChunks(string path)
    {
        var stream = File.OpenWrite(path);
        var writer = new BinaryWriter(stream);

        writer.Write(size);

        foreach (var chunk in chunks)
        {
            writer.Write(Mathf.RoundToInt(chunk.Key.x));
            writer.Write(Mathf.RoundToInt(chunk.Key.y));
            writer.Write(Mathf.RoundToInt(chunk.Key.z));
            chunk.Value.SaveChunk(writer);
        }

        writer.Close();
        Debug.Log(chunks.Count + " chunks written to " + path);
    }

    public void LoadChunks(string path)
    {
        foreach (var chunk in chunks)
        {
            Destroy(chunk.Value.gameObject);
        }
        chunks.Clear();

        if (!File.Exists(path))
        {
            return;
        }

        var stream = File.OpenRead(path);
        var reader = new BinaryReader(stream);

        size = reader.ReadInt32();

        while (stream.Position < stream.Length)
        {
            var v = new Vint3(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
            var chunk = CreateChunk(v);
            chunk.LoadChunk(reader);

            if (chunks.ContainsKey(v))
            {
                Debug.LogError(v + " duplicated in data");
            }
            else
            {
                chunks.Add(v, chunk);
            }
        }
        
        reader.Close();
        Debug.Log(chunks.Count + " chunks loaded from " + path);
    }
}
