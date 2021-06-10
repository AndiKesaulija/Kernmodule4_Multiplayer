using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine.UI;

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
            { NetworkMessageType.INPUT_UPDATE,              HandleInputMessage },
            { NetworkMessageType.NETWORK_DESTROY,           HandleDestroyMessage },
            { NetworkMessageType.RPC,                       HandleRPCMessage },
            { NetworkMessageType.SERVER_INFO,               HandleServerInfoMessage },
            { NetworkMessageType.PING,                      HandlePing },



        };



        public NetworkDriver m_Driver;
        public NetworkConnection m_Connection;
        public bool Done;

        public string myMessage;
        public ChatCanvas chat;

        public NetworkManager networkManager;
        public Client_UI client_UI;
        public string clientName = "Player_Name";

        public uint clientID;
        private InputUpdate myInput;
        private uint teamID;



        void Start()
        {
            m_Driver = NetworkDriver.Create();
            m_Connection = default(NetworkConnection);

            //var endpoint = NetworkEndPoint.LoopbackIpv4;
            var endpoint = NetworkEndPoint.Parse("127.0.0.1", 1511);
            //endpoint.Port = 1511;
            m_Connection = m_Driver.Connect(endpoint);

        }
        public void OnDisable()
        {
            Disconect();
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
                        name = clientName
                    };
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

            //GameObject obj;
            //if (client.networkManager.SpawnWithID(NetworkSpawnObject.PLAYER, response.networkID, response.networkID, new Vector3(0, 0, 0), out obj))
            //{
            //    NetworkPlayer player = obj.GetComponent<NetworkPlayer>();
            //    player.isLocal = true;
            //    player.isServer = false;
            //}
            //else
            //{
            //    Debug.LogError("Could not spawn player!");
            //}


        }
        static void HandleChatMessage(Client client, MessageHeader header)
        {
            ChatMessage msg = header as ChatMessage;
            Debug.Log(msg.message);

            client.chat.InvokeMessage(msg.message, client.chat.chatMessages);

        }
        static void HandlePlayerSpawnMessage(Client client, MessageHeader header)
        {
            NetworkPlayerSpawnMessage msg = header as NetworkPlayerSpawnMessage;

            GameObject obj;
            if (!client.networkManager.SpawnWithID(
                                                (NetworkSpawnObject)msg.objectType,
                                                msg.networkID,
                                                msg.teamID,
                                                msg.pos,
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

                client.chat.InvokeMessage("Client ID: " + msg.networkID, client.chat.chatMessages);
            }
        }
        static void HandleSpawnMessage(Client client, MessageHeader header)
        {
            NetworkSpawnMessage msg = header as NetworkSpawnMessage;

            GameObject obj;
            if(!client.networkManager.SpawnWithID(
                                                (NetworkSpawnObject)msg.objectType,
                                                msg.networkID,
                                                msg.teamID,
                                                msg.pos,
                                                out obj))
            {
                Debug.Log("Spawn Failed");
            }
            else
            {
                obj.transform.position = msg.pos;
            }
        }
        static void HandleInputMessage(Client client, MessageHeader header)
        {
            InputUpdateMessage msg = header as InputUpdateMessage;
            client.networkManager.networkedReferences[msg.networkID].GetComponent<NetworkPlayer>().UpdateInput(msg.input);
        }
        static void HandleDestroyMessage(Client client, MessageHeader header)
        {
            NetworkDestroyMessage msg = header as NetworkDestroyMessage;

            if (client.networkManager.networkedReferences.ContainsKey(msg.networkID))
            {
                client.networkManager.DestroyWithID(msg.networkID);
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

            client.client_UI.UpdateServerSettings();
        }

        //Client functions
        public void SelectTeam(int teamNum)
        {
            ClientInfoMessage msg = new ClientInfoMessage
            {
                clientID = clientID,
                teamNum = (uint)teamNum
            };
            SendPackedMessage(msg);
            client_UI.CloseWindow(client_UI.window_TeamSelection);

        }

        public void SetTeam(Server serv, int teamNum)
        {
            serv.playerInfo[clientID].team = (Team)teamNum;
        }

        
        public void SendMyMessage()
        {
            ChatMessage chatMsg = new ChatMessage
            {
                message = myMessage
            };

            SendPackedMessage(chatMsg);
        }
        public void Disconect()
        {
            QuitMessage quitGame = new QuitMessage
            {
                //networkID = clientID
            };
            SendPackedMessage(quitGame);
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
