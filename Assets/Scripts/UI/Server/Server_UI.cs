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

        //new PlayerCards
        public Transform container;
        public Transform template;

        //public List<Transform> playercards = new List<Transform>(0);
        public Dictionary<uint, Transform> playercards = new Dictionary<uint, Transform>();
        float tempplateHeight = 50f;


        public void Awake()
        {
            template.gameObject.SetActive(false);
        }
        public void AddPlayerCard(Server serv, PlayerInfo info)
        {
            Transform newPlayerCard = Instantiate(template, container);
            newPlayerCard.gameObject.SetActive(true);

            newPlayerCard.gameObject.GetComponent<Player_Card>().player = info;
            newPlayerCard.gameObject.GetComponent<Player_Card>().playerCardNumber = (uint)playercards.Count;

            serv.playerInfo.Add(info.networkID, info);


            playercards.Add(info.clientID, newPlayerCard);

            RectTransform cardTransform = newPlayerCard.GetComponent<RectTransform>();
            cardTransform.anchoredPosition = new Vector2(0, template.localPosition.y + (-tempplateHeight * playercards.Count - 1));

            RepaintCards();
        }
        public void RepaintCards()
        {
            //for (uint i = 0; i < playercards.Count; i++)
            //{
            //    playercards[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(0, template.localPosition.y + (-tempplateHeight * i));
            //}

            uint cardnum = 0;//Card counter

            foreach (KeyValuePair<uint,Transform> card in playercards)
            {
                card.Value.GetComponent<Player_Card>().playerCardNumber = cardnum;
                card.Value.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, template.localPosition.y + (-tempplateHeight * cardnum));
                cardnum++;
            }

        }
        public void UpdateCard(PlayerInfo info)
        {
            //Update playercard met KEY cardID met info
            playercards[info.clientID].GetComponent<Player_Card>().UpdateInfo(info);

        }

        public void DisconnectPlayer(Server serv,uint networkID)
        {
            //Remove PlayerCard
            Destroy(playercards[serv.playerInfo[networkID].clientID].gameObject);

            playercards.Remove(serv.playerInfo[networkID].clientID);

            RepaintCards();

            //Remove from Team
            if (teamBlue.Contains(serv.playerInfo[networkID]))
            {
                teamBlue.Remove(serv.playerInfo[networkID]);
            }
            else if(teamRed.Contains(serv.playerInfo[networkID]))
            {
                teamRed.Remove(serv.playerInfo[networkID]);
            }

            //Remove from server
            serv.playerInfo.Remove(networkID);

        }

        public void UpdateServerSettings()
        {
            redTeamCounter = (int)ServerSettings.redTeamPlayerCount;
            blueTeamCounter = (int)ServerSettings.blueTeamPlayerCount;
        }
        public void JoinTeam(Server serv,uint clientID, uint teamNum)
        {
            serv.playerInfo[clientID].team = (Team)teamNum;

            if(teamNum == 1)
            {
                serv.playerInfo[clientID].teamPos = ServerSettings.redTeamPlayerCount;

                ServerSettings.redTeamPlayerCount++;
                teamRed.Add(serv.playerInfo[clientID]);
            }
            if (teamNum == 2)
            {
                serv.playerInfo[clientID].teamPos = ServerSettings.blueTeamPlayerCount;

                ServerSettings.blueTeamPlayerCount++;
                teamBlue.Add(serv.playerInfo[clientID]);

            }

            serv.playerInfo[clientID].clientstate = ClientState.IN_GAME;

            UpdateCard(serv.playerInfo[clientID]);
            //playerCards[(int)serv.playerInfo[clientID].cardNum].UpdateInfo(serv.playerInfo[clientID]);

            UpdateServerSettings();

        }
        public void SetPlayerState(Server serv,uint networkID, int state)
        {
            serv.playerInfo[networkID].playerState = (PlayerState)state;

            UpdateCard(serv.playerInfo[networkID]);

        }

        public void SetTeamSize(Dropdown count)
        {
            ServerSettings.maxTeamPlayerCount = (uint)count.value + 1;
            Debug.Log(ServerSettings.maxTeamPlayerCount);
        }

    }
}

