using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChatClientExample
{
    public class NetworkProjectile : NetworkObject
    {
        public Rigidbody rb;
        public float speed = 10;

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

        }
      
        private void OnTriggerEnter(Collider other)
        {
            if (isLocal)
            {
                //Do Nothing
            }
            if (isServer)
            {
                
                if(other.GetComponent<NetworkObject>().teamID != teamID)
                {
                    if (serv.networkManager.DestroyWithID(networkID))
                    {
                        NetworkDestroyMessage msg = new NetworkDestroyMessage()
                        {
                            networkID = networkID
                        };
                        serv.SendBroadcast(msg);

                        if(other.GetComponent<NetworkObject>().type == NetworkSpawnObject.PLAYER)
                        {
                            InputUpdate update = new InputUpdate(0, -5f);
                            InputUpdateMessage push = new InputUpdateMessage
                            {
                                networkID = other.GetComponent<NetworkObject>().networkID,
                                input = update
                            };

                            serv.SendBroadcast(push);

                            //TODO: Move Local?
                        }

                    }
                }
            }
        }
        private void FixedUpdate()
        {
            MoveProjectile(Vector3.forward);
        }
        void MoveProjectile(Vector3 direction)
        {
            //rb.velocity = direction * speed;
            rb.MovePosition(transform.position + (direction * speed * Time.deltaTime));
        }
    }

}
