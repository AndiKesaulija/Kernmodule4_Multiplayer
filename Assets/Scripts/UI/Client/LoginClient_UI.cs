using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoginClient_UI : MonoBehaviour
{
    // Start is called before the first frame update
    public string ipAddress;
    public ushort port;

    public InputField ipAddressField;
    public InputField portField;

    public void OnEnable()
    {
        ipAddress = UserData.ipAddress;
        port = UserData.port;

        ipAddressField.text = ipAddress;
        portField.text = port.ToString();

    }

    public void SetIpAddress(string ip)
    {
        ipAddress = ip;
    }

    public void SetPort(string port)
    {
        ushort result;

        if(ushort.TryParse(port, out result) == true)
        {
            this.port = result;
        }
        else
        {
            //ERROR: Invalid port
        }
    }
    
}
