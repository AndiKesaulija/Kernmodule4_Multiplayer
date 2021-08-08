using System.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;

namespace ChatClientExample
{
    public class ServerInfoMessage : MessageHeader
    {
        public override NetworkMessageType Type { get { return NetworkMessageType.SERVER_INFO; } }

        public uint maxTeamCount = ServerSettings.maxTeamPlayerCount;
        public uint redteamCount = ServerSettings.redTeamPlayerCount;
        public uint blueteamCount = ServerSettings.blueTeamPlayerCount;

        public override void SerializeObject(ref DataStreamWriter writer)
        {
            base.SerializeObject(ref writer);

            writer.WriteUInt(maxTeamCount);
            writer.WriteUInt(redteamCount);
            writer.WriteUInt(blueteamCount);

        }

        public override void DeserializeObject(ref DataStreamReader reader)
        {
            base.DeserializeObject(ref reader);

            ServerSettings.maxTeamPlayerCount = reader.ReadUInt();
            ServerSettings.redTeamPlayerCount = reader.ReadUInt();
            ServerSettings.blueTeamPlayerCount = reader.ReadUInt();
        }
    }
}

