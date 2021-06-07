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
    public class NetworkPlayer : NetworkObject
    {
        //Camera
        //public uint networkId;

        public Client client;
        public Camera myCam;

        InputUpdate input;

        //movement
        public Rigidbody rb;
        public Vector3 movement;
        public float speed = 10;


        void Start()
        {
            rb = this.GetComponent<Rigidbody>();
            client = FindObjectOfType<Client>();
            //networkId = this.GetComponent<NetworkObject>().networkID;
            myCam = GetComponentInChildren<Camera>();
            if (isLocal)
            {
                myCam.enabled = true;
                if (Camera.main)
                {
                    Camera.main.enabled = false;
                }
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
                    networkID = this.networkID,
                    input = update
                };

                client.SendPackedMessage(msg);

                if (Input.GetMouseButtonDown(1))
                {
                    //Debug.Log("MouseDown");

                    //RPCMessage RPCmsg = new RPCMessage
                    //{
                    //    target = this,
                    //    methodName = "Fire",
                    //    data = new object[] {null,transform.position}
                    //};

                    //client.SendPackedMessage(RPCmsg);

                    client.CallOnServerObject("Fire", this, null, transform.position);
                }

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

        public void Fire(Server serv,Vector3 position)
        {
            uint id = serv.networkManager.GetNextID();

            GameObject obj;
            if (serv.networkManager.SpawnWithID(NetworkSpawnObject.BULLET, id, teamID, position, out obj))
            {
                obj.GetComponent<NetworkObject>().isServer = true;

                NetworkSpawnMessage msg = new NetworkSpawnMessage
                {
                    objectType = (uint)NetworkSpawnObject.BULLET,
                    networkID = id,
                    teamID = teamID,
                    pos = position
                };

                serv.SendBroadcast(msg);
            }
           
        }

        public void PushPlayer(Server serv, Vector3 direction)
        {

            InputUpdate update = new InputUpdate(0, -5f);

            InputUpdateMessage msg = new InputUpdateMessage
            {
                networkID = this.networkID,
                input = update
            };

            serv.SendBroadcast(msg);
        }
    }
}