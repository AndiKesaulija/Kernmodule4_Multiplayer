using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChatClientExample
{
    public class ChatCanvas : MonoBehaviour
    {
        // Start is called before the first frame update

        public static Color leaveColor = Color.red;
        public static Color chatColor = Color.white;

        private int messageCount = 5;
        public string[] chatMessages;
        public string chat;

        public Text text;

        void Start()
        {
            chatMessages = new string[messageCount];
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void NewMessage(string message, Color messageColor)
        {
            chatMessages[0] = message;
            Debug.Log(chatMessages[0]);
        }

        public void InvokeMessage(string message,string[] messageList)
        {
            string[] newMessageList = new string[messageCount];

            for (int i = 0; i < messageList.Length; i++)
            {
                if (i < messageCount - 1)
                {
                    newMessageList[i + 1] = messageList[i];
                }
            }

            newMessageList[0] = message + "\n";


            chatMessages = newMessageList;

            chat = null;

            foreach (string msg in chatMessages)
            {
                chat += msg;
            }

            text.text = chat;
        }
    }
}
