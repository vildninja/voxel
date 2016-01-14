using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using VildNinja.Voxels.Web;
using VildNinja.Utils;
#if !UNITY_WEBGL
using System.IO;
#endif

namespace VildNinja.Voxels
{
    public class ChunkManager : MonoBehaviour
    {

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
        private readonly HashList<Vint3> update = new HashList<Vint3>();
        private readonly List<Vint3> modified = new List<Vint3>();

        public IEnumerable<Vint3> AllChunks
        {
            get
            {
                return chunks.Keys;
            }
        }

        private void Update()
        {
            if (update.Count == 0)
            {
                return;
            }

            foreach (var redraw in update)
            {
                if (chunks.ContainsKey(redraw))
                {
                    continue;
                }
                var chunk = CreateChunk(redraw);
                chunks.Add(redraw, chunk);
            }

            foreach (var redraw in update)
            {
                chunks[redraw].UpdateChunk();
            }

            update.Clear();
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
            var logic = pos/size;
            VoxelChunk chunk;
            if (chunks.TryGetValue(logic, out chunk))
            {
                return chunk[pos - logic*size];
            }
            return 0;
        }

        public void Draw(Vector3 position, float radius, byte color)
        {
            position = transform.InverseTransformPoint(position);
            
            var pos = new Vint3(position) / size;

            var bounds = new Bounds(position, new Vector3(radius*2, radius*2, radius*2));

            for (int i = 0; i < Vint3.Offset.Length; i++)
            {
                var v = pos + Vint3.Offset[i];
                if (!chunks.ContainsKey(v))
                {
                    var chunk = CreateChunk(v);

                    chunks.Add(v, chunk);
                }

                if (chunks[v].bounds.Intersects(bounds))
                {
                    chunks[v].Draw(position, radius, color);
                    update.Add(v);
                    if (WebManager.IsConnected && !modified.Contains(v))
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
            chunk.Initialize(size, intPos.Vector*size, intPos);

            if (!WebManager.IsServer)
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

            SaveChunks(writer);

            writer.Close();
            Debug.Log(chunks.Count + " chunks written to " + path);
#endif
        }

        public void SaveChunks(BinaryWriter writer)
        {
            foreach (var chunk in chunks)
            {
                if (chunk.Value.IsEmpty())
                {
                    continue;
                }

                SaveChunk(chunk.Value, writer);
            }
        }

        public void SaveChunk(VoxelChunk chunk, BinaryWriter writer)
        {
            writer.Write(chunk.iPos.x);
            writer.Write(chunk.iPos.y);
            writer.Write(chunk.iPos.z);
            chunk.SaveChunk(writer);
        }

        public void SaveChunk(Vint3 pos, BinaryWriter writer)
        {
            var chunk = chunks[pos];
            SaveChunk(chunk, writer);
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

        public Vint3 LoadChunk(BinaryReader reader)
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

            return v;
        }
    }
}
