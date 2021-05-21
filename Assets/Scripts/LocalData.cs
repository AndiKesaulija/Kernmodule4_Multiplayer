using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LocalData : MonoBehaviour
{
    public NetworkBehavior netBehavior;
    public string user;
    public string password;

    public InputField userField;
    public InputField passwordField;

    private void OnEnable()
    {
        if (PlayerPrefs.GetString("user") != null)
        {
            user = PlayerPrefs.GetString("user");
            password = PlayerPrefs.GetString("password");

            userField.text = user;
            passwordField.text = password;


        }
    }

    public void SetData()
    {
        PlayerPrefs.SetString("user", netBehavior.email);
        PlayerPrefs.SetString("password", netBehavior.password);
    }


}
