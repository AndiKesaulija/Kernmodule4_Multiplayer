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

        public int redTeamCounter = (int)ServerSettings.redTeamPlayerCount;
        public int blueTeamCounter = (int)ServerSettings.blueTeamPlayerCount;
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

        }
        public void UpdateServerSettings()
        {
            redTeamCounter = (int)ServerSettings.redTeamPlayerCount;
            blueTeamCounter = (int)ServerSettings.blueTeamPlayerCount;
        }
        
        public void ToggleWindow(Image window)
        {
            window.gameObject.SetActive(!window.gameObject.activeInHierarchy);
        }
    }
}

