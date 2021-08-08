using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChatClientExample
{
    public class Client_UI : MonoBehaviour
    {

        public Image window_TeamSelection;
        public Button joinRed;
        public Button joinBlue;

        public NetworkPlayer player;
        public Image power;

        public Text powerText;
        public Text cooldownText;

        public Image setReadyText;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if(ServerSettings.redTeamPlayerCount >= ServerSettings.maxTeamPlayerCount)
            {
                joinRed.interactable = false;
            }
            else
            {
                joinRed.interactable = true;

            }
            if (ServerSettings.blueTeamPlayerCount >= ServerSettings.maxTeamPlayerCount)
            {
                joinBlue.interactable = false;
            }
            else
            {
                joinBlue.interactable = true;
            }
            if (player != null)
            {
                if (player.isLocal == true)
                {
                    power.GetComponent<RectTransform>().localScale = new Vector3((float)player.power / player.maxPower, 1, 1);
                }

                powerText.text = player.power.ToString();
                cooldownText.text = player.cooldown.ToString();
            }
           

        }
        
        public void ToggleWindow(Image window)
        {
            window.gameObject.SetActive(!window.gameObject.activeInHierarchy);
        }
        public void ToggleWindow(Image window, bool toggle)
        {
            window.gameObject.SetActive(toggle);
        }
    }
}

