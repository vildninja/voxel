﻿using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.IO;
using System;
using VildNinja.Utils;

namespace VildNinja.Voxels.Web
{
    public class AreaHistory
    {
        public int time;
        public readonly HashList<Vint3> changes;

        public AreaHistory(int time)
        {
            this.time = time;
            changes = new HashList<Vint3>();
        }
    }

    public class WebServer
    {
        private readonly byte[] buffer;

        private readonly MemoryStream ms;
        private readonly BinaryReader reader;
        private readonly BinaryWriter writer;
        
        private byte error;
        private readonly int host;
        private readonly int webHost;
        private readonly int channel;
        private readonly int movement;
        private readonly List<Player> players;
        private readonly Dictionary<int, Player> connections;
        private readonly Dictionary<Vint3, List<AreaHistory>> history;

        private readonly HashList<Vint3> changes;
        private int tick = 1;

        public WebServer(int port, int webPort, HostTopology topology)
        {
            buffer = new byte[WebManager.PACKET_SIZE];

            ms = new MemoryStream(buffer);
            reader = new BinaryReader(ms);
            writer = new BinaryWriter(ms);

            players = new List<Player>();
            connections = new Dictionary<int, Player>();
            history = new Dictionary<Vint3, List<AreaHistory>>();
            changes = new HashList<Vint3>();
            
            channel = 0;
            movement = 1;
            host = NetworkTransport.AddHost(topology, port);
            webHost = NetworkTransport.AddWebsocketHost(topology, webPort, null);
        }

        public void RefreshMap()
        {
            foreach (var v in ChunkManager.Instance.AllChunks)
            {
                var area = v / 8;

                List<AreaHistory> steps;
                if (!history.TryGetValue(area, out steps))
                {
                    steps = new List<AreaHistory>();
                    steps.Add(new AreaHistory(tick));
                    history.Add(area, steps);
                }
                steps[0].changes.Add(v);
            }
        }

        public void PollNetwork()
        {
            for (int i = 0; i < 10; i++)
            {
                int hostId;
                int connId;
                int chanId;
                int size;
                
                var reply = NetworkTransport.Receive(out hostId, out connId, out chanId, buffer, buffer.Length, out size,
                    out error);

                int id = connId + hostId*10000;
                TestError("Poll network #" + id);

                switch (reply)
                {
                    case NetworkEventType.ConnectEvent:
                        if (!connections.ContainsKey(id))
                        {
                            var player = new Player(connId, hostId);
                            players.Add(player);
                            connections.Add(id, player);
                            Debug.Log("New player connected: " + player);
                        }

                        break;

                    case NetworkEventType.DisconnectEvent:
                        if (connections.ContainsKey(id))
                        {
                            players.Remove(connections[id]);
                            Debug.Log("New player disconnected: " + connections[id]);
                            connections.Remove(id);
                        }

                        break;

                    case NetworkEventType.DataEvent:
                        ms.Position = 0;

                        if (chanId == channel)
                        {
                            Debug.Log("Data received from: " + connections[id] + " - " + size + " bytes");
                            ReceiveChanges(size);
                        }
                        else if (chanId == movement)
                        {
                            var command = reader.ReadByte();
                            if (command == WebManager.POSITION)
                            {
                                PlayerMovedTo(connections[id]);
                            }
                        }

                        break;

                    case NetworkEventType.Nothing:
                        i = 10000;
                        break;
                }
            }
        }

        private void ReceiveChanges(int length)
        {
            while (ms.Position < length)
            {
                var v = ChunkManager.Instance.LoadChunk(reader);
                changes.Add(v);
            }
        }

        private void PlayerMovedTo(Player player)
        {
            var pos = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            player.position = pos;

            var area = new Vint3(pos) / 64;

            if (player.area != area)
            {
                Debug.Log("Player " + player + " moved to " + area);
                player.area = area;
                for (int i = 0; i < Vint3.Offset.Length; i++)
                {
                    var a = area + Vint3.Offset[i];
                    SendArea(player, a);
                }
            }
        }

