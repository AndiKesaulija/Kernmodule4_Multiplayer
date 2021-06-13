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
        INTERMISSION,
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
        UPDATE_NETWORK_OBJECT,
        PLAYER_INFO,
        PLAYER_OUTOFBOUNDS,
        GAME_QUIT,
        RPC,
        CLIENT_STATE,
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
            //{ NetworkMessageType.CHAT_QUIT,                 typeof(ChatQuitMessage) },
            { NetworkMessageType.NETWORK_SPAWN,             typeof(NetworkSpawnMessage) },
            { NetworkMessageType.PLAYER_SPAWN,              typeof(NetworkPlayerSpawnMessage) },
            { NetworkMessageType.NETWORK_DESTROY,           typeof(NetworkDestroyMessage) },
            { NetworkMessageType.PLAYER_INFO,               typeof(PlayerInfoMessage) },
            { NetworkMessageType.PLAYER_OUTOFBOUNDS,        typeof(OutOfBoundsMessage) },
            //{ NetworkMessageType.NETWORK_UPDATE_POSITION,   typeof(UpdatePositionMessage) },
            { NetworkMessageType.INPUT_UPDATE,              typeof(InputUpdateMessage) },
            { NetworkMessageType.UPDATE_NETWORK_OBJECT,     typeof(UpdateNetworkObjectMessage) },
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
        private Dictionary<NetworkConnection, NetworkPlayer> playerInstances = new Dictionary<NetworkConnection, NetworkPlayer>();
        private Dictionary<NetworkConnection, PingPong> pongDict = new Dictionary<NetworkConnection, PingPong>();

        public Dictionary<uint, PlayerInfo> playerInfo = new Dictionary<uint, PlayerInfo>();

        public NetworkManager networkManager;
        //public Server_LobbyManager lobbyManager;
        public Server_UI server_UI;
        public GameState gameState;
        public SpawnPoint spawnPoints = new SpawnPoint();

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

                server_UI.UpdatePlayerCard(this, player.Value.clientID);

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
            if (gameState == GameState.INTERMISSION)
            {
                //Check if all players Ready
                ReadyCheck();
            }
            if (gameState == GameState.IN_GAME)
            {
                //Check if round is over
                if(CheckOutOfBounds(server_UI.teamRed) == true)
                {
                    if(ServerSettings.activeZone > 1)
                    {
                        ServerSettings.activeZone = ServerSettings.activeZone - 1;
                        gameState = GameState.INTERMISSION;

                        SpawnPlayers();
                    }
                    else
                    {
                        //EndGame
                        gameState = GameState.LOBBY;
                        EndRound();
                    }
                    
                }
                if (CheckOutOfBounds(server_UI.teamBlue) == true)
                {
                    if (ServerSettings.activeZone < 5)
                    {
                        ServerSettings.activeZone = ServerSettings.activeZone + 1;

                        gameState = GameState.INTERMISSION;

                        SpawnPlayers();
                    }
                    else
                    {
                        //EndGame
                        gameState = GameState.LOBBY;
                        EndRound();
                    }

                }

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
                    SpawnPlayers();//start in zone 3 and 4
                    gameState = GameState.INTERMISSION;
                }

            }
        }
        public void ReadyCheck()
        {

            bool clientsReady()
            {
                if (server_UI.teamRed.Count == ServerSettings.maxTeamPlayerCount && server_UI.teamBlue.Count == ServerSettings.maxTeamPlayerCount)
                {
                    foreach (PlayerInfo player in server_UI.teamRed)
                    {
                        if (player.playerState != PlayerState.READY)
                        {
                            Debug.Log($"Player from team Red: {player.clientID} is not Ready");
                            return false;
                        }
                    }
                    foreach (PlayerInfo player in server_UI.teamBlue)
                    {
                        if (player.playerState != PlayerState.READY)
                        {
                            Debug.Log($"Player from team Blue: {player.clientID} is not Ready");
                            return false;
                        }
                    }

                    return true;
                }
                return false;
            }



            if (clientsReady() == true)
            {
                gameState = GameState.IN_GAME;
            }
        }
        public bool CheckOutOfBounds(List<PlayerInfo> team)
        {
            bool outofbounds = true;

            for (int i = 0; i < team.Count; i++)
            {
                if(team[i].playerState != PlayerState.OUT_OF_BOUNDS)
                {
                    outofbounds = false;
                }
            }

            return outofbounds;

        }

        public void SpawnPlayers()
        {
            //CleanUp
            EndRound();

            //Spawn Players on SERVER
            foreach (KeyValuePair<uint, PlayerInfo> player in playerInfo)
            {
                uint networkID = NetworkManager.NextNetworkID;

                
                Vector3 rot= new Vector3();

                if(player.Value.team == Team.RED)
                {
                    rot = new Vector3(0, 180, 0);
                }
                if (player.Value.team == Team.BLUE)
                {
                    rot = new Vector3(0, 0, 0);
                }
                GameObject newPlayer;
                if (networkManager.SpawnWithID(NetworkSpawnObject.PLAYER, networkID, player.Value.clientID, 0, spawnPos(player.Value),rot , out newPlayer))
                {
                    NetworkPlayer playerInstance = newPlayer.GetComponent<NetworkPlayer>();
                    playerInstance.isServer = true;
                    playerInstance.isLocal = false;

                    playerInstance.teamID = (uint)player.Value.team;//Temp TeamID
                    playerInstance.clientID = player.Value.clientID;

                    playerInstances.Add(player.Value.connection, playerInstance);

                    player.Value.networkID = playerInstance.networkID;
                    player.Value.spawnPos = spawnPos(player.Value);
                    player.Value.spawnRot = rot;
                    player.Value.objectList.Add(playerInstance);

                    if(player.Value.team == Team.RED)
                    {
                        player.Value.activeZone = ServerSettings.activeZone;
                    }
                    if (player.Value.team == Team.BLUE)
                    {
                        player.Value.activeZone = ServerSettings.activeZone + 1;
                    }

                    //Spawn player on Client
                    NetworkPlayerSpawnMessage spawnMsg = new NetworkPlayerSpawnMessage
                    {
                        networkID = networkID,
                        objectType = (uint)NetworkSpawnObject.PLAYER,
                        pos = player.Value.spawnPos,
                        rot = player.Value.spawnRot
                    };

                    SendReply(player.Value.connection, spawnMsg);
                }
                else
                {
                    Debug.LogError("Could not spawn player instance");
                }
               
            }
            //Spawn Other players on Client
            foreach (KeyValuePair<uint, PlayerInfo> player in playerInfo)
            {
                foreach (KeyValuePair<uint, PlayerInfo> networkPlayer in playerInfo)
                {
                    if (player.Value.networkID == networkPlayer.Value.networkID)
                    {
                        //Debug.Log($"Same Key: {networkPlayer.Value.networkID} {player.Value.networkID}");
                        continue;
                    }

                    NetworkSpawnMessage clientMsg = new NetworkSpawnMessage
                    {
                        networkID = networkPlayer.Value.networkID,
                        objectType = (uint)NetworkSpawnObject.PLAYER,
                        pos = networkPlayer.Value.spawnPos,
                        rot = networkPlayer.Value.spawnRot

                    };

                    
                    SendReply(player.Value.connection, clientMsg);
                }
               
            }

        }
        public Vector3 spawnPos(PlayerInfo info)
        {
            if(info.team == Team.RED)
            {
                Vector3 spawnPos = spawnPoints.zones[ServerSettings.activeZone].spot[info.teamPos];
                return spawnPos;

            }
            if (info.team == Team.BLUE)
            {
                Vector3 spawnPos = spawnPoints.zones[ServerSettings.activeZone + 1].spot[info.teamPos];
                return spawnPos;

            }

            Debug.Log("Invalid Spawn Position");
            return Vector3.zero;
        }

        public void HandleOutOfBounds(uint clientID, uint networkID)
        {
            //Destroy Object and set playerinfo.PlayerState.OUT_OF_BOUNDS

            Debug.Log($"Destroy Player: {networkManager.networkedReferences[networkID].name}");

            networkManager.DestroyWithID(networkID);
            server_UI.SetPlayerState(this, clientID, (int)PlayerState.OUT_OF_BOUNDS);


            OutOfBoundsMessage msg = new OutOfBoundsMessage
            {
                clientID = clientID,
                networkID = networkID
            };

            SendBroadcast(msg);
        }
        public void EndRound()
        {
            foreach (KeyValuePair<uint, PlayerInfo> player in playerInfo)
            {
                foreach (NetworkObject netObject in player.Value.objectList)
                {
                    networkManager.DestroyWithID(netObject.networkID);

                    NetworkDestroyMessage msg = new NetworkDestroyMessage
                    {
                        networkID = netObject.networkID
                    };
                    SendBroadcast(msg);
                }

                playerInstances.Remove(player.Value.connection);
            }
        }
        static void HandleClientHandshake(Server serv, NetworkConnection connection, MessageHeader header)
        {
            HandshakeMessage message = header as HandshakeMessage;

            // Add to list
            serv.nameList.Add(connection, message.name);

            string msg = $"{message.name.ToString()} has joined the Game.";
            Debug.Log($"{msg.ToString()} has joined the chat.");
            
            uint networkID = NetworkManager.NextNetworkID;
            uint clientID = NetworkManager.NextClientID;


            //Add client to LobbyManager
            PlayerInfo info = new PlayerInfo
            {
                networkID = networkID,
                clientID = clientID,
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

            if (serv.gameState == GameState.IN_GAME)
            {
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
            if(serv.networkManager.SpawnWithID((NetworkSpawnObject)msg.objectType, NetworkManager.NextNetworkID,msg.clientID, msg.teamID, msg.pos, msg.rot, out obj)){

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
        static void HandlePlayerInfoMessage(Server serv, NetworkConnection connection, MessageHeader header)
        {
            PlayerInfoMessage msg = header as PlayerInfoMessage;

            serv.playerInfo[msg.info.networkID].currentZone = msg.info.currentZone;
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
    }
}