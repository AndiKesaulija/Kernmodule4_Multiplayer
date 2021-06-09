using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChatClientExample
{
    public class Server_UI : MonoBehaviour
    {

        public List<Player_Card> playerCards;
        public Dictionary<uint, PlayerInfo> playerInfo = new Dictionary<uint, PlayerInfo>();

        public int redTeamCounter = (int)ServerSettings.redTeamPlayerCount;
        public int blueTeamCounter = (int)ServerSettings.blueTeamPlayerCount;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            

        }

        public void ShowPlayerCard(PlayerInfo info)
        {
            //Bind playercard with playerInfo
            for (int i = 0; i < playerCards.Count; i++)
            {
                if (!playerCards[i].isActiveAndEnabled)
                {
                    info.cardNum = playerCards[i].playerCardNumber;
                    playerInfo.Add(info.clientID, info);

                    playerCards[i].gameObject.SetActive(true);
                    playerCards[i].player = info;
                    playerCards[i].UpdateInfo(info);


                    return;
                }
            }
        }
        public void DisconnectPlayer(uint networkID)
        {
            playerCards[(int)playerInfo[networkID].cardNum].gameObject.SetActive(false);

            playerInfo.Remove(networkID);

        }

        public void UpdatePlayerCard(uint networkID)
        {
            playerCards[(int)playerInfo[networkID].cardNum].UpdateInfo(playerInfo[networkID]);
        }
        public void UpdateServerSettings()
        {
            redTeamCounter = (int)ServerSettings.redTeamPlayerCount;
            blueTeamCounter = (int)ServerSettings.blueTeamPlayerCount;
        }
        public void UpdatePlayerCards()
        {
            for (int i = 0; i < playerCards.Count; i++)
            {
                if (playerCards[i].isActiveAndEnabled)
                {
                    playerCards[i].UpdateInfo(playerInfo[playerCards[i].player.clientID]);
                }
            }
        }

        public void JoinTeam(uint clientID, uint teamNum)
        {
            playerInfo[clientID].team = (Team)teamNum;

            if(teamNum == 1)
            {
                ServerSettings.redTeamPlayerCount++;
            }
            if (teamNum == 2)
            {
                ServerSettings.blueTeamPlayerCount++;
            }

            playerInfo[clientID].clientstate = ClientState.SPECTATING;

            playerCards[(int)playerInfo[clientID].cardNum].UpdateInfo(playerInfo[clientID]);
            UpdateServerSettings();

        }
        public void SetReady(uint networkID, int state)
        {
            playerInfo[networkID].playerState = (PlayerState)state;

            UpdatePlayerCard(networkID);

        }

    }
}

