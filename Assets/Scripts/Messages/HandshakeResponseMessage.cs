using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;

namespace ChatClientExample
{
    public class HandshakeResponseMessage : MessageHeader
    {

        public override NetworkMessageType Type { get { return NetworkMessageType.HANDSHAKE_RESPONSE; } }

        public string message;
        public uint clientID;

        public override void SerializeObject(ref DataStreamWriter writer)
        {
            base.SerializeObject(ref writer);

            writer.WriteFixedString128(new FixedString128($"Welcome {message.ToString()}!"));
            writer.WriteUInt(clientID);
        }
        public override void DeserializeObject(ref DataStreamReader reader)
        {
            // very important to call this first
            base.DeserializeObject(ref reader);
            message = reader.ReadFixedString128().ToString();
            clientID = reader.ReadUInt();
        }


    }
}

