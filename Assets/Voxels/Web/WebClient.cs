﻿using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.IO;
using System;
using VildNinja.Utils;

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

        private int recieveCount = 0;

        public WebClient(HostTopology topology)
        {
            buffer = new byte[WebManager.PACKET_SIZE];

            ms = new MemoryStream(buffer);
            reader = new BinaryReader(ms);
            writer = new BinaryWriter(ms);
            
            channel = 0;
            movement = 1;
            host = NetworkTransport.AddHost(topology, 0);
        }

        public void TryConnect(string address, int port)
        {
            NetworkTransport.Connect(host, address, port, 0, out error);
            TestError("Try connect");
        }

        public void PollNetwork()
        {
            for (int i = 0; i < 20; i++)
            {
                int hostId;
                int connId;
                int chanId;
                int size;

                var reply = NetworkTransport.Receive(out hostId, out connId, out chanId, buffer, buffer.Length, out size,
                    out error);
                TestError("Poll network");

                //Debug.Log("Net: " + reply + " error? " + (NetworkError)error);

                switch (reply)
                {
                    case NetworkEventType.ConnectEvent:
                        connection = connId;

                        isConnected = true;
                        WebManager.IsConnected = true;
                        ScreenLog.SetSticky("Connected", "TRUE");
                        break;

                    case NetworkEventType.DisconnectEvent:
                        isConnected = false;
                        WebManager.IsConnected = false;
                        ScreenLog.SetSticky("Connected", "FALSE");
                        break;

                    case NetworkEventType.DataEvent:

                        recieveCount++;

                        if (recieveCount >= 3)
                        {
                            buffer[0] = WebManager.ALIVE;
                            NetworkTransport.Send(hostId, connId, movement, buffer, 1, out error);
                            TestError("Send ALIVE");
                            recieveCount = 0;
                        }

                        ms.Position = 0;
                        int count = 0;
                        while (ms.Position < size)
                        {
                            ChunkManager.Instance.LoadChunk(reader);
                            count++;
                        }
                        Debug.Log("Received " + count + " chunks as " + size + " bytes");
                        ScreenLog.Write("Received " + count + " chunks as " + size + " bytes");

                        break;

                    case NetworkEventType.Nothing:
                        i = 10000;
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
            writer.Write(WebManager.POSITION);
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

                // max chunk size is 8*8*8*2 + 4*4 = 1040 bytes
                // but is more likely to be around 100 bytes
                if (ms.Position > buffer.Length - 1040)
                {
                    NetworkTransport.Send(host, connection, channel, buffer, (int)ms.Position, out error);
                    TestError("Send updates loop");
                    ms.Position = 0;
                }
            }


            //Debug.Log("Send movement " + position + " and " + ms.Position + " bytes");

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

            Debug.LogError("network_err #" + error + " " + ((NetworkError)error) + ": " + context);
            ScreenLog.Write("network_err #" + error + " " + ((NetworkError)error) + ": " + context);
            return true;
        }
    }
}