using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using Unity.Networking.Transport.Utilities;

namespace ChatClientExample
{
    public delegate void NetworkMessageHandler(object handler, NetworkConnection con, DataStreamReader stream);

    public delegate void ServerMessageHandler(Server server, NetworkConnection con, MessageHeader header);
    public delegate void ClientMessageHandler(Client client, MessageHeader header);
    public enum MessageType
    {
        MESSAGE,
        JOIN,
        QUIT
    }
    public enum NetworkMessageType
    {
        HANDSHAKE,
        HANDSHAKE_RESPONSE,
        CHAT_MESSAGE,
        CHAT_MESSAGE_RESPONSE,
        CHAT_QUIT,
        NETWORK_SPAWN
    }

    public static class NetworkMessageInfo
    {
        public static Dictionary<NetworkMessageType, System.Type> TypeMap = new Dictionary<NetworkMessageType, System.Type> {
            { NetworkMessageType.HANDSHAKE,                 typeof(HandshakeMessage) },
            { NetworkMessageType.HANDSHAKE_RESPONSE,        typeof(HandshakeResponseMessage) },
            { NetworkMessageType.CHAT_MESSAGE,              typeof(ChatMessage) },
            //{ NetworkMessageType.CHAT_QUIT,                 typeof(ChatQuitMessage) },
            { NetworkMessageType.NETWORK_SPAWN,             typeof(NetworkSpawnMessage) },
            //{ NetworkMessageType.NETWORK_DESTROY,           typeof(DestroyMessage) },
            //{ NetworkMessageType.NETWORK_UPDATE_POSITION,   typeof(UpdatePositionMessage) },
            //{ NetworkMessageType.INPUT_UPDATE,              typeof(InputUpdateMessage) },
            //{ NetworkMessageType.PING,                      typeof(PingMessage) },
            //{ NetworkMessageType.PONG,                      typeof(PongMessage) }
        };
    }

    public class Server : MonoBehaviour
    {

        static Dictionary<NetworkMessageType, ServerMessageHandler> networkMessageHandlers = new Dictionary<NetworkMessageType, ServerMessageHandler> {
            { NetworkMessageType.HANDSHAKE, HandleClientHandshake },
            { NetworkMessageType.CHAT_MESSAGE, HandleClientMessage },
            { NetworkMessageType.CHAT_QUIT, HandleClientExit },
            

        };

        public NetworkDriver m_Driver;
        public NetworkPipeline m_Pipeline;
        private NativeList<NetworkConnection> m_Connections;

        private Dictionary<NetworkConnection, string> nameList = new Dictionary<NetworkConnection, string>();

        public NetworkManager networkManager;

        void Start()
        {
            // Create Driver
            m_Driver = NetworkDriver.Create(new ReliableUtility.Parameters { WindowSize = 32 });
            m_Pipeline = m_Driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));

            // Open listener on server port
            NetworkEndPoint endpoint = NetworkEndPoint.AnyIpv4;
            endpoint.Port = 1511;
            if (m_Driver.Bind(endpoint) != 0)
                Debug.Log("Failed to bind to port 1511");
            else
                m_Driver.Listen();

            m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
        }
        void OnDestroy()
        {
            m_Driver.Dispose();
            m_Connections.Dispose();
        }

        void Update()
        {
            m_Driver.ScheduleUpdate().Complete();

            for (int i = 0; i < m_Connections.Length; i++)
            {
                if (!m_Connections[i].IsCreated)
                {
                    if (nameList.ContainsKey(m_Connections[i]))
                    {
                        nameList.Remove(m_Connections[i]);
                    }

                    m_Connections.RemoveAtSwapBack(i);
                    // This little trick means we can alter the contents of the list without breaking/skipping instances
                    --i;
                }
            }

            // Accept new connections
            NetworkConnection c;
            while ((c = m_Driver.Accept()) != default(NetworkConnection))
            {
                m_Connections.Add(c);
                Debug.Log("Accepted a connection");
            }

            //HandleEvents
            DataStreamReader stream;
            for (int i = 0; i < m_Connections.Length; i++)
            {
                if (!m_Connections[i].IsCreated)
                    continue;

                // Loop through available events
                NetworkEvent.Type cmd;
                while ((cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream)) != NetworkEvent.Type.Empty)
                {
                    if (cmd == NetworkEvent.Type.Data)
                    {
                        // First UInt is always message type (this is our own first design choice)
                        NetworkMessageType msgType = (NetworkMessageType)stream.ReadUInt();
                        // Create instance and deserialize
                        MessageHeader header = (MessageHeader)System.Activator.CreateInstance(NetworkMessageInfo.TypeMap[msgType]);
                        header.DeserializeObject(ref stream);

                        if (networkMessageHandlers.ContainsKey(msgType))
                        {
                            networkMessageHandlers[msgType].Invoke(this, m_Connections[i], header);
                        }
                        else
                        {
                            Debug.LogWarning($"Unsupported message type received: {msgType}", this);
                        }
                    }
                }
            }
        }
        static void HandleClientHandshake(Server serv, NetworkConnection connection, MessageHeader header)
        {
            HandshakeMessage message = header as HandshakeMessage;

            // Add to list
            serv.nameList.Add(connection, message.name);
            string msg = $"{message.name.ToString()} has joined the chat.";
            Debug.Log($"{msg.ToString()} has joined the chat.");

            ChatMessage chatMsg = new ChatMessage
            {
                message = msg
            };

            // Send all clients the chat message
            serv.SendBroadcast(chatMsg);

        }

        

        static void HandleClientExit(Server serv, NetworkConnection connection, MessageHeader header)
        {
            if (serv.nameList.ContainsKey(connection))
            {
                //serv.chat.NewMessage($"{serv.nameList[connection]} has left the chat.", ChatCanvas.leaveColor);

                connection.Disconnect(serv.m_Driver);
            }
            else
            {
                Debug.LogError("Received exit from unlisted connection");
            }
        }

        static void HandleClientMessage(Server serv, NetworkConnection connection, MessageHeader header)
        {
            ChatMessage receivedMsg = header as ChatMessage;

            if (serv.nameList.ContainsKey(connection))
            {
                string msg = $"{serv.nameList[connection]}: {receivedMsg.message}";

                receivedMsg.message = msg;

                Debug.Log(receivedMsg.message);
                // forward message to all clients
                serv.SendBroadcast(receivedMsg);
            }
            else
            {
                Debug.LogError($"Received message from unlisted connection: {receivedMsg.message}");
            }
        }
        public void SendBroadcast(MessageHeader header)
        {
            for (int i = 0; i < m_Connections.Length; i++)
            {
                DataStreamWriter writer;
                int result = m_Driver.BeginSend(NetworkPipeline.Null, m_Connections[i], out writer);

                if (result == 0)
                {
                    header.SerializeObject(ref writer);
                    m_Driver.EndSend(writer);
                    Debug.Log("Msg send");
                }
            }
        }
    }
}