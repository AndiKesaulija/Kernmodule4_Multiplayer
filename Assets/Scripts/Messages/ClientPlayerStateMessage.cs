using System.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;

namespace ChatClientExample
{
    public class ClientPlayerStateMessage : MessageHeader
    {
        public override NetworkMessageType Type { get { return NetworkMessageType.CLIENT_PLAYER_STATE; } }

        public uint clientID;
        public PlayerState state;

        public override void SerializeObject(ref DataStreamWriter writer)
        {
            base.SerializeObject(ref writer);

            writer.WriteUInt(clientID);
            writer.WriteInt((int)state);
        }
        public override void DeserializeObject(ref DataStreamReader reader)
        {
            base.DeserializeObject(ref reader);

            clientID = reader.ReadUInt();
            state = (PlayerState)reader.ReadInt();
        }
    }
}

