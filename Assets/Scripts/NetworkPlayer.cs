using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChatClientExample
{
    public struct InputUpdate
    {
        public float horizontal, vertical;
        //public bool fire, jump;

        public InputUpdate(float h, float v)
        {
            horizontal = h;
            vertical = v;
        }
    }
    public class NetworkPlayer : MonoBehaviour
    {
        public bool isLocal = false;
        public bool isServer = false;
        //Camera
        public uint networkId;

        public Client client;
        //public Server server;

        InputUpdate input;

        //movement
        public Rigidbody rb;
        public Vector3 movement;
        public float speed = 10;


        void Start()
        {
            rb = this.GetComponent<Rigidbody>();
            client = FindObjectOfType<Client>();
            networkId = this.GetComponent<NetworkObject>().networkID;

            if (isLocal)
            {
                GetComponentInChildren<Camera>().enabled = true;
                if (Camera.main)
                {
                    Camera.main.enabled = false;
                }

            }
            if (isServer)
            {

            }
        }

        void Update()
        {
            InputUpdate update = new InputUpdate(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

            if (isLocal)
			{
                //movement = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
                InputUpdateMessage msg = new InputUpdateMessage
                {
                    networkID = networkId,
                    input = update
                };

                client.SendPackedMessage(msg);

            }

			if (isServer)
			{
				
				
			}
		}
        void FixedUpdate()
        {
            MovePLayer(movement);
        }

        public void UpdateInput(InputUpdate received)
        {
            //input.horizontal = received.horizontal;
            //input.vertical = received.vertical;

            movement = new Vector3(received.horizontal, 0, received.vertical);
        }
        void MovePLayer(Vector3 direction)
        {
            //rb.velocity = direction * speed;
            rb.MovePosition(transform.position + (direction * speed * Time.deltaTime));
        }
    }
}