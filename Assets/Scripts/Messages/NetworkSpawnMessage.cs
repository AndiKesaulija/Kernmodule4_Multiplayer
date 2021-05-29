using System.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;

namespace ChatClientExample
{
    public class NetworkSpawnMessage : MessageHeader
    {
        public override NetworkMessageType Type { get { return NetworkMessageType.NETWORK_SPAWN; } }

        public uint networkID;
        public uint objectType;

        public float posx = 0;
        public float posy = 0;
        public float posz = 0;


        public override void SerializeObject(ref DataStreamWriter writer)
        {
            base.SerializeObject(ref writer);
            writer.WriteUInt(networkID);
            writer.WriteUInt(objectType);


            writer.WriteFloat(posx);
            writer.WriteFloat(posy);
            writer.WriteFloat(posz);
        }

        public override void DeserializeObject(ref DataStreamReader reader)
        {
            base.DeserializeObject(ref reader);
            networkID = reader.ReadUInt();
            objectType = reader.ReadUInt();

            posx = reader.ReadFloat();
            posy = reader.ReadFloat();
            posz = reader.ReadFloat();
        }
    }
}

