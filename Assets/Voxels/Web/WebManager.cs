using UnityEngine;
using System.Collections;

namespace VildNinja.Voxels.Web
{
    public class WebManager : MonoBehaviour
    {
        public static bool IsServer = false;
        internal static bool IsConnected = false;

        private WebServer server;
        private WebClient client;
        private float counter;

        // Use this for initialization
        void Awake()
        {
            if (SystemInfo.graphicsDeviceID == 0)
            {
                IsServer = true;

                server = new WebServer(19219);

                ChunkManager.Instance.LoadChunks("server.vox");
                
            }
        }
        

        // Update is called once per frame
        void Update()
        {


            counter += Time.deltaTime;
            if (counter > 0.5f)
            {

            }

        }
    }
}
