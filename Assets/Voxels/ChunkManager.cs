using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

    private readonly Dictionary<Vector3, VoxelChunk> chunks =new Dictionary<Vector3, VoxelChunk>();
    //private readonly HashSet<Vector3> populated = new HashSet<Vector3>();

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
        int x = Mathf.FloorToInt(position.x/size);
        int y = Mathf.FloorToInt(position.y/size);
        int z = Mathf.FloorToInt(position.z/size);

        var bounds = new Bounds(position, new Vector3(radius * 2, radius * 2, radius * 2));

        for (int i = 0; i < offset.GetLength(0); i++)
        {
            var v = new Vector3(offset[i, 0] + x, offset[i, 1] + y, offset[i, 2] + z);
            if (!chunks.ContainsKey(v))
            {
                var go = new GameObject("Chunk" + v);
                var chunk = go.AddComponent<VoxelChunk>();
                chunk.Initialize(size, 1, v * (size - 1));

                var rend = go.AddComponent<MeshRenderer>();
                rend.sharedMaterial = material;

                go.transform.SetParent(transform);

                chunks.Add(v, chunk);
            }

            if (chunks[v].bounds.Intersects(bounds))
            {
                chunks[v].Draw(position, radius, color);
            }
        }
    }
}
