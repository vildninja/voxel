using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.IO;
using System;

public class WebClient {
    
    private readonly byte[] buffer;

    private readonly int maxSize;
    private readonly MemoryStream ms;
    private readonly BinaryReader reader;
    private readonly BinaryWriter writer;

    private byte error;
    private int host;
    private int connection;
    private int channel;

    private bool isConnected = false;

    public WebClient()
    {
        maxSize = 9000;
        buffer = new byte[100000];

        ms = new MemoryStream(buffer);
        reader = new BinaryReader(ms);
        writer = new BinaryWriter(ms);

        NetworkTransport.Init();
    }

    public void PollNetwork()
    {
        int hostId;
        int connId;
        int chanId;
        int size;


        for (int i = 0; i < 10; i++)
        {
            var reply = NetworkTransport.Receive(out hostId, out connId, out chanId, buffer, buffer.Length, out size, out error);

            switch (reply)
            {
                case NetworkEventType.ConnectEvent:
                    connection = connId;

                    isConnected = true;
                    break;
                case NetworkEventType.DisconnectEvent:
                    isConnected = false;

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
        
        if (isConnected)
        {
            ms.Position = 0;
            for (int j = 0; j < 30; j++)
            {
                int last = (int)ms.Position;
                int remaining = ChunkManager.Instance.PollChanges(writer);

                if (remaining == 0)
                {
                    break;
                }

                if (ms.Position > buffer.Length)
                {
                    NetworkTransport.Send(host, connection, channel, buffer, last, out error);
                    Array.Copy(buffer, last, buffer, 0, ms.Position - last);
                }
            }

            if (ms.Position > 0)
            {
                NetworkTransport.Send(host, connection, channel, buffer, (int)ms.Position, out error);
            }
        }
    }
}
