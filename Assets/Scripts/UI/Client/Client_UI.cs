using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChatClientExample
{
    public class Client_UI : MonoBehaviour
    {

        public Image window_TeamSelection;
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
        public void SelectTeamRed()
        {

            CloseWindow(window_TeamSelection);
        }
        public void CloseWindow(Image window)
        {
            window.gameObject.SetActive(false);
        }
    }
}

