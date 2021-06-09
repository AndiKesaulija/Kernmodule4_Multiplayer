using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        public uint playerID;
        public string playerName;
        public ClientState clientstate;
        public PlayerState playerState;
        public Team team;

        //public uint teamNum;
        public uint cardNum;

    }
}

