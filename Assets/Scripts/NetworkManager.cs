using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    private static uint nextNetworkId = 0;
    public static uint NextNetworkID => ++nextNetworkId;

    [SerializeField]
    private Spawninfo spawnInfo;
    private Dictionary<uint, GameObject> networkedReferences = new Dictionary<uint, GameObject>();

    public bool GetObjectID(uint id, out GameObject obj)
    {
        obj = null;
        if (networkedReferences.ContainsKey(id))
        {
            return true;
        }
        return false;
    }
    public bool SpawnWithID(NetworkSpawnObject type, uint id, out GameObject obj)
    {
        obj = null;
        if (networkedReferences.ContainsKey(id))
        {
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

            networkedReferences.Add(id, obj);
            
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
}
