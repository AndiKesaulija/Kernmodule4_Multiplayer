using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChatClientExample
{
    public class NetworkManager : MonoBehaviour
    {
        private static uint nextNetworkId = 0;
        public static uint NextNetworkID => ++nextNetworkId;

        private static uint nextClientId = 0;
        public static uint NextClientID => ++nextClientId;

        [SerializeField]
        private Spawninfo spawnInfo;
        public bool isServer;
        public Server serv;

    public Dictionary<uint, GameObject> networkedReferences = new Dictionary<uint, GameObject>();
        
        public bool GetObjectID(uint networkID, out GameObject obj)
        {
            obj = null;
            if (networkedReferences.ContainsKey(networkID))
            {
                obj = networkedReferences[networkID];
                return true;
            }
            return false;
        }
        public bool SpawnWithID(NetworkSpawnObject type, uint networkID, uint clientID, uint teamID, Vector3 pos, Vector3 rot, out GameObject obj)
        {
            obj = null;
            if (networkedReferences.ContainsKey(networkID))
            {
                Debug.Log("CouldNot SpawnWithID: " + networkID);

                return false;
            }
            else
            {
                obj = GameObject.Instantiate(spawnInfo.prefabList[(int)type]);
                NetworkObject netObj = obj.GetComponent<NetworkObject>();
                if (netObj == null)
                {
                    netObj = obj.AddComponent<NetworkObject>();
                }
                netObj.networkID = networkID;
                netObj.clientID = clientID;
                netObj.teamID = teamID;
                netObj.type = type;

                obj.transform.position = pos;
                obj.transform.rotation = Quaternion.Euler(rot);

                //Add to Client ObjectPool if Server
                if (isServer == true)
                {
                    serv.playerInfo[clientID].objectList.Add(netObj);
                }

                networkedReferences.Add(networkID, obj);
                //Debug.Log($"SpawnWithID: {id} TeamID: {teamID}");
                return true;
            }
        }
        public bool DestroyWithID(uint id)
        {
            if (networkedReferences.ContainsKey(id))
            {
                Destroy(networkedReferences[id]);
                networkedReferences.Remove(id);
                return true;
            }
            Debug.Log($"networkedReferences does not contain ID: {id}");
            return false;
        }
        public uint GetNextID()
        {
            uint id = NetworkManager.NextNetworkID;
            return id;
        }
    }

}
