using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

namespace VildNinja.Voxels.Web
{
    public class WebManager : MonoBehaviour
    {
        public const ushort PACKET_SIZE = 3000;

        public static bool IsServer = false;
        public static bool IsConnected = false;

        private WebServer server;
        private WebClient client;
        private float counter;
        private int saveTimer;

        public Vector3 position;

        [SerializeField]
        private bool forceServer;

        // Use this for initialization
        void Awake()
        {

            NetworkTransport.Init();

            var config = new ConnectionConfig();
            config.PacketSize = PACKET_SIZE;
            config.Channels.Add(new ChannelQOS(QosType.ReliableFragmented));
            config.Channels.Add(new ChannelQOS(QosType.Unreliable));


            Debug.Log("AckDelay " + config.AckDelay);
            Debug.Log("AllCostTimeout " + config.AllCostTimeout);
            Debug.Log("ChannelCount " + config.ChannelCount);
            Debug.Log("ConnectTimeout " + config.ConnectTimeout);
            Debug.Log("DisconnectTimeout " + config.DisconnectTimeout);
            Debug.Log("FragmentSize " + config.FragmentSize);
            Debug.Log("IsAcksLong " + config.IsAcksLong);
            Debug.Log("MaxCombinedReliableMessageCount " + config.MaxCombinedReliableMessageCount);
            Debug.Log("MaxCombinedReliableMessageSize " + config.MaxCombinedReliableMessageSize);
            Debug.Log("MaxConnectionAttempt " + config.MaxConnectionAttempt);
            Debug.Log("MaxSentMessageQueueSize " + config.MaxSentMessageQueueSize);
            Debug.Log("MinUpdateTimeout " + config.MinUpdateTimeout);
            Debug.Log("NetworkDropThreshold " + config.NetworkDropThreshold);
            Debug.Log("OverflowDropThreshold " + config.OverflowDropThreshold);
            Debug.Log("PacketSize " + config.PacketSize);
            Debug.Log("PingTimeout " + config.PingTimeout);
            Debug.Log("ReducedPingTimeout " + config.ReducedPingTimeout);
            Debug.Log("ResendTimeout " + config.ResendTimeout);
            Debug.Log("UsePlatformSpecificProtocols " + config.UsePlatformSpecificProtocols);

            var topology = new HostTopology(config, 200);
            topology.ReceivedMessagePoolSize = 50000;
            topology.SentMessagePoolSize = 50000;

            if (SystemInfo.graphicsDeviceID == 0 || forceServer)
            {
                IsServer = true;

                server = new WebServer(19219, topology);

                ChunkManager.Instance.LoadChunks("server.vox");
                server.RefreshMap();
                
            }
            else
            {
                client = new WebClient(topology);
                client.TryConnect("127.0.0.1", 19219);
            }
        }
        

        // Update is called once per frame
        void Update()
        {
            if (IsServer)
            {
                server.PollNetwork();
                if (counter > 0.5f)
                {
                    server.Tick();
                    counter = 0;
                    saveTimer++;

                    if (saveTimer > 30)
                    {
                        ChunkManager.Instance.SaveChunks("server.vox");
                        saveTimer = 0;
                    }
                }
            }
            else
            {
                client.PollNetwork();
                if (counter > 0.5f && IsConnected)
                {
                    client.SendChanges(position);
                    counter = 0;
                }
            }

            counter += Time.deltaTime;
        }
    }
}
