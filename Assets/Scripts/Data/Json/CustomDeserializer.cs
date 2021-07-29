using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public static class CustomDeserialisor
{
    public static void Deserialize<T>(string json)
    {
        Debug.Log(json);
    }
    public static void DeserializeScoreData(string json)
    {

        char[] charsToTrim = { '[', ']'};
        string trim = json.Trim(charsToTrim);

        Debug.Log(trim);

        string[] scoreData = trim.Split('}');

        
        for (int i = 0; i < scoreData.Length; i++)
        {
            scoreData[i] = scoreData[i] + "}";


            if (scoreData[i].Substring(0,1) == ",")
            {
                scoreData[i] = scoreData[i].Remove(0,1);
            }
        }


      
    }
}
