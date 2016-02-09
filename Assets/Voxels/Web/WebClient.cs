using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.IO;
using System;

namespace VildNinja.Voxels.Web
{
    public class WebClient
    {
        private readonly byte[] buffer;
        
        private readonly MemoryStream ms;
        private readonly BinaryReader reader;
        private readonly BinaryWriter writer;
        
        private byte error;
        private readonly int host;
        private readonly int channel;
        private readonly int movement;
        private int connection;

        private bool isConnected = false;

        public WebClient()
        {
            buffer = new byte[10000];

            ms = new MemoryStream(buffer);
            reader = new BinaryReader(ms);
            writer = new BinaryWriter(ms);
            
            NetworkTransport.Init();
            
            var config = new ConnectionConfig();
            config.PacketSize = 10000;
            config.Channels.Add(new ChannelQOS(QosType.ReliableSequenced));
            channel = 0;
            config.Channels.Add(new ChannelQOS(QosType.Unreliable));
            movement = 1;
            var topology = new HostTopology(config, 1);
            host = NetworkTransport.AddHost(topology);
        }

        public void TryConnect(string address, int port)
        {
            NetworkTransport.Connect(host, address, port, 0, out error);
            TestError("Try connect");
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
                TestError("Poll network");

                switch (reply)
                {
                    case NetworkEventType.ConnectEvent:
                        connection = connId;

                        isConnected = true;
                        WebManager.IsConnected = true;
                        break;
                    case NetworkEventType.DisconnectEvent:
                        isConnected = false;
                        WebManager.IsConnected = false;

                        break;
                    case NetworkEventType.DataEvent:

                        ms.Position = 0;
                        while (ms.Position < size)
                        {
                            ChunkManager.Instance.LoadChunk(reader);
                        }

                        break;
                    case NetworkEventType.Nothing:
                        i = 10;
                        break;
                }
            }
        }

        public void SendChanges(Vector3 position)
        {
            if (!isConnected)
            {
                return;
            }
            
            ms.Position = 0;
            writer.Write(position.x);
            writer.Write(position.y);
            writer.Write(position.z);
            NetworkTransport.Send(host, connection, movement, buffer, (int)ms.Position, out error);

            ms.Position = 0;
            for (int j = 0; j < 30; j++)
            {
                int remaining = ChunkManager.Instance.PollChanges(writer);

                if (remaining == 0)
                {
                    break;
                }

                // max chunk size is 8*8*8*2 + 3*4 = 1036 bytes
                // but is more likely to be between 50 and 100 bytes
                if (ms.Position > buffer.Length - 1040)
                {
                    NetworkTransport.Send(host, connection, channel, buffer, (int)ms.Position, out error);
                    TestError("Send updates loop");
                    ms.Position = 0;
                }
            }

            if (ms.Position > 0)
            {
                NetworkTransport.Send(host, connection, channel, buffer, (int)ms.Position, out error);
                TestError("Send updates flush");
            }
        }

        private bool TestError(string context)
        {
            if (error == 0)
            {
                return false;
            }

            Debug.LogError("network_err #" + error + ": " + context);
            return true;
        }
    }
}