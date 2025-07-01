using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using UnityEngine.SceneManagement;

public class LoginUI : MonoBehaviour
{
    public TMP_InputField idInputField;
    public TMP_InputField passwordInputField;

    public Button loginButton;

    void Start()
    {
        loginButton.onClick.AddListener(OnLoginClicked);
    }

    private async void OnLoginClicked()
    {
        string id = idInputField.text.Trim();
        string password = passwordInputField.text.Trim();

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(password))
        {
            return;
        }

        string packet = $"LOGIN|{id},{password}\n";
        byte[] bytes = Encoding.UTF8.GetBytes(packet);
        await NetworkConnector.Instance.Stream.WriteAsync(bytes, 0, bytes.Length);
        Debug.Log("로그인 패킷 전송: " + packet);
    }

    public void OnLoginResponse(string msg)
    {
        if (msg.StartsWith("LOGIN_SUCCESS|"))
        {
            string data = msg.Substring("LOGIN_SUCCESS|".Length);
            string[] tokens = data.Split(',');

            string id = tokens[0];
            string pw = tokens[1];
            string nickname = tokens[2];

            SceneManager.LoadScene("JobSelectScene");


        }
        else if (msg == "WRONG_PASSWORD")
        {
            
        }
        else if (msg == "ID_NOT_FOUND")
        {

        }
    }
}
