using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HighScoreTable : MonoBehaviour
{

    public Transform container;
    public Transform template;

    public NetworkBehavior network;

    private void Awake()
    {

        network.StartCoroutine(network.GetPlayerScoreRequest(1, UserData.id));

        template.gameObject.SetActive(false);

    }
    public void GetUserScores()
    {
        template.gameObject.SetActive(true);

        float tempplateHeight = 16f;
        for (int i = 0; i < network.userScores.highScore.Length; i++)
        {
            if (network.userScores.highScore[i] != null)
            {
                Transform scoreEntry = Instantiate(template, container);
                RectTransform scoreRectTransform = scoreEntry.GetComponent<RectTransform>();

                scoreRectTransform.anchoredPosition = new Vector2(0, template.localPosition.y + (-tempplateHeight * i));

                //Set scoreEntry data
                scoreEntry.Find("TextLadderPos").GetComponent<Text>().text = (i + 1).ToString();
                scoreEntry.Find("TextRoundWins").GetComponent<Text>().text = "0";
                scoreEntry.Find("TextScore").GetComponent<Text>().text = network.userScores.highScore[i].score.ToString();

            }

        }
       

        template.gameObject.SetActive(false);
    }

    public void SetUserScore()
    {

    }

}
