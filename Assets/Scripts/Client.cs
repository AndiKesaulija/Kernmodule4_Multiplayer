using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


namespace ChatClientExample
{
    public class Client : MonoBehaviour
    {
        static Dictionary<NetworkMessageType, ClientMessageHandler> networkMessageHandlers = new Dictionary<NetworkMessageType, ClientMessageHandler>() {
            // link game events to functions...
            { NetworkMessageType.HANDSHAKE_RESPONSE,        HandshakeResponseHandler },
            { NetworkMessageType.CHAT_MESSAGE,              HandleChatMessage },
            { NetworkMessageType.NETWORK_SPAWN,             HandleSpawnMessage },
            { NetworkMessageType.PLAYER_SPAWN,              HandlePlayerSpawnMessage },
            { NetworkMessageType.PLAYER_OUTOFBOUNDS,        HandleOutOfBoundsMessage },
            { NetworkMessageType.INPUT_UPDATE,              HandleInputMessage },
            { NetworkMessageType.UPDATE_NETWORK_OBJECT,     HandleNetworkObjectUpdate },
            { NetworkMessageType.NETWORK_DESTROY,           HandleDestroyMessage },
            { NetworkMessageType.NETWORK_DESTROY_MULTIPLE,  HandleDestroyMultipleMessage },
            { NetworkMessageType.RPC,                       HandleRPCMessage },
            { NetworkMessageType.CALL_ON_FUNCTION,          HandleCOFMessage },
            { NetworkMessageType.SERVER_INFO,               HandleServerInfoMessage },
            { NetworkMessageType.PING,                      HandlePing },
            { NetworkMessageType.CLIENT_PLAYER_STATE,       HandlePlayerStateMessage},



        };



        public NetworkDriver m_Driver;
        public NetworkConnection m_Connection;
        public bool Done;

        public string myMessage;
        public ChatCanvas chat;

        public NetworkManager networkManager;
        public Client_UI client_UI;
        public string clientName = UserData.name;

        public uint clientID;
        public PlayerState playerstate;

        public Camera spectatorCam;



