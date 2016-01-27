using UnityEngine;
using System.Collections;

namespace VildNinja.Voxels.Web
{
    public class WebManager : MonoBehaviour
    {
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
            if (SystemInfo.graphicsDeviceID == 0 || forceServer)
            {
                IsServer = true;

                server = new WebServer(19219);

                ChunkManager.Instance.LoadChunks("server.vox");
                server.RefreshMap();
                
            }
            else
            {
                client = new WebClient();
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