        public void Tick()
        {
            if (changes.Count == 0)
            {
                return;
            }
            
            tick++;
            
            // go through all changes made
            foreach (var change in changes)
            {
                // find each change's area
                var area = change / 8;
                List<AreaHistory> steps;

                if (!history.TryGetValue(area, out steps))
                {
                    steps = new List<AreaHistory>();
                    steps.Add(new AreaHistory(tick));
                    history.Add(area, steps);
                }
                
                // if area doesn't already have a history for this tick: create one
                if (steps[steps.Count - 1].time < tick)
                {
                    // if we have 10 history entries for this area merge the last two together
                    // and reuse one as the new top entry. Else add a new top entry.
                    if (steps.Count >= 10)
                    {
                        var swap = steps[1];
                        steps.RemoveAt(1);
                        steps[0].changes.AddRange(swap.changes);
                        steps[0].time = swap.time;
                        swap.changes.Clear();
                        swap.time = tick;
                        steps.Add(swap);
                    }
                    else
                    {
                        steps.Add(new AreaHistory(tick));
                    }
                }

                // make sure that each change is only represented at ONE history step
                for (int i = 0; i < steps.Count; i++)
                {
                    if (steps[i].changes.Remove(change))
                    {
                        break;
                    }
                }

                // add the change to the top history step
                steps[steps.Count - 1].changes.Add(change);
            }

            changes.Clear();

            for (int i = 0; i < players.Count; i++)
            {
                for (int j = 0; j < Vint3.Offset.Length; j++)
                {
                    var a = players[i].area + Vint3.Offset[j];
                    SendArea(players[i], a);
                }
            }
        }

        private void SendArea(Player player, Vint3 area)
        {
            if (!history.ContainsKey(area))
            {
                return;
            }

            int time;
            if (!player.histroy.TryGetValue(area, out time))
            {
                player.histroy.Add(area, 0);
            }

            var steps = history[area];

            ms.Position = 0;

            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                if (step.time > time)
                {
                    foreach (var change in step.changes)
                    {
                        ChunkManager.Instance.SaveChunk(change, writer);
                        //if (buffer[ms.Position - 2] == 0)
                        //{
                        //    Debug.LogError("Voxel error: " + change);
                        //}
                        //Debug.Log("Voxel written: " + change + " last 6 bytes:" + buffer[ms.Position - 6] + ", " +
                        //    buffer[ms.Position - 5] + ", " +
                        //    buffer[ms.Position - 4] + ", " +
                        //    buffer[ms.Position - 3] + ", " +
                        //    buffer[ms.Position - 2] + ", " +
                        //    buffer[ms.Position - 1]);
                        if (ms.Position > buffer.Length - 1040)
                        {
                            Flush(player);
                        }
                    }
                }
            }

            player.histroy[area] = steps[steps.Count - 1].time;

            Flush(player);
        }

        public void Flush(Player player)
        {
            if (ms.Position > 0)
            {
                Debug.Log("Sending " + ms.Position + " bytes to player " + player);
                NetworkTransport.Send(player.host, player.connection, channel, buffer, (int)ms.Position, out error);
                TestError("Flush to " + player);
                ms.Position = 0;
            }
        }

        //public void SendUpdates()
        //{
        //    ms.Position = 0;
        //    for (int j = 0; j < 30; j++)
        //    {
        //        int remaining = ChunkManager.Instance.PollChanges(writer);

        //        if (remaining == 0)
        //        {
        //            break;
        //        }

        //        // max chunk size is 8*8*8*2 + 3*4 = 1036 bytes
        //        // but is more likely to be between 50 and 100 bytes
        //        if (ms.Position > buffer.Length - 1040)
        //        {
        //            NetworkTransport.Send(host, connection, channel, buffer, (int)ms.Position, out error);
        //            TestError("Send updates loop");
        //            ms.Position = 0;
        //        }
        //    }

        //    if (ms.Position > 0)
        //    {
        //        NetworkTransport.Send(host, connection, channel, buffer, (int)ms.Position, out error);
        //        TestError("Send updates flush");
        //    }
        //}

        private bool TestError(string context)
        {
            if (error == 0)
            {
                return false;
            }

            Debug.LogError("network_err #" + error + " " + ((NetworkError)error) + ": " + context);
            return true;
        }
    }
}