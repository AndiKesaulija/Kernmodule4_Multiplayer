using System.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;

namespace ChatClientExample
{
    public class UpdateNetworkObjectMessage : MessageHeader
    {
        public override NetworkMessageType Type { get { return NetworkMessageType.UPDATE_NETWORK_OBJECT; } }

        public uint networkID;
        public Vector3 position;

        public override void SerializeObject(ref DataStreamWriter writer)
        {
            base.SerializeObject(ref writer);

            writer.WriteUInt(networkID);

            writer.WriteFloat(position.x);
            writer.WriteFloat(position.y);
            writer.WriteFloat(position.z);
        }

        public override void DeserializeObject(ref DataStreamReader reader)
        {
            base.DeserializeObject(ref reader);

            networkID = reader.ReadUInt();

            position = new Vector3(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat());
        }
    }
}

