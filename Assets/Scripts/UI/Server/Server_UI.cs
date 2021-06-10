using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChatClientExample
{
    public class Server_UI : MonoBehaviour
    {

        public List<Player_Card> playerCards;

        public List<PlayerInfo> teamRed = new List<PlayerInfo>(3);
        public List<PlayerInfo> teamBlue = new List<PlayerInfo>(3);

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

        public void AddPlayerCard(Server serv, PlayerInfo info)
        {
            //Bind playercard with playerInfo
            for (int i = 0; i < playerCards.Count; i++)
            {
                if (!playerCards[i].isActiveAndEnabled)
                {
                    info.cardNum = playerCards[i].playerCardNumber;
                    serv.playerInfo.Add(info.clientID, info);

                    playerCards[i].gameObject.SetActive(true);
                    playerCards[i].player = info;
                    playerCards[i].UpdateInfo(info);


                    return;
                }
            }
        }
        public void DisconnectPlayer(Server serv,uint networkID)
        {
            playerCards[(int)serv.playerInfo[networkID].cardNum].gameObject.SetActive(false);

            serv.playerInfo.Remove(networkID);

        }

        public void UpdatePlayerCard(Server serv,uint networkID)
        {
            playerCards[(int)serv.playerInfo[networkID].cardNum].UpdateInfo(serv.playerInfo[networkID]);
        }
        public void UpdateServerSettings()
        {
            redTeamCounter = (int)ServerSettings.redTeamPlayerCount;
            blueTeamCounter = (int)ServerSettings.blueTeamPlayerCount;
        }
        public void UpdatePlayerCards(Server serv)
        {
            for (int i = 0; i < playerCards.Count; i++)
            {
                if (playerCards[i].isActiveAndEnabled)
                {
                    playerCards[i].UpdateInfo(serv.playerInfo[playerCards[i].player.clientID]);
                }
            }
        }

        public void JoinTeam(Server serv,uint clientID, uint teamNum)
        {
            serv.playerInfo[clientID].team = (Team)teamNum;

            if(teamNum == 1)
            {
                ServerSettings.redTeamPlayerCount++;
                teamRed.Add(serv.playerInfo[clientID]);
            }
            if (teamNum == 2)
            {
                ServerSettings.blueTeamPlayerCount++;
                teamBlue.Add(serv.playerInfo[clientID]);

            }

            serv.playerInfo[clientID].clientstate = ClientState.SPECTATING;

            playerCards[(int)serv.playerInfo[clientID].cardNum].UpdateInfo(serv.playerInfo[clientID]);
            UpdateServerSettings();

        }
        public void SetReady(Server serv,uint networkID, int state)
        {
            serv.playerInfo[networkID].playerState = (PlayerState)state;

            UpdatePlayerCard(serv, networkID);

        }

    }
}

