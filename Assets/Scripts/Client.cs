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
            { NetworkMessageType.HANDSHAKE_RESPONSE, HandshakeResponseHandler },
            { NetworkMessageType.CHAT_MESSAGE, HandleChatMessage },


        };

        public NetworkDriver m_Driver;
        public NetworkConnection m_Connection;
        public bool Done;

        public string myMessage;
        public ChatCanvas chat;

        public NetworkManager networkManager;


        void Start()
        {
            m_Driver = NetworkDriver.Create();
            m_Connection = default(NetworkConnection);

            //var endpoint = NetworkEndPoint.LoopbackIpv4;
            var endpoint = NetworkEndPoint.Parse("127.0.0.1", 1511);
            //endpoint.Port = 1511;
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
            HandshakeResponseMessage response = header as HandshakeResponseMessage;
            FixedString128 str = response.message;
            Debug.Log(str);
        }
        static void HandleChatMessage(Client client, MessageHeader header)
        {
            ChatMessage msg = header as ChatMessage;
            Debug.Log(msg.message);

            client.chat.InvokeMessage(msg.message, client.chat.chatMessages);

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
            DataStreamWriter writer;
            int result = m_Driver.BeginSend(NetworkPipeline.Null, m_Connection, out writer);

            //non-0 is an error code
            if (result == 0)
            {
                writer.WriteUInt((uint)NetworkMessageType.CHAT_QUIT);
                m_Driver.EndSend(writer);

                //Done = true;
                //m_Connection.Disconnect(m_Driver);
                //m_Connection = default(NetworkConnection);
            }
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
