using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;

namespace ChatClientExample
{
    public class GameQuitMessage : MessageHeader
    {
        public override NetworkMessageType Type { get { return NetworkMessageType.GAME_QUIT; } }

        public uint clientID;
        public override void SerializeObject(ref DataStreamWriter writer)
        {
            base.SerializeObject(ref writer);

            writer.WriteUInt(clientID);
        }

        public override void DeserializeObject(ref DataStreamReader reader)
        {
            base.DeserializeObject(ref reader);

            clientID = reader.ReadUInt();
        }
    }
}

