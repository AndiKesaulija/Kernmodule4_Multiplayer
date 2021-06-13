using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkObject : MonoBehaviour
{
    public uint networkID = 0;
    public uint clientID = 0;
    public uint teamID = 0;
    public NetworkSpawnObject type;

    public bool isServer = false;
    public bool isLocal = false;
}
