using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Main : MonoBehaviour
{

    public NetworkBehavior networkBehavior;
    public void Awake()
    {
        Screen.SetResolution(1920,1080, false);
    }
    public void HostGame()
    {
        SceneManager.LoadScene("Server");
    }
    public void JoinGame()
    {
        SceneManager.LoadScene("Client");
    }
    public void GoToRegisterUser()
    {
        SceneManager.LoadScene("Register");
    }
    public void RegisterUser()
    {
        networkBehavior.RegisterUser();
    }
    public void LoginClient()
    {
        networkBehavior.LoginUser();
    }

}
