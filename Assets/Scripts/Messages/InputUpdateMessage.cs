using System.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;

namespace ChatClientExample
{
    public class InputUpdateMessage : MessageHeader
    {
        public override NetworkMessageType Type { get { return NetworkMessageType.INPUT_UPDATE; } }

        public uint networkObjectID;

        //Temp X,Z pos + 10
        public uint movex;
        public uint movez;

        public override void SerializeObject(ref DataStreamWriter writer)
        {
            base.SerializeObject(ref writer);

            writer.WriteUInt(networkObjectID);

            writer.WriteUInt(movex);
            writer.WriteUInt(movez);
        }
        public override void DeserializeObject(ref DataStreamReader reader)
        {
            base.DeserializeObject(ref reader);

            networkObjectID = reader.ReadUInt();
            movex = reader.ReadUInt();
            movez = reader.ReadUInt();

        }
    }
}

