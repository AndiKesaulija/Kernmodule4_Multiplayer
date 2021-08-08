using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChatClientExample
{
    public class NetworkProjectile : NetworkObject
    {
        public Rigidbody rb;
        public Vector3 direction;
        public float speed = 10;
        public uint power;

        public Server serv;


    void Start()
        {
            rb = this.GetComponent<Rigidbody>();

            if (isServer)
            {
                serv = FindObjectOfType<Server>();
            }
        }

        void Update()
        {
            if (isServer)
            {
                //Stop Projectile if game not running
                if (serv.gameManager.gameState != GameState.IN_GAME)
                {
                    direction = Vector3.zero;
                }
                else if (direction != Vector3.forward)
                {
                    direction = Vector3.forward;
                }
            }
           
        }
      
        private void OnTriggerEnter(Collider other)
        {
            if (isLocal)
            {
                //Do Nothing
            }
            if (isServer)
            {
                if(serv.gameManager.gameState == GameState.IN_GAME)
                {
                    if (other.GetComponent<NetworkObject>())
                    {
                        if (other.GetComponent<NetworkObject>().teamID != teamID)
                        {
                            if (other.GetComponent<NetworkObject>().type == NetworkSpawnObject.PLAYER)
                            {

                                other.GetComponent<NetworkPlayer>().PushPlayer(new Vector3(0, 0, -5f), power);
                                //AddScore
                                serv.playerInfo[clientID].score += 10;
                                serv.server_UI.UpdateCard(serv.playerInfo[clientID]);

                            }

                            serv.networkManager.DestroyWithID(networkID);

                            NetworkDestroyMessage msg = new NetworkDestroyMessage()
                            {
                                networkID = networkID
                            };
                            serv.SendBroadcast(msg);

                        }
                    }
                }
               
            }
        }
        private void FixedUpdate()
        {
            MoveProjectile();
        }
        void MoveProjectile()
        {
            transform.Translate(direction * speed * Time.deltaTime);

            //rb.velocity = direction * speed;
            //rb.MovePosition(transform.position + (direction * speed * Time.deltaTime));
        }
    }

}
