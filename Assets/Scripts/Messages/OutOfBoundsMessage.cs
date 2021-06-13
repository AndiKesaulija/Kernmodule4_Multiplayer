using System.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;

namespace ChatClientExample
{
    public class OutOfBoundsMessage : MessageHeader
    {
        public override NetworkMessageType Type { get { return NetworkMessageType.PLAYER_OUTOFBOUNDS; } }

        public uint clientID;
        public uint networkID;

        public override void SerializeObject(ref DataStreamWriter writer)
        {
            base.SerializeObject(ref writer);

            writer.WriteUInt(clientID);
            writer.WriteUInt(networkID);
        }

        public override void DeserializeObject(ref DataStreamReader reader)
        {
            base.DeserializeObject(ref reader);

            clientID = reader.ReadUInt();
            networkID = reader.ReadUInt();
        }
    }
}

