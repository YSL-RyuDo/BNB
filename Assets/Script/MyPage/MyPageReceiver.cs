using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MyPageReceiver : MonoBehaviour, IMessageHandler
{
    [SerializeField] private MyPageUserInfo userInfo;

    // 메시지 구독
    private readonly string[] commands =
    {
        "SETINFO", "WINRATE", "GETMYEMO", "GETMYBALLOON", "GETMYICON", "CHAR_WINLOSS"
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
            case "SETINFO": HandleSetInfoMessage(message); break;
            case "WINRATE": HandleWinRateMessage(message); break;
            case "GETMYEMO": HandleGetMyEmoMessage(message); break;
            case "GETMYICON": HandleGetMyIconMessage(message); break;
            case "CHAR_WINLOSS":HandleCharWinLossMessage(message); break;
        }
    }

    public void HandleSetInfoMessage(string message)
    {
        if (string.IsNullOrEmpty(message) || !message.StartsWith("SETINFO|"))
            return;

        // SETINFO|닉,레벨,경험치,아이콘,emo0,emo1,emo2,emo3,balloon
        string data = message.Substring("SETINFO|".Length).Trim();
        string[] p = data.Split(',');

        if (p.Length < 9)
        {
            Debug.LogError($"[SETINFO] 필드 수 부족: {data}");
            return;
        }

        string nickname = p[0].Trim();

        int level = TryInt(p[1], 1);
        float exp = TryFloat(p[2], 0f);

        int[] equippedEmos = new int[4];
        equippedEmos[0] = TryInt(p[4], -1);
        equippedEmos[1] = TryInt(p[5], -1);
        equippedEmos[2] = TryInt(p[6], -1);
        equippedEmos[3] = TryInt(p[7], -1);

        int balloonIndex = TryInt(p[8], -1);

        int equippedIcon = TryInt(p[3], -1);

        userInfo.SetUserInfoUI(nickname, level, exp, equippedEmos, balloonIndex, equippedIcon);
    }

    private void HandleWinRateMessage(string message)
    {
        if (!message.StartsWith("WINRATE|")) return;

        string data = message.Substring("WINRATE|".Length).Trim();
        string[] p = data.Split(',');

        if (p.Length < 3)
        {
            Debug.LogError($"[WINRATE] 필수 항목 부족: {data}");
            return;
        }

        string nickname = p[0].Trim();
        int totalWin = TryInt(p[1], 0);
        int totalLose = TryInt(p[2], 0);

        var top3Triples = new List<int[]>(3);
        for (int i = 0; i < 3; i++)
        {
            int baseIdx = 3 + i * 3;
            if (p.Length <= baseIdx + 2) break;

            int idx = TryInt(p[baseIdx], -1);
            int win = TryInt(p[baseIdx + 1], 0);
            int lose = TryInt(p[baseIdx + 2], 0);

            if (idx >= 0)
                top3Triples.Add(new int[] { idx, win, lose });
        }

        userInfo.SetWinRateUI(nickname, totalWin, totalLose, top3Triples);
    }


    private void HandleCharWinLossMessage(string message)
    {
        if (!message.StartsWith("CHAR_WINLOSS|")) return;

        string data = message.Substring("CHAR_WINLOSS|".Length).Trim();
        string[] p = data.Split(',');

        if (p.Length < 1 + 7 * 2)
        {
            Debug.LogError($"[CHAR_WINLOSS] 데이터 부족: {data}");
            return;
        }

        string nickname = p[0].Trim();

        int idx = 1;
        for (int charIndex = 0; charIndex < 7; charIndex++)
        {
            int win = TryInt(p[idx++], 0);
            int lose = TryInt(p[idx++], 0);

            userInfo.BuildFullCharacterRateItem(charIndex, win, lose);
        }
    }

    private void HandleGetMyEmoMessage(string message)
    {
        if (!message.StartsWith("GETMYEMO|")) return;

        string data = message.Substring("GETMYEMO|".Length).Trim();
        if (string.IsNullOrEmpty(data)) return;

        var tokens = data.Split(',');
        if (tokens.Length == 0) return;

        string nickname = tokens[0].Trim();

        var owned = new List<int>();
        for (int i = 1; i < tokens.Length; i++)
        {
            if (int.TryParse(tokens[i].Trim(), out var idx))
                owned.Add(idx);
        }

        userInfo.BuildOwnedEmoticonButtons(owned);
    }

    private void HandleGetMyIconMessage(string message)
    {
        if (!message.StartsWith("GETMYICON|")) return;

        string data = message.Substring("GETMYICON|".Length).Trim();
        if (string.IsNullOrEmpty(data)) return;

        var tokens = data.Split(',');
        if (tokens.Length <= 1) return;

        var ownedIcons = new List<int>();
        for (int i = 1; i < tokens.Length; i++)
        {
            if (int.TryParse(tokens[i].Trim(), out var idx))
                ownedIcons.Add(idx);
        }

        userInfo.BuildOwnedIconButtons(ownedIcons);
    }
    private static int TryInt(string s, int def)
       => int.TryParse(s.Trim(), out var v) ? v : def;
    private static float TryFloat(string s, float def)
       => float.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : def;
}

