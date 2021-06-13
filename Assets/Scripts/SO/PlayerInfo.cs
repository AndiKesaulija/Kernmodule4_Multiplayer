using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;

namespace ChatClientExample
{
    public enum ClientState
    {
        SPECTATING,
        IN_LOBBY,
        IN_GAME,
    }
    public enum PlayerState
    {
        NOT_READY,
        READY,
        OUT_OF_BOUNDS
    }
    public enum Team
    {
        SPECTATOR,
        RED,
        BLUE
    }
    public class PlayerInfo
    {
        public uint clientID;
        public uint networkID;
        public string playerName;
        public ClientState clientstate;
        public PlayerState playerState;
        public uint cardNum;

        public NetworkConnection connection;
        public List<NetworkObject> objectList = new List<NetworkObject>();

        //SpawnPosition
        public Team team;
        public uint teamPos;
        //public uint round;

        public Vector3 spawnPos;
        public Vector3 spawnRot;

        public uint currentZone;
        public uint activeZone;

    }
}

