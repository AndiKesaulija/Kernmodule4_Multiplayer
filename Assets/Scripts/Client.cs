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
            { NetworkMessageType.HANDSHAKE_RESPONSE,    HandshakeResponseHandler },
            { NetworkMessageType.CHAT_MESSAGE,          HandleChatMessage },
            { NetworkMessageType.NETWORK_SPAWN,         HandleSpawnMessage },
            { NetworkMessageType.INPUT_UPDATE,          HandleInputMessage },
            { NetworkMessageType.NETWORK_DESTROY,       HandleDestroyMessage },



        };


        public uint clientID;

        public NetworkDriver m_Driver;
        public NetworkConnection m_Connection;
        public bool Done;

        public string myMessage;
        public ChatCanvas chat;

        public NetworkManager networkManager;

        private InputUpdate myInput;

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
                        name = "Andi"
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

            client.clientID = response.clientID;
            client.chat.InvokeMessage(response.message, client.chat.chatMessages);

            GameObject obj;
            if (client.networkManager.SpawnWithID(NetworkSpawnObject.PLAYER, response.networkID,new Vector3(0,0,0), out obj))
            {
                NetworkPlayer player = obj.GetComponent<NetworkPlayer>();
                player.isLocal = true;
                player.isServer = false;
            }
            else
            {
                Debug.LogError("Could not spawn player!");
            }


        }
        static void HandleChatMessage(Client client, MessageHeader header)
        {
            ChatMessage msg = header as ChatMessage;
            Debug.Log(msg.message);

            client.chat.InvokeMessage(msg.message, client.chat.chatMessages);

        }
        static void HandleSpawnMessage(Client client, MessageHeader header)
        {
            NetworkSpawnMessage msg = header as NetworkSpawnMessage;

            GameObject obj;
            if(!client.networkManager.SpawnWithID(
                                                (NetworkSpawnObject)msg.objectType,
                                                msg.networkID,
                                                new Vector3(msg.posx,msg.posy,msg.posz),
                                                out obj))
            {
                client.chat.InvokeMessage("Client ID: " + msg.networkID, client.chat.chatMessages);
                Debug.Log("Spawn Failed");
            }
            else
            {
                Debug.Log("Spawn Player:" + msg.networkID + "Pos: " + new Vector3(msg.posx, msg.posy, msg.posz));
                if(msg.networkID == client.clientID)
                {
                    obj.GetComponent<NetworkPlayer>().isLocal = true;
                }
                else
                {
                    obj.GetComponent<NetworkPlayer>().isServer = true;

                }
                obj.transform.position = new Vector3(msg.posx, msg.posy, msg.posz);
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
        public void SendInput(InputUpdate myInput)
        {
            InputUpdateMessage msg = new InputUpdateMessage
            {
                networkID = clientID,
                input = myInput
            };

            SendPackedMessage(msg);
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
                networkID = clientID
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


    }
}
