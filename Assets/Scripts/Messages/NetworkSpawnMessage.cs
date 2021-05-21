using System.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;

namespace ChatClientExample
{
    public class NetworkSpawnMessage : MessageHeader
    {
        public override NetworkMessageType Type { get { return NetworkMessageType.NETWORK_SPAWN; } }

        public uint objectType;
        public uint posx;
        public uint posy;
        public uint posz;


        public override void SerializeObject(ref DataStreamWriter writer)
        {
            base.SerializeObject(ref writer);

            writer.WriteUInt(objectType);

            writer.WriteUInt(posx);
            writer.WriteUInt(posy);
            writer.WriteUInt(posz);
        }

        public override void DeserializeObject(ref DataStreamReader reader)
        {
            base.DeserializeObject(ref reader);
            objectType = reader.ReadUInt();

            posx = reader.ReadUInt();
            posy = reader.ReadUInt();
            posz = reader.ReadUInt();
        }
    }
}

