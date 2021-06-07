using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    private static uint nextNetworkId = 0;
    public static uint NextNetworkID => ++nextNetworkId;

    [SerializeField]
    private Spawninfo spawnInfo;
    
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
    public bool SpawnWithID(NetworkSpawnObject type, uint id,uint teamID,Vector3 pos, out GameObject obj)
    {
        obj = null;
        if (networkedReferences.ContainsKey(id))
        {
            Debug.Log("CouldNot SpawnWithID: " + id);

            return false;
        }
        else
        {
            obj = GameObject.Instantiate(spawnInfo.prefabList[(int)type]);
            NetworkObject netObj = obj.GetComponent<NetworkObject>();
            if(netObj == null)
            {
                netObj = obj.AddComponent<NetworkObject>();
            }
            netObj.networkID = id;
            netObj.teamID = teamID;
            netObj.type = type;

            obj.transform.position = pos;

            networkedReferences.Add(id, obj);
            Debug.Log($"SpawnWithID: {id} TeamID: {teamID}");
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
        return false;
    }
    public uint GetNextID()
    {
        uint id = NetworkManager.NextNetworkID;
        return id;
    }
}
