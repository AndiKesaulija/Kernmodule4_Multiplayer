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
    //public enum GameState
    //{
    //    LOBBY,
    //    INTERMISSION,
    //    IN_GAME
    //}
    public enum NetworkMessageType
    {
        HANDSHAKE,
        HANDSHAKE_RESPONSE,
        CHAT_MESSAGE,
        CHAT_MESSAGE_RESPONSE,
        CHAT_QUIT,
        NETWORK_SPAWN,
        PLAYER_SPAWN,
        NETWORK_DESTROY,
        NETWORK_DESTROY_MULTIPLE,
        INPUT_UPDATE,
        UPDATE_NETWORK_OBJECT,
        PLAYER_INFO,
        PLAYER_OUTOFBOUNDS,
        GAME_LOBBEY,
        GAME_QUIT,
        RPC,
        CALL_ON_FUNCTION,
        CLIENT_STATE,
        CLIENT_PLAYER_STATE,
        CLIENT_INFO,
        SERVER_INFO,
        PING,
        PONG,
        
    }
    public class PingPong
    {
        public uint clientID;
        public float lastSendTime = 0;
        public int status = -1;
        public string name = ""; // because of weird issues...
    }
    public static class NetworkMessageInfo
    {
        public static Dictionary<NetworkMessageType, System.Type> TypeMap = new Dictionary<NetworkMessageType, System.Type> {
            { NetworkMessageType.HANDSHAKE,                 typeof(HandshakeMessage) },
            { NetworkMessageType.HANDSHAKE_RESPONSE,        typeof(HandshakeResponseMessage) },
            { NetworkMessageType.CHAT_MESSAGE,              typeof(ChatMessage) },
            { NetworkMessageType.CHAT_QUIT,                 typeof(ChatQuitMessage) },
            { NetworkMessageType.NETWORK_SPAWN,             typeof(NetworkSpawnMessage) },
            { NetworkMessageType.PLAYER_SPAWN,              typeof(NetworkPlayerSpawnMessage) },
            { NetworkMessageType.NETWORK_DESTROY,           typeof(NetworkDestroyMessage) },
            { NetworkMessageType.NETWORK_DESTROY_MULTIPLE,  typeof(NetworkDestroyMultipleMessage) },
            { NetworkMessageType.PLAYER_INFO,               typeof(PlayerInfoMessage) },
            { NetworkMessageType.PLAYER_OUTOFBOUNDS,        typeof(OutOfBoundsMessage) },
            //{ NetworkMessageType.NETWORK_UPDATE_POSITION,   typeof(UpdatePositionMessage) },
            { NetworkMessageType.INPUT_UPDATE,              typeof(InputUpdateMessage) },
            { NetworkMessageType.UPDATE_NETWORK_OBJECT,     typeof(UpdateNetworkObjectMessage) },
            { NetworkMessageType.PING,                      typeof(PingMessage) },
            { NetworkMessageType.PONG,                      typeof(PongMessage) },
            { NetworkMessageType.GAME_QUIT,                 typeof(GameQuitMessage) },
            { NetworkMessageType.GAME_LOBBEY,               typeof(GameLobbyMessage) },
            { NetworkMessageType.RPC,                       typeof(RPCMessage) },
            { NetworkMessageType.CALL_ON_FUNCTION,          typeof(CallOnFunctionMessage) },
            { NetworkMessageType.CLIENT_STATE,              typeof(ClientStateMessage) },
            { NetworkMessageType.CLIENT_INFO,               typeof(ClientInfoMessage) },
            { NetworkMessageType.CLIENT_PLAYER_STATE,       typeof(ClientPlayerStateMessage) },
            { NetworkMessageType.SERVER_INFO,               typeof(ServerInfoMessage) },




        };
    }

    public class Server : MonoBehaviour
    {

        static Dictionary<NetworkMessageType, ServerMessageHandler> networkMessageHandlers = new Dictionary<NetworkMessageType, ServerMessageHandler> {
            { NetworkMessageType.HANDSHAKE,                 HandleClientHandshake },
            { NetworkMessageType.CHAT_MESSAGE,              HandleClientMessage },
            { NetworkMessageType.GAME_QUIT,                 HandleClientExit },
            { NetworkMessageType.GAME_LOBBEY,               HandleClientLobbeyMessage },
            { NetworkMessageType.INPUT_UPDATE,              HandleInputMessage },
            { NetworkMessageType.PLAYER_INFO,               HandlePlayerInfoMessage },
            { NetworkMessageType.NETWORK_SPAWN,             HandleSpawnMessage },
            { NetworkMessageType.NETWORK_DESTROY,           HandleDestroyMessage },
            { NetworkMessageType.RPC,                       HandleRPCMessage },
            { NetworkMessageType.CLIENT_INFO,               HandleClientInfoMessage },
            { NetworkMessageType.SERVER_INFO,               HandleServerInfoMessage },
            { NetworkMessageType.PONG,                      HandlePongMessage }



        };

        public NetworkDriver m_Driver;
        public NetworkPipeline m_Pipeline;
        private NativeList<NetworkConnection> m_Connections;

        private Dictionary<NetworkConnection, string> nameList = new Dictionary<NetworkConnection, string>();
        public Dictionary<NetworkConnection, NetworkPlayer> playerInstances = new Dictionary<NetworkConnection, NetworkPlayer>();
        private Dictionary<NetworkConnection, PingPong> pongDict = new Dictionary<NetworkConnection, PingPong>();

        //TODO: Alleen Message Handlers in deze class? rest naaar servermanger
        public Dictionary<uint, PlayerInfo> playerInfo = new Dictionary<uint, PlayerInfo>();

        public NetworkManager networkManager;
        public NetworkBehavior networkBehavior;

        public Server_UI server_UI;
        public MyGameManager gameManager;
        //public GameState gameState;
        //public SpawnPoint spawnPoints = new SpawnPoint();

        public uint redteamcount;
        public uint blueteamcount;

        void Start()
        {
            gameManager = new MyGameManager(this);
            gameManager.gameState = GameState.LOBBY;

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
            redteamcount = ServerSettings.redTeamPlayerCount;
            blueteamcount = ServerSettings.blueTeamPlayerCount;

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
                {
                    Debug.Log($"{m_Connections[i]} Has No Connection");
                    continue;
                }

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


            //Update PLayer Position Zoals ping?
            foreach (KeyValuePair<uint, PlayerInfo> player in playerInfo)
            {
                foreach (KeyValuePair<uint, GameObject> networkObject in networkManager.networkedReferences)
                {
                    UpdateNetworkObjectMessage msg = new UpdateNetworkObjectMessage();

                    msg.networkID = networkObject.Key;
                    msg.position = networkObject.Value.transform.position;

                    SendReply(player.Value.connection, msg);
                }

                //server_UI.UpdatePlayerCard(this, player.Value.clientID);

            }


            //Ping Pong
            // Ping Pong stuff for timeout disconnects
            for (int i = 0; i < m_Connections.Length; i++)
            {
                if (!m_Connections[i].IsCreated)
                {
                    continue;
                }

                if (pongDict.ContainsKey(m_Connections[i]))
                {
                    //Check if no replys
                    if (Time.time - pongDict[m_Connections[i]].lastSendTime > 5f)
                    {
                        pongDict[m_Connections[i]].lastSendTime = Time.time;
                        if (pongDict[m_Connections[i]].status == 0)
                        {
                            //Cleanup Player
                            Debug.Log($"Disconnecting: Client {pongDict[m_Connections[i]].clientID} Has Lost Connection!");

                            //HandleClientExit
                            //TODO: alles wat er moet gebeuren naar de HandleClientExit en vervolgens hier aan roepen

                            GameQuitMessage quit = new GameQuitMessage
                            {
                                clientID = playerInfo[pongDict[m_Connections[i]].clientID].clientID
                            };
                            HandleClientExit(this, m_Connections[i], quit);
                        }
                        else
                        {
                            pongDict[m_Connections[i]].status -= 1;
                            PingMessage pingMsg = new PingMessage();
                            SendReply(m_Connections[i], pingMsg);
                        }
                    }
                    
                }
                else if (nameList.ContainsKey(m_Connections[i]))
                { //means they've succesfully handshaked

                    PingPong ping = new PingPong();
                    ping.clientID = 0;//temp
                    ping.lastSendTime = Time.time;
                    ping.status = 3;    // 3 retries
                    ping.name = nameList[m_Connections[i]];
                    pongDict.Add(m_Connections[i], ping);

                    PingMessage pingMsg = new PingMessage();
                    SendReply(m_Connections[i], pingMsg);
                }
            }

            gameManager.GameUpdate();
            
        }

        static void HandleClientHandshake(Server serv, NetworkConnection connection, MessageHeader header)
        {
            HandshakeMessage message = header as HandshakeMessage;

            // Add to list
            serv.nameList.Add(connection, message.name);

            string msg = $"{message.name.ToString()} has joined the Game.";
            
            uint clientID = NetworkManager.NextClientID;

            //Add client to LobbyManager
            PlayerInfo info = new PlayerInfo
            {
                userID = message.userID,
                clientID = clientID,
                playerName = message.name,
                clientstate = ClientState.IN_LOBBY,
                team = 0,
                connection = connection
            };
            serv.playerInfo.Add(info.clientID, info);

            //Add Client to Server_UI
            serv.server_UI.AddPlayerCard(serv, info);

            HandshakeResponseMessage responseMsg = new HandshakeResponseMessage
            {
                message = $"Welcome {message.name.ToString()}!",
                clientID = clientID,
            };
            serv.SendReply(connection, responseMsg);

            //Sync Client ServerSettings with server
            ServerInfoMessage serverInfo = new ServerInfoMessage();
            serv.SendReply(connection, serverInfo);

        }
        
        static void HandleInputMessage(Server serv, NetworkConnection connection, MessageHeader header)
        {
            InputUpdateMessage inputMsg = header as InputUpdateMessage;

            InputUpdate stop = new InputUpdate(0, 0);

            if (serv.gameManager.gameState == GameState.IN_GAME)
            {
                if (serv.playerInstances.ContainsKey(connection))
                {
                    if (serv.playerInfo[inputMsg.clientID].playerState != PlayerState.OUT_OF_BOUNDS)
                    {
                        if (serv.playerInstances[connection].networkID == inputMsg.networkID)
                        {
                            serv.playerInstances[connection].UpdateInput(inputMsg.input);
                        }
                        else
                        {
                            Debug.LogError("NetworkID Mismatch for Player Input" +
                                            "Connection ID: " + serv.playerInstances[connection].networkID +
                                            "Input ID: " + inputMsg.networkID);
                        }
                    }
                    else
                    {
                        serv.playerInstances[connection].UpdateInput(stop);

                    }
                }
                else
                {
                    Debug.LogError("Received player input from unlisted connection");
                }

                //BroadCast to Clients
                serv.SendBroadcast(inputMsg);
            }
            

        }

        public static void HandleClientExit(Server serv, NetworkConnection connection, MessageHeader header)
        {
            GameQuitMessage msg = header as GameQuitMessage;

            if (serv.nameList.ContainsKey(connection))
            {
                //Destroy Connection owned Objects on Server and Clients
                foreach (NetworkObject netObject in serv.playerInfo[msg.clientID].objectList)
                {
                    NetworkDestroyMessage destroyMsg = new NetworkDestroyMessage
                    {
                        networkID = netObject.networkID
                    };
                    serv.networkManager.DestroyWithID(netObject.networkID);
                    serv.SendBroadcast(destroyMsg);

                }
                //Remove from Dictionary
                if (serv.playerInstances.ContainsKey(connection))
                {
                    serv.playerInstances.Remove(connection);
                }
                //Remove PlayerCard
                serv.server_UI.DisconnectPlayer(serv, msg.clientID);
                //Remove from nameList
                if (serv.nameList.ContainsKey(connection))
                {
                    serv.nameList.Remove(connection);
                }
                //Remove form PingList
                serv.pongDict.Remove(connection);

                connection.Disconnect(serv.m_Driver);
                connection = default;
                Debug.Log("Client ID: " + msg.clientID + " Has Disconected");

            }
            else
            {
                Debug.LogError("Received exit from unlisted connection");
            }

        }
        public static void HandleClientLobbeyMessage(Server serv, NetworkConnection connection, MessageHeader header)
        {
            GameLobbyMessage msg = header as GameLobbyMessage;

            if (serv.nameList.ContainsKey(connection))
            {
                ServerSettings.LeaveTeam(serv, serv.playerInfo[msg.clientID]);

                Debug.Log($"Client: {serv.playerInfo[msg.clientID].clientID} ObjectListCount: {serv.playerInfo[msg.clientID].objectList.Count}");

                //Destroy Connection owned Objects on Server and Clients
                NetworkDestroyMultipleMessage destroyMsg = new NetworkDestroyMultipleMessage();

                for (int i = 0; i < serv.playerInfo[msg.clientID].objectList.Count; i++)
                {
                    if (serv.playerInfo[msg.clientID].objectList[i] != null)
                    {
                        //Debug.Log($"NetworkObject: {serv.playerInfo[msg.clientID].objectList[i].networkID} Destroyed");

                        //Remove from PlayerInstances
                        if (serv.playerInfo[msg.clientID].objectList[i].type == NetworkSpawnObject.PLAYER)
                        {
                            serv.playerInstances.Remove(serv.playerInfo[msg.clientID].connection);
                        }

                        serv.networkManager.DestroyWithID(serv.playerInfo[msg.clientID].objectList[i].networkID);

                        destroyMsg.networkIDs.Add(serv.playerInfo[msg.clientID].objectList[i].networkID);
                    }
                }

                Debug.Log(destroyMsg.networkIDs.Count);
                serv.SendBroadcast(destroyMsg);


                CallOnFunctionMessage lobbyMsg = new CallOnFunctionMessage
                {
                    methodName = "ToggleLobbyUI"
                };
                serv.SendReply(connection, lobbyMsg);

                Debug.Log("Client ID: " + msg.clientID + " Has Returned to Lobby");
            }
            else
            {
                Debug.LogError("Received exit from unlisted connection");
            }

        }
        static void HandleClientMessage(Server serv, NetworkConnection connection, MessageHeader header)
        {
            ChatMessage receivedMsg = header as ChatMessage;

            Debug.Log("CLIENT MESSAGE");
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
                if (serv.playerInstances[connection].networkID == msg.networkID)
                {
                    serv.networkManager.DestroyWithID(msg.networkID);
                }

            }
        }
        static void HandleSpawnMessage(Server serv, NetworkConnection connection, MessageHeader header)
        {
            NetworkSpawnMessage msg = header as NetworkSpawnMessage;

            GameObject obj;
            if(serv.networkManager.SpawnWithID((NetworkSpawnObject)msg.objectType, NetworkManager.NextNetworkID,msg.clientID, msg.teamID, msg.pos, msg.rot, out obj)){

                msg.networkID = obj.GetComponent<NetworkObject>().networkID;
                serv.SendBroadcast(msg);
            }
        }
        static void HandleRPCMessage(Server serv, NetworkConnection connection, MessageHeader header)
        {
            RPCMessage msg = header as RPCMessage;

            //Debug.Log($"RPC: {msg.methodName}");
            //Try to call function
            try
            {
                msg.mInfo.Invoke(msg.target, msg.data);
            }
            catch (System.Exception e)
            {
                Debug.Log(e);
            }
        }
        static void HandlePongMessage(Server serv, NetworkConnection connection, MessageHeader header)
        {
            PongMessage msg = header as PongMessage;
            // Debug.Log("PONG");
            //Set ID if needed
            if (serv.pongDict[connection].clientID == 0)
            {
                serv.pongDict[connection].clientID = msg.clientID;
            }
            serv.pongDict[connection].status = 3;   //reset retry count
        }
        static void HandleClientInfoMessage(Server serv, NetworkConnection connection, MessageHeader header)
        {
            ClientInfoMessage msg = header as ClientInfoMessage;

            //Join Team and Set Ready
            ServerSettings.JoinTeam(serv, serv.playerInfo[msg.clientID], (Team)msg.teamNum);

        }
        static void HandleServerInfoMessage(Server serv, NetworkConnection connection, MessageHeader header)
        {
            ServerInfoMessage msg = header as ServerInfoMessage;

            //serv.server_UI.UpdateServerSettings();
        }
        static void HandlePlayerInfoMessage(Server serv, NetworkConnection connection, MessageHeader header)
        {
            PlayerInfoMessage msg = header as PlayerInfoMessage;

            serv.playerInfo[msg.info.clientID].currentZone = msg.info.currentZone;
        }

        //Server functions

        public void SendBroadcast(MessageHeader header)
        {
            //Debug.Log($"Msg Send {header.Type} to {m_Connections.Length} Clients");
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
        public void BundelInputUpdate(params InputUpdateMessage[] bundel)
        {
            for (int i = 0; i < bundel.Length; i++)
            {
                InputUpdateMessage msg = new InputUpdateMessage();

                msg.networkID = bundel[i].networkID;

                foreach (InputUpdateMessage input in bundel)
                {
                    if(msg.networkID == input.networkID)
                    {
                        msg.input.horizontal = msg.input.horizontal + input.input.horizontal;
                        msg.input.vertical = msg.input.vertical + input.input.vertical;
                    }
                    
                }

                SendBroadcast(msg);

            }
        }

        public void ClearGameFunction()//Function to call on UI element
        {
            gameManager.ClearGame();
        }


    }
}