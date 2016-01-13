using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if !UNITY_WEBGL
using System.IO;
#endif

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

    public static bool IsServer = false;
    public static bool IsConnected = false;

    public int size = 16;
    public Material material;

    private readonly Dictionary<Vint3, VoxelChunk> chunks = new Dictionary<Vint3, VoxelChunk>();
    private readonly List<VoxelChunk> justPainted = new List<VoxelChunk>();
    private readonly HashSet<Vint3> update = new HashSet<Vint3>();
    private readonly List<Vint3> modified = new List<Vint3>();
    private readonly List<Vint3> redraw = new List<Vint3>();

    private void Awake()
    {
        if (SystemInfo.graphicsDeviceID == 0)
        {
            IsServer = true;
        }
    }

    private void Update()
    {
        if (update.Count == 0)
        {
            return;
        }

        // to prevent creating garbage when looping through updated elements
        redraw.Clear();
        redraw.AddRange(update);
        update.Clear();

        for (int i = 0; i < redraw.Count; i++)
        {
            if (chunks.ContainsKey(redraw[i]))
            {
                continue;
            }
            var chunk = CreateChunk(redraw[i]);
            chunks.Add(redraw[i], chunk);
        }

        for (int i = 0; i < redraw.Count; i++)
        {
            chunks[redraw[i]].UpdateChunk();
        }
    }

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

    public byte GetSingleVoxel(Vint3 pos)
    {
        var logic = pos / size;
        VoxelChunk chunk;
        if (chunks.TryGetValue(logic, out chunk))
        {
            return chunk[pos - logic * size];
        }
        return 0;
    }
 
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
                update.Add(v);
                if (IsConnected && !modified.Contains(v))
                {
                    modified.Add(v);
                }
            }
        }
    }

    private VoxelChunk CreateChunk(Vint3 intPos)
    {
        var go = new GameObject("Chunk" + intPos);
        var chunk = go.AddComponent<VoxelChunk>();
        chunk.Initialize(size, 1, intPos.Vector * size, intPos);

        if (!IsServer)
        {
            var rend = go.AddComponent<MeshRenderer>();
            rend.sharedMaterial = material;
        }

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
#if !UNITY_WEBGL
        var stream = File.Create(path);
        var writer = new BinaryWriter(stream);

        writer.Write(size);

        foreach (var chunk in chunks)
        {
            if (chunk.Value.IsEmpty())
            {
                continue;
            }

            writer.Write(chunk.Key.x);
            writer.Write(chunk.Key.y);
            writer.Write(chunk.Key.z);
            chunk.Value.SaveChunk(writer);
        }

        writer.Close();
        Debug.Log(chunks.Count + " chunks written to " + path);
#endif
    }

    public void LoadChunks(string path)
    {
#if !UNITY_WEBGL
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
            LoadChunk(reader);
        }
        
        reader.Close();
        

        Debug.Log(chunks.Count + " chunks loaded from " + path);
#endif
    }

    // used for networking only
    public int PollChanges(BinaryWriter writer)
    {
        if (modified.Count == 0)
        {
            return 0;
        }
        var v = modified[modified.Count - 1];
        modified.RemoveAt(modified.Count - 1);

        VoxelChunk chunk;
        if (chunks.TryGetValue(v, out chunk))
        {
            writer.Write(v.x);
            writer.Write(v.y);
            writer.Write(v.z);
            chunk.SaveChunk(writer);
        }

        return modified.Count;
    }

    public void LoadChunk(BinaryReader reader)
    {
        var v = new Vint3(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
        if (!chunks.ContainsKey(v))
        {
            var chunk = CreateChunk(v);
            chunks.Add(v, chunk);
        }

        chunks[v].LoadChunk(reader);

        for (int i = 0; i < offset.GetLength(0); i++)
        {
            var vo = v + new Vint3(offset[i, 0], offset[i, 1], offset[i, 2]);
            update.Add(vo);
        }
    }
}
