using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;

namespace ChatClientExample
{
    public class NetworkDestroyMultipleMessage : MessageHeader
    {
        public override NetworkMessageType Type { get { return NetworkMessageType.NETWORK_DESTROY_MULTIPLE; } }

        public uint idcount;
        public List<uint> networkIDs = new List<uint>();
        public override void SerializeObject(ref DataStreamWriter writer)
        {
            base.SerializeObject(ref writer);

            idcount = (uint)networkIDs.Count;
            writer.WriteUInt(idcount);

            for (int i = 0; i < networkIDs.Count; i++)
            {
                writer.WriteUInt(networkIDs[i]);
            }
        }

        public override void DeserializeObject(ref DataStreamReader reader)
        {
            base.DeserializeObject(ref reader);

            idcount = reader.ReadUInt();

            for (int i = 0; i < idcount; i++)
            {
                networkIDs.Add(reader.ReadUInt());
            }
            
        }
    }
}

