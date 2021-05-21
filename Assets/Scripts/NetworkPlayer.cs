using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChatClientExample
{
    public class NetworkPlayer : MonoBehaviour
    {
        public bool isLocal = false;
        public bool isServer = false;
        //Camera
        public uint id;

        Client client;
        Server server;

        void Start()
        {
            if (isLocal)
            {
                GetComponentInChildren<Camera>().enabled = true;
                if (Camera.main)
                {
                    Camera.main.enabled = false;
                }

                client = FindObjectOfType<Client>();
            }
            if (isServer)
            {
                server = FindObjectOfType<Server>();
            }
        }

        void Update()
        {
			if (isLocal)
			{
				
			}

			if (isServer)
			{
				
				
			}
		}
    }
}