using System.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;

namespace ChatClientExample
{
    public class ServerInfoMessage : MessageHeader
    {
        public override NetworkMessageType Type { get { return NetworkMessageType.SERVER_INFO; } }

        public override void SerializeObject(ref DataStreamWriter writer)
        {
            base.SerializeObject(ref writer);

            writer.WriteUInt(ServerSettings.redTeamPlayerCount);
            writer.WriteUInt(ServerSettings.blueTeamPlayerCount);

        }

        public override void DeserializeObject(ref DataStreamReader reader)
        {
            base.DeserializeObject(ref reader);

            ServerSettings.redTeamPlayerCount = reader.ReadUInt();
            ServerSettings.blueTeamPlayerCount = reader.ReadUInt();
        }
    }
}

