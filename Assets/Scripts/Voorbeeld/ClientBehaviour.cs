using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Networking.Transport;

namespace ChatClientExample
{
    public class ClientBehaviour : MonoBehaviour
    {
        static Dictionary<GameEvent, GameEventHandler> gameEventDictionary = new Dictionary<GameEvent, GameEventHandler>() {
            // link game events to functions...
            { GameEvent.NUMBER_REPLY, NumberReplyHandler },
        };

        public NetworkDriver m_Driver;
        public NetworkConnection m_Connection;
        public bool Done;

        void Start()
        {
            m_Driver = NetworkDriver.Create();
            m_Connection = default(NetworkConnection);

            var endpoint = NetworkEndPoint.LoopbackIpv4;
            endpoint.Port = 1511;
            m_Connection = m_Driver.Connect(endpoint);

        }

        public void OnDestroy()
        {
            m_Driver.Dispose();
        }
        void Update()
        {
            m_Driver.ScheduleUpdate().Complete();

            if (!m_Connection.IsCreated)
            {
                if (!Done)
                {
                    Debug.Log("Something went wrong during connect");
                    return;
                }
            }

            DataStreamReader stream;
            NetworkEvent.Type cmd;
            while ((cmd = m_Connection.PopEvent(m_Driver, out stream)) != NetworkEvent.Type.Empty)
            {
                //Connect
                if (cmd == NetworkEvent.Type.Connect)
                {
                    Debug.Log("We are now connected to the server");

                    uint value = 1;
                    DataStreamWriter writer;
                    int result = m_Driver.BeginSend(NetworkPipeline.Null, m_Connection, out writer);

                    //non-0 is an error code
                    if (result == 0)
                    {
                        //GameEvent
                        writer.WriteUInt((uint)GameEvent.NUMBER);

                        writer.WriteUInt(value);
                        m_Driver.EndSend(writer);
                    }
                }
                //Data
                else if (cmd == NetworkEvent.Type.Data)
                {
                    // Read GameEvent type from stream
                    GameEvent gameEventType = (GameEvent)stream.ReadUInt();
                    if (gameEventDictionary.ContainsKey(gameEventType))
                    {
                        gameEventDictionary[gameEventType].Invoke(stream, this, m_Connection);
                    }
                    else
                    {
                        //Unsupported event received...
                    }
                }
                //Disconect
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client got disconnected from server");
                    m_Connection = default(NetworkConnection);
                }
            }

        }
        static void NumberReplyHandler(DataStreamReader stream, object sender, NetworkConnection connection)
        {
            uint value = stream.ReadUInt();
            Debug.Log("Got the value = " + value + " back from the server");
            ClientBehaviour client = sender as ClientBehaviour;

            //TODO remove
            client.Done = true;
            client.m_Connection.Disconnect(client.m_Driver);
            client.m_Connection = default(NetworkConnection);
        }

    }
}
