using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Main : MonoBehaviour
{

    public NetworkBehavior networkBehavior;
    
    public void HostGame()
    {
        SceneManager.LoadScene(1);
    }
    public void JoinGame()
    {
        SceneManager.LoadScene(2);
    }
    public void LocalGame()
    {
        SceneManager.LoadScene(3);
    }
    public void GoToRegisterUser()
    {
        SceneManager.LoadScene(4);
    }
    public void RegisterUser()
    {
        networkBehavior.RegisterUser();
    }
    public void LoginClient()
    {
        networkBehavior.LoginUser();
        //SceneManager.LoadScene(5); 
    }

}
