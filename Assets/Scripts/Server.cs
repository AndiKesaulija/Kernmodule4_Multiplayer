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
    public enum GameState
    {
        LOBBY,
        IN_GAME
    }
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
        INPUT_UPDATE,
        GAME_QUIT,
        RPC,
        CLIENT_STATE,
        CLIENT_INFO,
        SERVER_INFO,
        PING,
        PONG,
        REQUEST
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
            //{ NetworkMessageType.CHAT_QUIT,                 typeof(ChatQuitMessage) },
            { NetworkMessageType.NETWORK_SPAWN,             typeof(NetworkSpawnMessage) },
            { NetworkMessageType.PLAYER_SPAWN,              typeof(NetworkPlayerSpawnMessage) },
            { NetworkMessageType.NETWORK_DESTROY,           typeof(NetworkDestroyMessage) },
            //{ NetworkMessageType.NETWORK_UPDATE_POSITION,   typeof(UpdatePositionMessage) },
            { NetworkMessageType.INPUT_UPDATE,              typeof(InputUpdateMessage) },
            { NetworkMessageType.PING,                      typeof(PingMessage) },
            { NetworkMessageType.PONG,                      typeof(PongMessage) },
            { NetworkMessageType.GAME_QUIT,                 typeof(QuitMessage) },
            { NetworkMessageType.RPC,                       typeof(RPCMessage) },
            { NetworkMessageType.CLIENT_STATE,              typeof(ClientStateMessage) },
            { NetworkMessageType.CLIENT_INFO,               typeof(ClientInfoMessage) },
            { NetworkMessageType.SERVER_INFO,               typeof(ServerInfoMessage) },




        };
    }

    public class Server : MonoBehaviour
    {

        static Dictionary<NetworkMessageType, ServerMessageHandler> networkMessageHandlers = new Dictionary<NetworkMessageType, ServerMessageHandler> {
            { NetworkMessageType.HANDSHAKE,                 HandleClientHandshake },
            { NetworkMessageType.CHAT_MESSAGE,              HandleClientMessage },
            { NetworkMessageType.GAME_QUIT,                 HandleClientExit },
            { NetworkMessageType.INPUT_UPDATE,              HandleInputMessage },
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
        private Dictionary<NetworkConnection, NetworkPlayer> playerInstances = new Dictionary<NetworkConnection, NetworkPlayer>();
        private Dictionary<NetworkConnection, PingPong> pongDict = new Dictionary<NetworkConnection, PingPong>();

        public Dictionary<uint, PlayerInfo> playerInfo = new Dictionary<uint, PlayerInfo>();

        public NetworkManager networkManager;
        //public Server_LobbyManager lobbyManager;
        public Server_UI server_UI;
        public GameState gameState;

        void Start()
        {
            gameState = GameState.LOBBY;

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
                            Debug.Log($"Disconnecting: Client {pongDict[m_Connections[i]].clientID}");

                            //Destroy PlayerObject
                            foreach(NetworkObject netObject in playerInfo[pongDict[m_Connections[i]].clientID].objectList)
                            {
                                networkManager.DestroyWithID(netObject.networkID);
                            }
                            
                            //Remove PlayerCard
                            server_UI.DisconnectPlayer(this, pongDict[m_Connections[i]].clientID);

                            //Remove from nameList
                            if (nameList.ContainsKey(m_Connections[i]))
                            {
                                nameList.Remove(m_Connections[i]);
                            }

                            //Remove form PingList
                            pongDict.Remove(m_Connections[i]);
                            //Disconnect connection
                            m_Connections[i].Disconnect(m_Driver);
                            m_Connections[i] = default;
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
                    Debug.Log($"Added {m_Connections[i]} to Ping List.");

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

            //TEST START GAME

            if(gameState == GameState.LOBBY)
            {
                //Check Teams
                CheckTeamStatus();
            }
            
        }
        public void CheckTeamStatus()
        {
            //Check if teams are full
            if (ServerSettings.redTeamPlayerCount >= ServerSettings.maxTeamPlayerCount && ServerSettings.blueTeamPlayerCount >= ServerSettings.maxTeamPlayerCount)
            {
                if(gameState == GameState.LOBBY)
                {
                    //Spawn Players
                    SpawnPlayers();
                    gameState = GameState.IN_GAME;
                }

                //ReadyCheck();
            }
            //Initiate Game

            //Check if teams are Ready

            //Start Round
        }
        public void SpawnPlayers()
        {
            //Spawn Players on SERVER
            foreach (KeyValuePair<uint, PlayerInfo> player in playerInfo)
            {
                uint networkID = NetworkManager.NextNetworkID;
                GameObject newPlayer;
                if (networkManager.SpawnWithID(NetworkSpawnObject.PLAYER, networkID, networkID, new Vector3(0, 0, 0), out newPlayer))
                {
                    NetworkPlayer playerInstance = newPlayer.GetComponent<NetworkPlayer>();
                    playerInstance.isServer = true;
                    playerInstance.isLocal = false;

                    playerInstance.teamID = playerInstance.networkID;//Temp TeamID
                    networkID = playerInstance.networkID;

                    playerInstances.Add(player.Value.connection, playerInstance);

                    player.Value.objectList.Add(playerInstance);
                }
                else
                {
                    Debug.LogError("Could not spawn player instance");
                }
                //Spawn player on Client
                NetworkPlayerSpawnMessage spawnMsg = new NetworkPlayerSpawnMessage
                {
                    networkID = networkID,
                    objectType = (uint)NetworkSpawnObject.PLAYER,
                    pos = new Vector3(0,0,0)
                };

                SendReply(player.Value.connection, spawnMsg);
               
            }
            //Spawn Other players on Client
            foreach (KeyValuePair<uint, PlayerInfo> player in playerInfo)
            {
                foreach (KeyValuePair<NetworkConnection, NetworkPlayer> networkPlayer in playerInstances)
                {
                    if (networkPlayer.Key == player.Value.connection)
                    {
                        Debug.Log($"Same Key: {networkPlayer.Key} {player.Key}");
                        continue;
                    }

                    NetworkSpawnMessage clientMsg = new NetworkSpawnMessage
                    {
                        networkID = networkPlayer.Value.networkID,
                        objectType = (uint)NetworkSpawnObject.PLAYER,
                        pos = new Vector3(0, 0, 0)
                    };

                    SendReply(player.Value.connection, clientMsg);
                }
            }

            //// Send all existing players to this player

            //Spawn other players


        }
        public void ReadyCheck()
        {

            //bool clientsReady()
            //{
            //    if (server_UI.teamRed.Count == ServerSettings.maxTeamPlayerCount && server_UI.teamBlue.Count == ServerSettings.maxTeamPlayerCount)
            //    {
            //        foreach (PlayerInfo player in server_UI.teamRed)
            //        {
            //            if (player.playerState != PlayerState.READY)
            //            {
            //                Debug.Log($"Player from team Red: {player.clientID} is not Ready");
            //                return false;
            //            }
            //        }
            //        foreach (PlayerInfo player in server_UI.teamBlue)
            //        {
            //            if (player.playerState != PlayerState.READY)
            //            {
            //                Debug.Log($"Player from team Blue: {player.clientID} is not Ready");
            //                return false;
            //            }
            //        }

            //        return true;
            //    }
            //    return false;
            //}



            //if (clientsReady() == true)
            //{
            //    StartRound();
            //}
        }
       
        public void StartRound()
        {
            //Spawn player on SERVER
            foreach (KeyValuePair<uint, PlayerInfo> player in playerInfo)
            {

                if (player.Value.team != Team.SPECTATOR)
                {
                    player.Value.clientstate = ClientState.IN_GAME;
                    player.Value.playerState = PlayerState.NOT_READY;
                }

                

            }

            //Spawn all players to clients

            ////SetClientState to IN_GAME
            //foreach (KeyValuePair<uint,PlayerInfo> pair in server_UI.playerInfo)
            //{
            //    if(pair.Value.team != Team.SPECTATOR)
            //    {
            //        pair.Value.clientstate = ClientState.IN_GAME;
            //        pair.Value.playerState = PlayerState.NOT_READY;
            //    }
            //}

            server_UI.UpdatePlayerCards(this);
            Debug.Log("StartRound");

        }

        static void HandleClientHandshake(Server serv, NetworkConnection connection, MessageHeader header)
        {
            HandshakeMessage message = header as HandshakeMessage;

            // Add to list
            serv.nameList.Add(connection, message.name);

            string msg = $"{message.name.ToString()} has joined the Game.";
            Debug.Log($"{msg.ToString()} has joined the chat.");
            
            //uint clientID = NetworkManager.NextNetworkID;
            uint clientID = NetworkManager.NextNetworkID;

            //Add client to LobbyManager
            PlayerInfo info = new PlayerInfo
            {
                clientID = clientID,
                playerID = 0,
                playerName = message.name,
                clientstate = ClientState.IN_LOBBY,
                team = 0,
                connection = connection
            };

            //Add Client to Server_UI
            serv.server_UI.AddPlayerCard(serv, info);

            HandshakeResponseMessage responseMsg = new HandshakeResponseMessage
            {
                message = $"Welcome {message.name.ToString()}!",
                clientID = clientID,
            };
            serv.SendReply(connection, responseMsg);

            ServerInfoMessage serverInfo = new ServerInfoMessage();
            serv.SendReply(connection, serverInfo);


            ////Spawn LOCAL Player Object
            //GameObject newPlayer;
            //if (serv.networkManager.SpawnWithID(NetworkSpawnObject.PLAYER, networkId, networkId, new Vector3(0, 0, 0), out newPlayer))
            //{
            //    NetworkPlayer playerInstance = newPlayer.GetComponent<NetworkPlayer>();
            //    playerInstance.isServer = true;
            //    playerInstance.isLocal = false;

            //    playerInstance.teamID = playerInstance.networkID;//Temp TeamID
            //    networkId = playerInstance.networkID;

            //    serv.playerInstances.Add(connection, playerInstance);

            //    // Send spawn local player back to sender
            //    HandshakeResponseMessage responseMsg = new HandshakeResponseMessage
            //    {
            //        //clientID = NetworkManager.NextNetworkID,
            //        message = $"Welcome {message.name.ToString()}!",
            //        networkID = playerInstance.networkID,

            //    };

            //    serv.SendReply(connection, responseMsg);

            //}
            //else
            //{
            //    Debug.LogError("Could not spawn player instance");
            //}

            //// Send all existing players to this player
            //foreach (KeyValuePair<NetworkConnection, NetworkPlayer> pair in serv.playerInstances)
            //{
            //    if (pair.Key == connection)
            //    {
            //        Debug.Log($"Same Key: {pair.Key} {connection}");
            //        continue;
            //    }

            //    NetworkSpawnMessage spawnMsg = new NetworkSpawnMessage
            //    {
            //        networkID = pair.Value.networkID,
            //        objectType = (uint)NetworkSpawnObject.PLAYER,
            //        pos = pair.Value.transform.position
            //    };

            //    serv.SendReply(connection, spawnMsg);
            //}

            //// Send creation of this player to all existing players
            //if (networkId != 0)
            //{
            //    for (int i = 0; i < serv.m_Connections.Length; i++)
            //    {
            //        NetworkSpawnMessage spawnMsg = new NetworkSpawnMessage
            //        {
            //            networkID = networkId,
            //            objectType = (uint)NetworkSpawnObject.PLAYER
            //        };
            //        serv.SendReply(serv.m_Connections[i], spawnMsg);
            //    }

            //}
            //else
            //{
            //    Debug.LogError("Invalid network id for broadcasting creation");
            //}



        }
        static void HandleClientInfoMessage(Server serv, NetworkConnection connection, MessageHeader header)
        {
            ClientInfoMessage msg = header as ClientInfoMessage;

            //Join Team and Set Ready
            serv.server_UI.JoinTeam(serv, msg.clientID, msg.teamNum);
            //serv.server_UI.playerInfo[msg.clientID].state = ClientState.READY;

            //Update Clients
            ServerInfoMessage servMsg = new ServerInfoMessage();
            serv.SendBroadcast(servMsg);
        }
        static void HandleInputMessage(Server serv, NetworkConnection connection, MessageHeader header)
        {
            InputUpdateMessage inputMsg = header as InputUpdateMessage;

            if (serv.playerInstances.ContainsKey(connection))
            {
                if (serv.playerInstances[connection].networkID == inputMsg.networkID)
                {
                    serv.playerInstances[connection].UpdateInput(inputMsg.input);
                }
                else
                {
                    Debug.LogError("NetworkID Mismatch for Player Input" + 
                                    "Connection ID: " + serv.playerInstances[connection].networkID + 
                                    "InputID: " + inputMsg.networkID);
                }
            }
            else
            {
                Debug.LogError("Received player input from unlisted connection");
            }

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
                //Remove from Dictionary
                if (serv.playerInstances.ContainsKey(connection))
                {
                    serv.playerInstances.Remove(connection);
                }
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
            if(serv.networkManager.SpawnWithID((NetworkSpawnObject)msg.objectType, NetworkManager.NextNetworkID, msg.teamID, msg.pos, out obj)){

                msg.networkID = obj.GetComponent<NetworkObject>().networkID;
                serv.SendBroadcast(msg);
            }
        }
        static void HandleRPCMessage(Server serv, NetworkConnection connection, MessageHeader header)
        {
            RPCMessage msg = header as RPCMessage;

            Debug.Log($"RPC: {msg.methodName}");
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
        static void HandleServerInfoMessage(Server serv, NetworkConnection connection, MessageHeader header)
        {
            ServerInfoMessage msg = header as ServerInfoMessage;

            serv.server_UI.UpdateServerSettings();
        }




        //Server functions
        
        public void SendBroadcast(MessageHeader header)
        {
            Debug.Log($"Msg Send {header.Type} to {m_Connections.Length} Clients");
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