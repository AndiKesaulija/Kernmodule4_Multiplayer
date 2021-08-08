using System.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;

namespace ChatClientExample
{
    public class InputUpdateMessage : MessageHeader
    {
        public override NetworkMessageType Type { get { return NetworkMessageType.INPUT_UPDATE; } }

        public uint networkID;
        public uint clientID;
        public InputUpdate input;

        public override void SerializeObject(ref DataStreamWriter writer)
        {
            base.SerializeObject(ref writer);

            writer.WriteUInt(networkID);
            writer.WriteUInt(clientID);
            writer.WriteFloat(input.horizontal);
            writer.WriteFloat(input.vertical);


        }
        public override void DeserializeObject(ref DataStreamReader reader)
        {
            base.DeserializeObject(ref reader);

            networkID = reader.ReadUInt();
            clientID = reader.ReadUInt();
            input.horizontal = reader.ReadFloat();
            input.vertical = reader.ReadFloat();

        }
    }
}

