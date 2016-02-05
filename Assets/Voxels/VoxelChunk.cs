using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Collections.Generic;
using VildNinja.Voxels.Web;

namespace VildNinja.Voxels
{
    public class VoxelChunk : MonoBehaviour, MarchingCubes.ByteData
    {
        private static readonly Stack<bool[,,]> boolPool = new Stack<bool[,,]>(); 
        private bool[,,] myChanges;
        private int changeCount;

        private byte[,,] data;

        private Mesh mesh;
        private MeshCollider collider;

        public Bounds bounds;

        public Vint3 iPos;

        public int Length
        {
            get { return ChunkManager.Size; }
        }

        public byte this[int x, int y, int z]
        {
            get { return this[new Vint3(x, y, z)]; }
        }

        public byte this[Vint3 pos]
        {
            get
            {
                //return data[x, y, z];
                var logic = pos/ ChunkManager.Size;
                if (logic == Vint3.Zero)
                {
                    return data[pos.x, pos.y, pos.z];
                }
                else
                {
                    return ChunkManager.Instance.GetSingleVoxel(iPos* ChunkManager.Size + pos);
                }
            }
        }

        private static bool[,,] RequestBools()
        {
            if (boolPool.Count > 0)
            {
                var b = boolPool.Pop();
                for (int x = 0; x < b.GetLength(0); x++)
                {
                    for (int y = 0; y < b.GetLength(1); y++)
                    {
                        for (int z = 0; z < b.GetLength(2); z++)
                        {
                            b[x, y, z] = false;
                        }
                    }
                }
            }
            return new bool[ChunkManager.Size, ChunkManager.Size, ChunkManager.Size];
        }

        // Use this for initialization
        public void Initialize(Vector3 position, Vint3 intPos)
        {
            iPos = intPos;
            data = new byte[ChunkManager.Size, ChunkManager.Size, ChunkManager.Size];

            if (!WebManager.IsServer)
            {
                mesh = new Mesh();
                var filter = gameObject.AddComponent<MeshFilter>();
                filter.mesh = mesh;
                collider = gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
            }

            transform.localPosition = position;
            bounds = new Bounds(position + new Vector3(ChunkManager.Size, ChunkManager.Size, ChunkManager.Size)*0.5f,
                new Vector3(ChunkManager.Size, ChunkManager.Size, ChunkManager.Size));
        }

        public void Draw(Vector3 position, float radius, byte color)
        {
            if (myChanges == null)
            {
                myChanges = RequestBools();
                changeCount = 0;
            }

            for (int x = 0; x < data.GetLength(0); x++)
            {
                for (int y = 0; y < data.GetLength(1); y++)
                {
                    for (int z = 0; z < data.GetLength(2); z++)
                    {
                        if (Vector3.Distance(position, transform.localPosition + new Vector3(x, y, z)) < radius)
                        {
                            data[x, y, z] = color;
                            myChanges[x, y, z] = true;
                            changeCount++;
                        }
                    }
                }
            }
        }

        public void UpdateChunk()
        {
            if (!WebManager.IsServer)
            {
                MarchingCubes.Builder.ProcessChunk(this, mesh);
                collider.sharedMesh = mesh;
            }
        }

        public bool IsEmpty()
        {
            for (int x = 0; x < data.GetLength(0); x++)
            {
                for (int y = 0; y < data.GetLength(1); y++)
                {
                    for (int z = 0; z < data.GetLength(2); z++)
                    {
                        if (data[x, y, z] != 0)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public void SaveChunk(BinaryWriter writer)
        {
            byte current = 0;
            byte count = 0;
            for (int x = 0; x < data.GetLength(0); x++)
            {
                for (int y = 0; y < data.GetLength(1); y++)
                {
                    for (int z = 0; z < data.GetLength(2); z++)
                    {
                        if (current == data[x, y, z])
                        {
                            if (count < 255)
                            {
                                count++;
                            }
                            else
                            {
                                writer.Write(count);
                                writer.Write(current);
                                count = 1;
                            }
                        }
                        else
                        {
                            if (count > 0)
                            {
                                writer.Write(count);
                                writer.Write(current);
                            }
                            current = data[x, y, z];
                            count = 1;
                        }
                    }
                }
            }

            writer.Write(count);
            writer.Write(current);
        }

        public void LoadChunk(BinaryReader reader)
        {
            byte current = 0;
            byte count = 0;

            for (int x = 0; x < data.GetLength(0); x++)
            {
                for (int y = 0; y < data.GetLength(1); y++)
                {
                    for (int z = 0; z < data.GetLength(2); z++)
                    {
                        if (count == 0)
                        {
                            count = reader.ReadByte();
                            current = reader.ReadByte();
                        }

                        if (myChanges != null && myChanges[x, y, z] && data[x, y, z] == current)
                        {
                            myChanges[x, y, z] = false;
                            changeCount--;
                        }

                        if (data[x, y, z] == 0 || myChanges == null || !myChanges[x, y, z])
                        {
                            data[x, y, z] = current;
                        }
                        count--;

                        //data[x, y, z] = reader.ReadByte();
                    }
                }
            }

            if (myChanges != null && changeCount <= 0)
            {
                boolPool.Push(myChanges);
                myChanges = null;
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
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
    }
}