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

    // 메시지 구독
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

    // 구독한 메시지 처리
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
        Debug.Log("HandleUserInfoMessage 호출: " + message);

        if (!message.StartsWith("USER_INFO|"))
        {
            Debug.LogError("USER_INFO 메시지 포맷 오류");
            return;
        }

        string data = message.Substring("USER_INFO|".Length).Trim();

        string[] parts = data.Split(',');

        if (parts.Length < 3)
        {
            Debug.LogError("USER_INFO 데이터 파싱 실패: " + data);
            return;
        }

        string nickname = parts[0].Trim();
        int level;
        float exp;

        if (!int.TryParse(parts[1].Trim(), out level))
        {
            Debug.LogError("레벨 파싱 실패: " + parts[1]);
            level = 1; // 기본값
        }

        if (!float.TryParse(parts[2].Trim(), out exp))
        {
            Debug.LogError("경험치 파싱 실패: " + parts[2]);
            exp = 0f;
        }

        Debug.Log($"유저 정보 - 닉네임: {nickname}, 레벨: {level}, 경험치: {exp}");

        userInfo.SetUserInfoUI(nickname, level, exp);
    }

    public void HandleEmoticonMessage(string message)
    {
        if (!message.StartsWith("EMO_LIST|")) return;

        string[] parts = message.Substring("EMO_LIST|".Length).Split(',');
        if (parts.Length != 4)
        {
            Debug.LogWarning("[EmoticonSystem] 이모티콘 개수 오류");
            return;
        }

        for (int i = 0; i < 4; i++)
        {
            if (int.TryParse(parts[i], out int emoIndex))
            {
                if (emoIndex >= 0 && emoIndex < emoticon.emoticonPrefabs.Length)
                {
                    emoticon.emoticonButtons[i].image.sprite = emoticon.emoticonPrefabs[emoIndex];

                    // 버튼 이름을 emoIndex로 설정
                    emoticon.emoticonButtons[i].name = emoIndex.ToString();
                }
                else
                {
                    Debug.LogWarning($"[EmoticonSystem] 잘못된 이모티콘 인덱스: {emoIndex}");
                }
            }
            else
            {
                Debug.LogWarning($"[EmoticonSystem] 이모티콘 파싱 실패: {parts[i]}");
            }
        }
    }
}
