using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using VildNinja.Voxels.Web;
using VildNinja.Utils;
using System.Text;
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

        public static int Size = 8;
        public Material material;

        private readonly Dictionary<Vint3, VoxelChunk> chunks = new Dictionary<Vint3, VoxelChunk>();
        private readonly HashList<Vint3> update = new HashList<Vint3>();
        private readonly Queue<Vint3> painted = new Queue<Vint3>();

        private readonly HashList<Vint3> areas = new HashList<Vint3>(); 

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
                CreateChunk(redraw);
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

        // Overflow may cause endless recursion. Hence the debugCounter
        private int debugCounter = 0;
        public byte GetSingleVoxel(Vint3 pos)
        {
            var logic = pos/Size;
            VoxelChunk chunk;
            if (debugCounter == 0 && chunks.TryGetValue(logic, out chunk))
            {
                debugCounter++;
                var found = chunk[pos - logic*Size];
                debugCounter = 0;
                return found;
            }
            debugCounter = 0;
            return 0;
        }

        public void Draw(Vector3 position, float radius, byte color)
        {
            position = transform.InverseTransformPoint(position);
            
            var pos = new Vint3(position) / Size;

            var bounds = new Bounds(position, new Vector3(radius*2, radius*2, radius*2));

            for (int i = 0; i < Vint3.Offset.Length; i++)
            {
                var v = pos + Vint3.Offset[i];
                if (!chunks.ContainsKey(v))
                {
                    CreateChunk(v);
                }

                if (chunks[v].bounds.Intersects(bounds))
                {
                    chunks[v].Draw(position, radius, color);
                    update.Add(v);
                    if (WebManager.IsConnected && !painted.Contains(v))
                    {
                        painted.Enqueue(v);
                    }
                }
            }
        }

        private VoxelChunk CreateChunk(Vint3 intPos)
        {
            if (intPos.x > int.MaxValue - 100 || intPos.x < int.MinValue + 100 ||
                intPos.y > int.MaxValue - 100 || intPos.y < int.MinValue + 100 ||
                intPos.z > int.MaxValue - 100 || intPos.z < int.MinValue + 100)
            {
                Debug.LogError("Attempted to create chunk close to border: " + intPos);
                return null;
            }

            var go = new GameObject("Chunk" + intPos);
            var chunk = go.AddComponent<VoxelChunk>();
            chunk.Initialize(intPos.Vector*Size, intPos);

            if (!WebManager.IsServer)
            {
                var rend = go.AddComponent<MeshRenderer>();
                rend.sharedMaterial = material;
            }

            go.transform.SetParent(transform, false);

            chunks.Add(intPos, chunk);

            areas.Add(intPos/64);

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

            writer.Write(Size);

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
            writer.Write(chunk.iPos.GetHashCode());
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

            Size = reader.ReadInt32();

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
            if (painted.Count == 0)
            {
                return 0;
            }
            var v = painted.Dequeue();

            VoxelChunk chunk;
            if (chunks.TryGetValue(v, out chunk))
            {
                SaveChunk(chunk, writer);
            }

            return painted.Count;
        }

        public Vint3 LoadChunk(BinaryReader reader)
        {
            var v = new Vint3(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
            int hash = reader.ReadInt32();

            if (hash != v.GetHashCode())
            {
                Debug.LogError("Chunk at " + v + " is malformed!");
                var ms = (MemoryStream)reader.BaseStream;

                //StringBuilder builder = new StringBuilder();
                //builder.AppendLine("Data:");
                //var array = WebClient.buffer;
                //for (int i = 0; i < array.Length; i++)
                //{
                //    builder.AppendLine(i.ToString("0000") + " " + array[i] + (i == ms.Position ? " <---" : ""));
                //}
                //Debug.Log(builder.ToString());

                ms.Position = ms.Length;
                return v;
            }

            Debug.Log("Receiving chunk " + v);

            if (!chunks.ContainsKey(v))
            {
                CreateChunk(v);
            }

            chunks[v].LoadChunk(reader);

            for (int i = 0; i < offset.GetLength(0); i++)
            {
                var vo = v + new Vint3(offset[i, 0], offset[i, 1], offset[i, 2]);
                update.Add(vo);
            }

            return v;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.white;

            foreach (var area in areas)
            {
                Gizmos.DrawWireCube(((area.Vector + new Vector3(0.5f, 0.5f, 0.5f)) * 64), new Vector3(64, 64, 64));
            }
        }
    }
}
