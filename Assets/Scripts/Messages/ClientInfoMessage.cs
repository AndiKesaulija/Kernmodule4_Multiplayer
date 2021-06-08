using System.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;

namespace ChatClientExample
{
    public class ClientInfoMessage : MessageHeader
    {
        public override NetworkMessageType Type  { get { return NetworkMessageType.CLIENT_INFO; } }

        public uint clientID;
        public uint teamNum;

        public override void SerializeObject(ref DataStreamWriter writer)
        {
            base.SerializeObject(ref writer);

            writer.WriteUInt(clientID);
            writer.WriteUInt(teamNum);

        }

        public override void DeserializeObject(ref DataStreamReader reader)
        {
            base.DeserializeObject(ref reader);

            clientID = reader.ReadUInt();
            teamNum = reader.ReadUInt();
        }

    }
}

