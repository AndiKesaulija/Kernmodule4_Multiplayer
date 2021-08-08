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

        public float spellCooldown = 100;
        public float cooldown;
        public uint maxPower = 100;
        public uint power = 0;



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
                client.client_UI.player = this;

                cooldown = spellCooldown;

            }
            if (isServer)
            {
                serv = FindObjectOfType<Server>();
                currentZone = CheckPlayerPosition(transform.position.z);
                serv.server_UI.UpdateCard(serv.playerInfo[clientID]);

            }

        }

        void Update()
        {
            
            if (isLocal)
			{
                InputUpdate update = new InputUpdate(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

                InputUpdateMessage msg = new InputUpdateMessage
                {
                    networkID = networkID,
                    clientID = clientID,
                    input = update,

                };

                client.SendPackedMessage(msg);

                if (cooldown < spellCooldown)
                {
                    cooldown += 1;
                }

                if(cooldown >= spellCooldown)
                {
                    if (Input.GetMouseButton(0))
                    {
                        if(power < maxPower)
                        {
                            power += 1;
                        }
                    }
                }
                
                if (Input.GetMouseButtonUp(0))
                {
                    if(cooldown >= spellCooldown)
                    {
                        client.CallOnServerObject("Fire", this, null, transform.position, clientID, power);
                        cooldown = 0;
                    }
                    power = 0;


                }
                if (Input.GetMouseButtonDown(1))
                {
                    client.CallOnServerObject("SetPlayerState", this, null, (int)PlayerState.READY);
                }

            }

            if (isServer)
            {
                updateZone = CheckPlayerPosition(transform.position.z);
                if (updateZone != currentZone)
                {
                    serv.playerInfo[clientID].currentZone = updateZone;
                    serv.server_UI.UpdateCard(serv.playerInfo[clientID]);
                    serv.gameManager.HandleOutOfBounds(clientID, networkID);

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
            transform.Translate(direction * speed * Time.deltaTime);

        }

        public void Fire(Server serv,Vector3 position, uint clientID, uint power)
        {
            if (serv.gameManager.gameState == GameState.IN_GAME && serv.playerInfo[clientID].playerState == PlayerState.READY)
            {
                uint networkID = serv.networkManager.GetNextID();

                Vector3 rot = transform.rotation.eulerAngles; //TEMP

                GameObject obj;
                if (serv.networkManager.SpawnWithID(NetworkSpawnObject.BULLET, networkID, clientID, teamID, position, rot, out obj))
                {

                    Debug.Log("Power: " + power);

                    obj.GetComponent<NetworkObject>().isServer = true;
                    obj.GetComponent<NetworkProjectile>().power = power;

                    NetworkSpawnMessage msg = new NetworkSpawnMessage
                    {
                        objectType = (uint)NetworkSpawnObject.BULLET,
                        networkID = networkID,
                        clientID = clientID,
                        teamID = teamID,
                        pos = position,
                        rot = rot,
                    };

                    serv.SendBroadcast(msg);
                }
            }
           
           
        }

        public void PushPlayer(Vector3 direction, uint power)
        {
            direction = direction - new Vector3(0, 0, power / 10);
            MovePLayer(direction);
        }

        public void SetPlayerState(Server serv, int state)
        {
            //Set Ready
            serv.gameManager.SetPlayerState(serv, clientID, state);
        }
    }
}