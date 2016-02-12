using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using VildNinja.Utils;

namespace VildNinja.Voxels.Web
{
    public class WebManager : MonoBehaviour
    {
        public const ushort PACKET_SIZE = 2000;

        public const byte ALIVE = 1;
        public const byte POSITION = 2;

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
            ScreenLog.Write("WebManager Awake!");

            var global = new GlobalConfig();

            Debug.Log("global recieve " + global.ReactorMaximumReceivedMessages);
            Debug.Log("global sent " + global.ReactorMaximumSentMessages);
            Debug.Log("global max " + global.MaxPacketSize);
            Debug.Log("global model " + global.ReactorModel);
            Debug.Log("global thread timeout " + global.ThreadAwakeTimeout);

            global.ReactorMaximumReceivedMessages = 50000;

            NetworkTransport.Init(global);

            var config = new ConnectionConfig();
            //config.PacketSize = PACKET_SIZE;
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
            //topology.SentMessagePoolSize = 50000;

            if (Application.platform != RuntimePlatform.WebGLPlayer &&
                (SystemInfo.graphicsDeviceID == 0 || forceServer))
            {
                ScreenLog.Write("Server starting!");
                IsServer = true;

                server = new WebServer(19219, 8123, topology);

                ChunkManager.Instance.LoadChunks("server.vox");
                server.RefreshMap();

            }
            else
            {
                ScreenLog.Write("Client starting!");
                client = new WebClient(topology);
#if UNITY_WEBGL
                //client.TryConnect("127.0.0.1", 8123);
                client.TryConnect("188.226.164.147", 8123);
#else
                //client.TryConnect("127.0.0.1", 19219);
                client.TryConnect("188.226.164.147", 19219);
#endif
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
