using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MyPageReceiver : MonoBehaviour, IMessageHandler
{
    [SerializeField] private MyPageUserInfo userInfo;
    [SerializeField] private MyPageEmoticon emoticon;

    // �޽��� ����
    private readonly string[] commands =
    {
        "USER_INFO", "EMO_LIST"
    };

    private void OnEnable()
    {
        foreach (string command in commands)
        {
            NetworkConnector.Instance.MyPageHandler(command, this);
        }
    }

    private void OnDisable()
    {
        foreach (string command in commands)
        {
            NetworkConnector.Instance.RemoveMyPageHandler(command, this);
        }
    }

    // ������ �޽��� ó��
    public void HandleMessage(string message)
    {

        string[] parts = message.Split('|');
        string command = message.Split('|')[0];

        switch (command)
        {
            case "USER_INFO": HandleUserInfoMessage(message); break;
            case "EMO_LIST": HandleEmoticonMessage(message); break; 
        }
    }

    public void HandleUserInfoMessage(string message)
    {
        Debug.Log("HandleUserInfoMessage ȣ��: " + message);

        if (!message.StartsWith("USER_INFO|"))
        {
            Debug.LogError("USER_INFO �޽��� ���� ����");
            return;
        }

        string data = message.Substring("USER_INFO|".Length).Trim();

        string[] parts = data.Split(',');

        if (parts.Length < 3)
        {
            Debug.LogError("USER_INFO ������ �Ľ� ����: " + data);
            return;
        }

        string nickname = parts[0].Trim();
        int level;
        float exp;

        if (!int.TryParse(parts[1].Trim(), out level))
        {
            Debug.LogError("���� �Ľ� ����: " + parts[1]);
            level = 1; // �⺻��
        }

        if (!float.TryParse(parts[2].Trim(), out exp))
        {
            Debug.LogError("����ġ �Ľ� ����: " + parts[2]);
            exp = 0f;
        }

        Debug.Log($"���� ���� - �г���: {nickname}, ����: {level}, ����ġ: {exp}");

        userInfo.SetUserInfoUI(nickname, level, exp);
    }

    public void HandleEmoticonMessage(string message)
    {
        if (!message.StartsWith("EMO_LIST|")) return;

        string[] parts = message.Substring("EMO_LIST|".Length).Split(',');
        if (parts.Length != 4)
        {
            Debug.LogWarning("[EmoticonSystem] �̸�Ƽ�� ���� ����");
            return;
        }

        for (int i = 0; i < 4; i++)
        {
            if (int.TryParse(parts[i], out int emoIndex))
            {
                if (emoIndex >= 0 && emoIndex < emoticon.emoticonPrefabs.Length)
                {
                    emoticon.emoticonButtons[i].image.sprite = emoticon.emoticonPrefabs[emoIndex];

                    // ��ư �̸��� emoIndex�� ����
                    emoticon.emoticonButtons[i].name = emoIndex.ToString();
                }
                else
                {
                    Debug.LogWarning($"[EmoticonSystem] �߸��� �̸�Ƽ�� �ε���: {emoIndex}");
                }
            }
            else
            {
                Debug.LogWarning($"[EmoticonSystem] �̸�Ƽ�� �Ľ� ����: {parts[i]}");
            }
        }
    }
}
