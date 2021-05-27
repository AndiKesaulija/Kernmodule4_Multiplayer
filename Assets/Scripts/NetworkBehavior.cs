using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;


public class NetworkBehavior : MonoBehaviour
{
    public string user;
    public string email;
    public string password;

    UserInfo userInfo = new UserInfo();

    void Start()
    {
        StartCoroutine(GetRequest("https://studenthome.hku.nl/~Andi.Kesaulija/server_login.php?id=1&pass=12345"));
    }

    public void LoginUser()
    {
        StartCoroutine(GetLoginRequest("https://studenthome.hku.nl/~Andi.Kesaulija/user_login.php?" +
            "email=" + email +
            "&pass=" + password
            ));
    }
    public void RegisterUser()
    {
        StartCoroutine(Register());
    }
    public void UpdateUser()
    {
        StartCoroutine(UpdateData());
    }
    IEnumerator UpdateData()
    {
        yield return StartCoroutine(GetRequest("https://studenthome.hku.nl/~Andi.Kesaulija/update_user.php?" +
           "id=" + UserData.id +
           "&user=" + user +
           "&email=" + email +
           "&pass=" + password
           ));
        //SceneManager.LoadScene(0);
    }
    IEnumerator Register()
    {
        yield return StartCoroutine(GetRequest("https://studenthome.hku.nl/~Andi.Kesaulija/update_user.php?" +
           "user=" + user +
           "&email=" + email +
           "&pass=" + password
           ));
        SceneManager.LoadScene(0);
    }
    IEnumerator Login()
    {
        yield return StartCoroutine(GetLoginRequest("https://studenthome.hku.nl/~Andi.Kesaulija/user_login.php?" +
            "email=" + email +
            "&pass=" + password
            ));
        //SceneManager.LoadScene(5);

    }

    IEnumerator GetRequest(string url)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();
            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogError(": Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogError(": HTTP Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.Success:
                    Debug.Log(webRequest.downloadHandler.text);
                    break;
            }
        }
    }
    IEnumerator GetLoginRequest(string url)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();
         
            if(webRequest.error != null)
            {
                Debug.Log(webRequest.error);
                Debug.Log("UNITY: Error");
            }
            else
            {
                string json = webRequest.downloadHandler.text;

                //check Json Data if correct
                SetStaticData(json);
               
            }
            
        }
    }

    private void SetStaticData(string text)
    {
        //ReadJSON
        try
        {
            userInfo = JsonUtility.FromJson<UserInfo>(text);

            UserData.id = userInfo.id;
            UserData.name = userInfo.name;
            UserData.email = userInfo.email;
            UserData.password = userInfo.password;
            Debug.Log("UserName: " + UserData.name + " Email: " + UserData.email + " Password: " + UserData.password);

            SceneManager.LoadScene(5);

        }
        catch
        {
            Debug.Log("Invalid Data");
        }
       


    }
    public void ReadUserInputField(string input)
    {
        user = input;
    }
    public void ReadEmailInputField(string input)
    {
        email = input;
    }
    public void ReadPassInputField(string input)
    {
        password = input;
    }


}
