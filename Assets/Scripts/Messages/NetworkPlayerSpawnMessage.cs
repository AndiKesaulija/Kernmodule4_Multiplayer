using System.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;

namespace ChatClientExample
{
    public class NetworkPlayerSpawnMessage : MessageHeader
    {
        public override NetworkMessageType Type { get { return NetworkMessageType.PLAYER_SPAWN; } }

        public uint networkID;
        public uint objectType;
        public uint teamID = 0;


        public Vector3 pos;


        public override void SerializeObject(ref DataStreamWriter writer)
        {
            base.SerializeObject(ref writer);
            writer.WriteUInt(networkID);
            writer.WriteUInt(objectType);
            writer.WriteUInt(teamID);

            writer.WriteFloat(pos.x);
            writer.WriteFloat(pos.y);
            writer.WriteFloat(pos.z);
        }

        public override void DeserializeObject(ref DataStreamReader reader)
        {
            base.DeserializeObject(ref reader);
            networkID = reader.ReadUInt();
            objectType = reader.ReadUInt();
            teamID = reader.ReadUInt();

            pos = new Vector3(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat());

        }
    }
}

