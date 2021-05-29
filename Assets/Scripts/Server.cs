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
    public enum NetworkMessageType
    {
        HANDSHAKE,
        HANDSHAKE_RESPONSE,
        CHAT_MESSAGE,
        CHAT_MESSAGE_RESPONSE,
        CHAT_QUIT,
        NETWORK_SPAWN,
        NETWORK_DESTROY,
        INPUT_UPDATE,
        GAME_QUIT
    }

    public static class NetworkMessageInfo
    {
        public static Dictionary<NetworkMessageType, System.Type> TypeMap = new Dictionary<NetworkMessageType, System.Type> {
            { NetworkMessageType.HANDSHAKE,                 typeof(HandshakeMessage) },
            { NetworkMessageType.HANDSHAKE_RESPONSE,        typeof(HandshakeResponseMessage) },
            { NetworkMessageType.CHAT_MESSAGE,              typeof(ChatMessage) },
            //{ NetworkMessageType.CHAT_QUIT,                 typeof(ChatQuitMessage) },
            { NetworkMessageType.NETWORK_SPAWN,             typeof(NetworkSpawnMessage) },
            { NetworkMessageType.NETWORK_DESTROY,           typeof(NetworkDestroyMessage) },
            //{ NetworkMessageType.NETWORK_UPDATE_POSITION,   typeof(UpdatePositionMessage) },
            { NetworkMessageType.INPUT_UPDATE,              typeof(InputUpdateMessage) },
            //{ NetworkMessageType.PING,                      typeof(PingMessage) },
            //{ NetworkMessageType.PONG,                      typeof(PongMessage) }
            { NetworkMessageType.GAME_QUIT,                      typeof(QuitMessage) }

        };
    }

    public class Server : MonoBehaviour
    {

        static Dictionary<NetworkMessageType, ServerMessageHandler> networkMessageHandlers = new Dictionary<NetworkMessageType, ServerMessageHandler> {
            { NetworkMessageType.HANDSHAKE, HandleClientHandshake },
            { NetworkMessageType.CHAT_MESSAGE, HandleClientMessage },
            { NetworkMessageType.GAME_QUIT, HandleClientExit },
            { NetworkMessageType.INPUT_UPDATE, HandleInputMessage },
            { NetworkMessageType.NETWORK_DESTROY, HandleDestroyMessage },


        };

        public NetworkDriver m_Driver;
        public NetworkPipeline m_Pipeline;
        private NativeList<NetworkConnection> m_Connections;

        private Dictionary<NetworkConnection, string> nameList = new Dictionary<NetworkConnection, string>();
        private Dictionary<NetworkConnection, NetworkPlayer> playerInstances = new Dictionary<NetworkConnection, NetworkPlayer>();

        public NetworkManager networkManager;

        private static uint nextClientId = 0;
        public static uint NextClientID => ++nextClientId;

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

            //HandshakeResponseMessage response = new HandshakeResponseMessage
            //{
            //    message = msg,
            //};
            //serv.SendReply(connection, response);


            //Spawn LOCAL Player Object
            GameObject newPlayer;
            uint networkId = 0;
            if (serv.networkManager.SpawnWithID(NetworkSpawnObject.PLAYER, NetworkManager.NextNetworkID,new Vector3(0,0,0), out newPlayer))
            {
                NetworkPlayer playerInstance = newPlayer.GetComponent<NetworkPlayer>();
                playerInstance.isServer = true;
                playerInstance.isLocal = false;
                playerInstance.networkId = playerInstance.GetComponent<NetworkObject>().networkID;
                networkId = playerInstance.GetComponent<NetworkObject>().networkID;

                serv.playerInstances.Add(connection, playerInstance);

                // Send spawn local player back to sender
                HandshakeResponseMessage responseMsg = new HandshakeResponseMessage
                {
                    clientID = NextClientID,
                    message = $"Welcome {message.name.ToString()}!",
                    networkID = playerInstance.networkId,


                };

                serv.SendReply(connection, responseMsg);

            }
            else
            {
                Debug.LogError("Could not spawn player instance");
            }

            // Send all existing players to this player
            foreach (KeyValuePair<NetworkConnection, NetworkPlayer> pair in serv.playerInstances)
            {
                if (pair.Key == connection) continue;

                NetworkSpawnMessage spawnMsg = new NetworkSpawnMessage
                {
                    networkID = pair.Value.networkId,
                    objectType = (uint)NetworkSpawnObject.PLAYER,
                    posx = pair.Value.transform.position.x,
                    posy = pair.Value.transform.position.y,
                    posz = pair.Value.transform.position.z
                };

                serv.SendReply(connection, spawnMsg);
            }

            // Send creation of this player to all existing players
            if (networkId != 0)
            {
                for (int i = 0; i < serv.m_Connections.Length; i++)
                {
                    NetworkSpawnMessage spawnMsg = new NetworkSpawnMessage
                    {
                        networkID = networkId,
                        objectType = (uint)NetworkSpawnObject.PLAYER
                    };
                    serv.SendReply(serv.m_Connections[i], spawnMsg);
                }

            }
            else
            {
                Debug.LogError("Invalid network id for broadcasting creation");
            }



        }
        static void HandleInputMessage(Server serv, NetworkConnection connection, MessageHeader header)
        {
            InputUpdateMessage inputMsg = header as InputUpdateMessage;

            if (serv.playerInstances.ContainsKey(connection))
            {
                if (serv.playerInstances[connection].networkId == inputMsg.networkID)
                {
                    serv.playerInstances[connection].UpdateInput(inputMsg.input);
                }
                else
                {
                    Debug.LogError("NetworkID Mismatch for Player Input" + 
                                    "Connection ID: " + serv.playerInstances[connection].networkId + 
                                    "InputID: " + inputMsg.networkID);
                }
            }
            else
            {
                Debug.LogError("Received player input from unlisted connection");
            }
            //Update Server object
            //serv.networkManager.networkedReferences[msg.networkObjectID].GetComponent<NetworkPlayer>().UpdateInput(msg.inputMsg);

            //BroadCast to Clients
            serv.SendBroadcast(inputMsg);

        }


        static void HandleClientExit(Server serv, NetworkConnection connection, MessageHeader header)
        {
            QuitMessage quitmsg = header as QuitMessage;

            if (serv.nameList.ContainsKey(connection))
            {
                //Get Network ID van player
                //Destroy PLayer
                NetworkDestroyMessage destroyMsg = new NetworkDestroyMessage
                {
                    networkID = quitmsg.networkID
                };
                //BroadCast Destroy
                serv.SendBroadcast(destroyMsg);

                serv.networkManager.DestroyWithID(quitmsg.networkID);
                connection.Disconnect(serv.m_Driver);
                Debug.Log("Client ID: " + quitmsg.networkID + " Has Disconected");

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
        static void HandleDestroyMessage(Server serv, NetworkConnection connection, MessageHeader header)
        {
            NetworkDestroyMessage msg = header as NetworkDestroyMessage;

            if (serv.playerInstances.ContainsKey(connection))
            {
                if (serv.playerInstances[connection].networkId == msg.networkID)
                {
                    serv.networkManager.DestroyWithID(msg.networkID);
                }

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
                }
            }
        }
        public void SendReply(NetworkConnection connection, MessageHeader header)
        {
            DataStreamWriter writer;
            int result = m_Driver.BeginSend(NetworkPipeline.Null, connection, out writer);

            if (result == 0)
            {
                header.SerializeObject(ref writer);
                m_Driver.EndSend(writer);

            }
        }
    }
}