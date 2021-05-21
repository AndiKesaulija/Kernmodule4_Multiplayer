using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class RegisterUser : MonoBehaviour
{

    public InputField userField;
    public InputField passwordField;
    public InputField emailField;

    private void OnEnable()
    {
        //Debug.Log("UserName: " + UserData.name + " Email: " + UserData.email + " Password: " + UserData.password);

        userField.text = UserData.name;
        emailField.text = UserData.email;
        passwordField.text = UserData.password;
    }
    
}
