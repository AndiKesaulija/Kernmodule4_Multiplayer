using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChatClientExample
{
    public class Server_UI : MonoBehaviour
    {

        public List<Player_Display> playerCards;
        public Dictionary<uint, PlayerInfo> playerInfo = new Dictionary<uint, PlayerInfo>();

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void AddPlayerCard(PlayerInfo info)
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

        public void UpdatePlayerCard(uint networkID)
        {
            playerCards[(int)playerInfo[networkID].cardNum].UpdateInfo(playerInfo[networkID]);
        }

    }
}

