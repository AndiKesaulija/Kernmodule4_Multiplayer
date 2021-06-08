using System.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;

namespace ChatClientExample
{
    public class ClientStateMessage : MessageHeader
    {
        public override NetworkMessageType Type { get { return NetworkMessageType.CLIENT_STATE; } }

        public uint networkID;
        public ClientState state;

        public override void SerializeObject(ref DataStreamWriter writer)
        {
            base.SerializeObject(ref writer);
            
            writer.WriteUInt(networkID);
            writer.WriteUInt((uint) state);
        }
        public override void DeserializeObject(ref DataStreamReader reader)
        {
            base.DeserializeObject(ref reader);

            networkID = reader.ReadUInt();
            state = (ClientState)reader.ReadUInt();
        }
    }
}

