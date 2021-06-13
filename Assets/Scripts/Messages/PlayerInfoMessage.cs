using System.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;

namespace ChatClientExample
{
    public class PlayerInfoMessage : MessageHeader
    {
        public override NetworkMessageType Type { get { return NetworkMessageType.PLAYER_INFO; } }

        public PlayerInfo info;
        public override void SerializeObject(ref DataStreamWriter writer)
        {
            base.SerializeObject(ref writer);

            writer.WriteUInt(info.networkID);

            writer.WriteUInt(info.currentZone);
        }

        public override void DeserializeObject(ref DataStreamReader reader)
        {
            base.DeserializeObject(ref reader);

            info.networkID = reader.ReadUInt();

            info.currentZone = reader.ReadUInt();
        }
    }

}
