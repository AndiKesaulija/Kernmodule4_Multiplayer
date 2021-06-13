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
        public Server serv;
        public Camera myCam;

        InputUpdate input;

        //movement
        public Rigidbody rb;
        public Vector3 direction;
        public float speed = 10;

        public uint currentZone;
        private uint updateZone;


        void Start()
        {
            rb = this.GetComponent<Rigidbody>();
            //networkId = this.GetComponent<NetworkObject>().networkID;
            myCam = GetComponentInChildren<Camera>();
            if (isLocal)
            {
                client = FindObjectOfType<Client>();

                client.spectatorCam.enabled = false;
                myCam.enabled = true;
                //if (Camera.main)
                //{
                //    Camera.main.enabled = false;
                //}
            }
            if (isServer)
            {
                serv = FindObjectOfType<Server>();
                currentZone = CheckPlayerPosition(transform.position.z);
            }

        }

        void Update()
        {

            if (isLocal)
			{
                InputUpdate update = new InputUpdate(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

                

                InputUpdateMessage msg = new InputUpdateMessage
                {
                    networkID = this.networkID,
                    input = update,

                };

                client.SendPackedMessage(msg);

                if (Input.GetMouseButtonDown(0))
                {
                    Debug.Log("MouseDown");

                    //RPCMessage RPCmsg = new RPCMessage
                    //{
                    //    target = this,
                    //    methodName = "Fire",
                    //    data = new object[] { null, transform.position }
                    //};

                    //client.SendPackedMessage(RPCmsg);

                    client.CallOnServerObject("Fire", this, null, transform.position);
                }
                if (Input.GetMouseButtonDown(1))
                {
                    client.CallOnServerObject("SetPlayerState", this, null, (int)PlayerState.READY);
                }

            }

            if (isServer)
            {
                updateZone = CheckPlayerPosition(transform.position.z);
                if(updateZone != currentZone)
                {
                    serv.playerInfo[clientID].currentZone = updateZone;

                    serv.HandleOutOfBounds(clientID, networkID);
                    //SetPlayerState(serv, (int)PlayerState.OUT_OF_BOUNDS);

                }
                currentZone = updateZone;
            }
			
		}
        void FixedUpdate()
        {
            MovePLayer(direction);
        }

        public void UpdateInput(InputUpdate received)
        {
            direction = new Vector3(received.horizontal, 0, received.vertical).normalized;
        }
        public uint CheckPlayerPosition(float position)
        {
            uint currZone = 0;
            //Define zones
            if (position < 33 && position > 22)
            {
                currZone = 1;
                return currZone;
            }
            else if (position < 22 && position > 11)
            {
                currZone = 2;
                return currZone;
            }
            else if (position < 11 && position > 0)
            {
                currZone = 3;
                return currZone;
            }
            else if (position < 0 && position > -11)
            {
                currZone = 4;
                return currZone;
            }
            else if (position < -11 && position > -22)
            {
                currZone = 5;
                return currZone;
            }
            else if (position < -22 && position > -33)
            {
                currZone = 6;
                return currZone;
            }

            return currZone;

        }
        void MovePLayer(Vector3 direction)
        {
            //Gebruik RB met axis?
            transform.Translate(direction * speed * Time.deltaTime);


            //rb.MovePosition(transform.position + (direction * speed * Time.deltaTime));
            //rb.velocity = direction * speed;
        }

        public void Fire(Server serv,Vector3 position)
        {
            if(serv.gameState == GameState.IN_GAME)
            {
                uint id = serv.networkManager.GetNextID();

                Vector3 rot = transform.rotation.eulerAngles; //TEMP

                GameObject obj;
                if (serv.networkManager.SpawnWithID(NetworkSpawnObject.BULLET, id, 0, teamID, position, rot, out obj))
                {
                    obj.GetComponent<NetworkObject>().isServer = true;

                    NetworkSpawnMessage msg = new NetworkSpawnMessage
                    {
                        objectType = (uint)NetworkSpawnObject.BULLET,
                        networkID = id,
                        teamID = teamID,
                        pos = position,
                        rot = rot
                    };

                    serv.SendBroadcast(msg);
                }
            }
           
           
        }

        public void PushPlayer(Vector3 direction)
        {
            MovePLayer(direction);
        }

        public void SetPlayerState(Server serv, int state)
        {
            //Set Ready
            serv.server_UI.SetPlayerState(serv, clientID, state);
        }
    }
}