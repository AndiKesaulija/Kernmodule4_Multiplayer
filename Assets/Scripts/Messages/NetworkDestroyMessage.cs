using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;

namespace ChatClientExample
{
    public class NetworkDestroyMessage : MessageHeader
    {
        public override NetworkMessageType Type { get { return NetworkMessageType.NETWORK_DESTROY; } }

        public uint networkID;
        public override void SerializeObject(ref DataStreamWriter writer)
        {
            base.SerializeObject(ref writer);

            writer.WriteUInt(networkID);
        }

        public override void DeserializeObject(ref DataStreamReader reader)
        {
            base.DeserializeObject(ref reader);

            networkID = reader.ReadUInt();
        }
    }
}

