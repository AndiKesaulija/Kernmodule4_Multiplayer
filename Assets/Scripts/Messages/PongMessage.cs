using System.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;

namespace ChatClientExample
{
	public class PongMessage : MessageHeader
	{
		public override NetworkMessageType Type { get { return NetworkMessageType.PONG; } }

		public uint networkId;

		public override void SerializeObject(ref DataStreamWriter writer)
		{
			// very important to call this first
			base.SerializeObject(ref writer);
		}

		public override void DeserializeObject(ref DataStreamReader reader)
		{
			// very important to call this first
			base.DeserializeObject(ref reader);
		}
	}
}