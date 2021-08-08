using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChatClientExample
{
    public class Server_UI : MonoBehaviour
    {

        //public List<Player_Card> playerCards;

        //public List<PlayerInfo> teamRed = new List<PlayerInfo>(3);
        //public List<PlayerInfo> teamBlue = new List<PlayerInfo>(3);

        //public int redTeamCounter = (int)ServerSettings.redTeamPlayerCount;
        //public int blueTeamCounter = (int)ServerSettings.blueTeamPlayerCount;

        public Server serv;
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



            playercards.Add(info.clientID, newPlayerCard);

            RectTransform cardTransform = newPlayerCard.GetComponent<RectTransform>();
            cardTransform.anchoredPosition = new Vector2(0, template.localPosition.y + (-tempplateHeight * playercards.Count - 1));

            RepaintCards();
        }
        public void RepaintCards()
        {
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

            //Remove from server
            serv.playerInfo.Remove(networkID);

        }
        

        public void SetTeamSize(Dropdown count)
        {
            ServerSettings.maxTeamPlayerCount = (uint)count.value + 1;
            Debug.Log(ServerSettings.maxTeamPlayerCount);

            ServerInfoMessage msg = new ServerInfoMessage();

            //ResetGame
            serv.gameManager.ClearGame();

            //Update Clients
            serv.SendBroadcast(msg);


        }

    }
}

