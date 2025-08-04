using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// �α��� �� ȸ�� ���� ���� ������ �޽��� ���� Ŭ����
public class AuthReceiver : IMessageHandler
{
    private readonly LoginUI loginUI;
    private readonly RegisterUI registerUI;

    private readonly string[] commands = {
        "LOGIN_SUCCESS", "WRONG_PASSWORD", "ID_NOT_FOUND",
        "REGISTER_SUCCESS", "EMPTY_PASSWORD", "DUPLICATE_ID",
        "DUPLICATE_NICK", "REGISTER_ERROR", "FILE_WRITE_ERROR"
    };

    public AuthReceiver(LoginUI loginUI, RegisterUI registerUI)
    {
        this.loginUI = loginUI;
        this.registerUI = registerUI;

        foreach (string command in commands)
        {
            NetworkConnector.Instance.RegisterHandler(command, this);
        }
    }
    public void Dispose()
    {
        foreach (string command in commands)
        {
            NetworkConnector.Instance.RemoveHandler(command, this);
        }
    }

    public void HandleMessage(string message)
    {
        if (message.StartsWith("LOGIN_SUCCESS"))
        {
            string[] parts = message.Split('|');
            if (parts.Length == 2)
            {
                string[] userParts = parts[1].Split(',');

                if (userParts.Length == 3)
                {
                    string userId = userParts[0];
                    string userPw = userParts[1];
                    string userNick = userParts[2];

                    PlayerPrefs.SetString("nickname", userNick);
                    PlayerPrefs.Save();
                    NetworkConnector.Instance.UserNickname = userNick;
                    if (!NetworkConnector.Instance.CurrentUserList.Contains(userNick))
                    {
                        NetworkConnector.Instance.CurrentUserList.Add(userNick);
                        Debug.Log($"[�α���] ���� �߰���: {userNick}");
                    }
                    SceneManager.LoadScene("LobbyScene");
                }
                else
                {
                    Debug.LogError("���� ���� �Ľ� ����");
                    if (loginUI != null)
                        loginUI.loginErrorText.text = "���� ���� �Ľ� ����";
                }
            }
            else
            {
                Debug.LogError("���� ���� ����");
                if (loginUI != null)
                    loginUI.loginErrorText.text = "���� ���� ����";
            }
        }
        else if (message == "WRONG_PASSWORD")
        {
            Debug.Log("��й�ȣ ����");
            if (loginUI != null)
                loginUI.loginErrorText.text = "�߸��� ��й�ȣ";
        }
        else if (message == "ID_NOT_FOUND")
        {
            Debug.Log("������ ����");
            if (loginUI != null)
                loginUI.loginErrorText.text = "�������� �ʴ� �����";
        }

        if (message == "REGISTER_SUCCESS")
        {
            Debug.Log("ȸ������ ����");
            if (registerUI != null)
                registerUI.registerErrorText.text = "ȸ������ ����";
        }
        else if (message == "EMPTY_PASSWORD")
        {
            if (registerUI != null)
                registerUI.registerErrorText.text = "�� ��й�ȣ";
        }
        else if (message == "DUPLICATE_ID")
        {
            if (registerUI != null)
                registerUI.registerErrorText.text = "ID �ߺ�";
        }
        else if (message == "DUPLICATE_NICK")
        {
            if (registerUI != null)
                registerUI.registerErrorText.text = "�г��� �ߺ�";
        }
        else if (message == "REGISTER_ERROR")
        {
            if (registerUI != null)
                registerUI.registerErrorText.text = "�� �� ���� ����";
        }
        else if (message == "FILE_WRITE_ERROR")
        {
            if (registerUI != null)
                registerUI.registerErrorText.text = "���� ����";
        }
    }
    
}