        void Start()
        {
            m_Driver = NetworkDriver.Create();
            m_Connection = default(NetworkConnection);

            var endpoint = NetworkEndPoint.Parse(UserData.ipAddress, UserData.port);

            m_Connection = m_Driver.Connect(endpoint);


        }
        public void OnDisable()
        {
            Disconect();
        }
        public void OnDestroy()
        {
            Disconect();
            m_Driver.Dispose();
        }
        void Update()
        {
            m_Driver.ScheduleUpdate().Complete();

            if (!m_Connection.IsCreated)
            {
                if (!Done)
                {
                    //Debug.Log("Something went wrong during connect");
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

                    var header = new HandshakeMessage
                    {
                        name = UserData.name,
                        userID = UserData.id
                    };
                    clientName = UserData.name;

                    SendPackedMessage(header);
                }
                //Response Data
                else if (cmd == NetworkEvent.Type.Data)
                {
                    // First UInt is always message type (this is our own first design choice)
                    NetworkMessageType msgType = (NetworkMessageType)stream.ReadUInt();

                    // TODO: Create message instance, and parse data...
                    MessageHeader header = (MessageHeader)System.Activator.CreateInstance(NetworkMessageInfo.TypeMap[msgType]);
                    header.DeserializeObject(ref stream);

                    if (networkMessageHandlers.ContainsKey(msgType))
                    {
                        networkMessageHandlers[msgType].Invoke(this, header);
                    }
                    else
                    {
                        Debug.LogWarning($"Unsupported message type received: {msgType}", this);
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
        static void HandshakeResponseHandler(Client client, MessageHeader header)
        {
            //SpawnPlayer
            HandshakeResponseMessage response = header as HandshakeResponseMessage;

            client.chat.InvokeMessage(response.message, client.chat.chatMessages);
            client.clientID = response.clientID;
        }
        static void HandleChatMessage(Client client, MessageHeader header)
        {
            ChatMessage msg = header as ChatMessage;
            Debug.Log(msg.message);

            client.chat.InvokeMessage(msg.message, client.chat.chatMessages);

        }
        static void HandlePlayerStateMessage(Client client, MessageHeader header)
        {
            ClientPlayerStateMessage msg = header as ClientPlayerStateMessage;
            if(msg.state == PlayerState.NOT_READY || msg.state == PlayerState.OUT_OF_BOUNDS)
            {
                client.client_UI.ToggleWindow(client.client_UI.setReadyText, true);
            }
            if (msg.state == PlayerState.READY)
            {
                client.client_UI.ToggleWindow(client.client_UI.setReadyText, false);
            }
            client.playerstate = msg.state;

        }

        static void HandlePlayerSpawnMessage(Client client, MessageHeader header)
        {
            NetworkPlayerSpawnMessage msg = header as NetworkPlayerSpawnMessage;

            GameObject obj;
            if (!client.networkManager.SpawnWithID(
                                                (NetworkSpawnObject)msg.objectType,
                                                msg.networkID,
                                                msg.clientID,
                                                msg.teamID,
                                                msg.pos,
                                                msg.rot,
                                                out obj))
            {
                Debug.Log("Spawn Failed");
            }
            else
            {
                NetworkPlayer player = obj.GetComponent<NetworkPlayer>();
                player.isLocal = true;
                player.isServer = false;

                obj.transform.position = msg.pos;

                //client.chat.InvokeMessage("Client ID: " + msg.networkID, client.chat.chatMessages);
            }
        }
        static void HandleSpawnMessage(Client client, MessageHeader header)
        {
            NetworkSpawnMessage msg = header as NetworkSpawnMessage;

            GameObject obj;
            if (!client.networkManager.SpawnWithID(
                                                (NetworkSpawnObject)msg.objectType,
                                                msg.networkID,
                                                msg.clientID,
                                                msg.teamID,
                                                msg.pos,
                                                msg.rot,
                                                out obj))
            {
                Debug.Log("Spawn Failed");
            }
            else
            {
                NetworkObject netobj = obj.GetComponent<NetworkObject>();
              
                obj.transform.position = msg.pos;
            }
        }
        static void HandleOutOfBoundsMessage(Client client, MessageHeader header)
        {
            OutOfBoundsMessage msg = header as OutOfBoundsMessage;

            client.networkManager.DestroyWithID(msg.networkID);

            if(msg.clientID == client.clientID)
            {
                client.spectatorCam.enabled = true;
            }
        }
        
        static void HandleInputMessage(Client client, MessageHeader header)
        {
            InputUpdateMessage msg = header as InputUpdateMessage;

            if (client.networkManager.networkedReferences[msg.networkID])
            {
                client.networkManager.networkedReferences[msg.networkID].GetComponent<NetworkPlayer>().UpdateInput(msg.input);
            }   
        }
        static void HandleDestroyMessage(Client client, MessageHeader header)
        {
            NetworkDestroyMessage msg = header as NetworkDestroyMessage;

            if (client.networkManager.networkedReferences.ContainsKey(msg.networkID))
            {
                ChatMessage logMsg = new ChatMessage
                {
                    message = $"DestroyMsg ID:{msg.networkID}"
                };

                client.networkManager.DestroyWithID(msg.networkID);
            }
            else
            {
                ChatMessage logMsg = new ChatMessage
                {
                    message = $"DestroyMsg ID:{msg.networkID}: FAILED"
                };
                HandleChatMessage(client, logMsg);
            }

        }
        static void HandleDestroyMultipleMessage(Client client, MessageHeader header)
        {
            NetworkDestroyMultipleMessage msg = header as NetworkDestroyMultipleMessage;

            Debug.Log($"Destroy {msg.networkIDs.Count} Objects");
            for (int i = 0; i < msg.networkIDs.Count; i++)
            {
                if (client.networkManager.networkedReferences.ContainsKey(msg.networkIDs[i]))
                {
                    ChatMessage logMsg = new ChatMessage
                    {
                        message = $"DestroyMsg ID:{msg.networkIDs[i]}"
                    };

                    client.networkManager.DestroyWithID(msg.networkIDs[i]);
                }
                else
                {
                    ChatMessage logMsg = new ChatMessage
                    {
                        message = $"DestroyMsg ID:{msg.networkIDs[i]}: FAILED"
                    };
                    HandleChatMessage(client, logMsg);
                }
            }
           

        }
        static void HandleRPCMessage(Client client, MessageHeader header)
        {
            RPCMessage msg = header as RPCMessage;

            //Try to call function
            try
            {
                msg.mInfo.Invoke(msg.target, msg.data);
            }
            catch (System.Exception e)
            {
                Debug.Log(e.Message);
                Debug.Log(e.StackTrace);
            }


        }
        static void HandleCOFMessage(Client client, MessageHeader header)
        {
            CallOnFunctionMessage msg = header as CallOnFunctionMessage;

            //Try to call function
            try
            {
                msg.mInfo.Invoke(msg.target, msg.data);
            }
            catch (System.Exception e)
            {
                Debug.Log(e.Message);
                Debug.Log(e.StackTrace);
            }


        }
        static void HandlePing(Client client, MessageHeader header)
        {
            Debug.Log("PING");

            PongMessage pongMsg = new PongMessage
            {
                clientID = client.clientID
            };

            client.SendPackedMessage(pongMsg);
        }

        static void HandleServerInfoMessage(Client client, MessageHeader header)
        {
            ServerInfoMessage msg = header as ServerInfoMessage;
        }

        static void HandleNetworkObjectUpdate(Client client, MessageHeader header)
        {
            UpdateNetworkObjectMessage msg = header as UpdateNetworkObjectMessage;

            client.networkManager.networkedReferences[msg.networkID].transform.position = msg.position;
        }

        //Client functions
        
        public void SendMyMessage()
        {
            ChatMessage chatMsg = new ChatMessage
            {
                message = myMessage
            };

            SendPackedMessage(chatMsg);
        }
        public void ReturnToLobby()
        {
            GameLobbyMessage msg = new GameLobbyMessage
            {
                clientID = clientID
            };

            SendPackedMessage(msg);
        }
        public void ToggleLobbyUI()
        {
            Debug.Log("ToggleUI");
            spectatorCam.enabled = true;
            client_UI.ToggleWindow(client_UI.window_TeamSelection);
        }
        public void Disconect()
        {
            GameQuitMessage quitGame = new GameQuitMessage
            {
                clientID = clientID
            };
            SendPackedMessage(quitGame);

            SceneManager.LoadScene("LoginClient");


        }


        //UIFunction
        public void SelectTeam(int teamNum)
        {
            ClientInfoMessage msg = new ClientInfoMessage
            {
                clientID = clientID,
                teamNum = (uint)teamNum
            };
            SendPackedMessage(msg);
            client_UI.ToggleWindow(client_UI.window_TeamSelection);

        }
        public void ReadInputField(string input)
        {
            myMessage = input;
        }

        public void SendPackedMessage(MessageHeader header)
        {
            DataStreamWriter writer;
            int result = m_Driver.BeginSend(NetworkPipeline.Null, m_Connection, out writer);

            // non-0 is an error code
            if (result == 0)
            {
                header.SerializeObject(ref writer);
                m_Driver.EndSend(writer);
            }
            else
            {
                Debug.LogError($"Could not wrote message to driver: {result}", this);
            }
        }

        //RPC
        public void CallOnServerObject(string function, NetworkObject target, params object[] data)
        {

            RPCMessage RPCmsg = new RPCMessage
            {
                target = target,
                methodName = function,
                data = data
            };

            SendPackedMessage(RPCmsg);
        }


       


    }
}
