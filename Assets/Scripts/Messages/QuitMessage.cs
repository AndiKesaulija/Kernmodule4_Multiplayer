using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;

namespace ChatClientExample
{
    public class QuitMessage : MessageHeader
    {
        public override NetworkMessageType Type { get { return NetworkMessageType.GAME_QUIT; } }

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

