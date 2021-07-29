using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChatClientExample
{
    public class Player_Card : MonoBehaviour
    {
        public PlayerInfo player;

        public Text textId;
        public Text textTeam;
        public Text textName;
        public Text textZone;
        public Text textScore;


        public uint num;
        public uint playerCardNumber;

        // Start is called before the first frame update
        void Start()
        {
            textId.text = player.clientID.ToString();
            textTeam.text = player.team.ToString();
            textName.text = player.playerName;
            textZone.text = player.currentZone.ToString();
            textScore.text = "TODO";

            
        }

        public void UpdateInfo(PlayerInfo info)
        {
            Debug.Log(info.clientID);
            textId.text = info.clientID.ToString();
            textTeam.text = info.team.ToString();
            textName.text = info.playerName;
            textZone.text = info.currentZone.ToString();
            textScore.text = "TODO";
        }

    }
}

