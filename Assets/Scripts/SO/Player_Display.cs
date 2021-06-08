using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChatClientExample
{
    public class Player_Display : MonoBehaviour
    {
        public PlayerInfo player;

        public Text nameText;
        public Text clientStatusText;
        public Text teamText;

        public uint num;
        public uint playerCardNumber { get{ return num; }}

        // Start is called before the first frame update
        void Start()
        {
            nameText.text = player.playerName;
            clientStatusText.text = player.state.ToString();
            teamText.text = player.teamNum.ToString();

        }

        public void UpdateInfo(PlayerInfo info)
        {
            nameText.text = info.playerName;
            clientStatusText.text = info.state.ToString();
            teamText.text = info.teamNum.ToString();
        }
    }
}

