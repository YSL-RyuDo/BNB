using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyReceiver : MonoBehaviour, IMessageHandler
{
    [SerializeField] private LobbyUserInfo userInfo;
    [SerializeField] private LobbyUserList userList;
    [SerializeField] private LobbyChat chat;
    [SerializeField] private LobbyRoom lobbyRoom;
    [SerializeField] private LobbyUserPage userPage;

    // 메시지 구독
    private readonly string[] commands =
    {
        "ROOM_LIST", "ROOM_CREATED", "CREATE_ROOM_SUCCESS",
        "ENTER_ROOM_SUCCESS", "LOBBY_CHAT", "LOBBY_USER_LIST", "USER_INFO",
        "SETINFO", "WINRATE", "GETMYEMO", "GETMYBALLOON"
    };

    private void OnEnable()
    {
        foreach (string command in commands)
        {
            NetworkConnector.Instance.LobbyHandler(command, this);
        }
    }

    private void OnDisable()
    {
        foreach (string command in commands)
        {
            NetworkConnector.Instance.RemoveLobbyHandler(command, this);
        }
    }

    // 구독한 메시지 처리
    public void HandleMessage(string message)
    {

        string[] parts = message.Split('|');
        string command = message.Split('|')[0];

        switch (command)
        {
            case "ROOM_LIST": HandleRoomListMessage(message); break;
            case "ROOM_CREATED": HandleRoomCreated(message); break;
            case "CREATE_ROOM_SUCCESS": HandleCreateRoomSuccess(message); break;
            case "ENTER_ROOM_SUCCESS": HandleEnterRoomSuccess(message); break;
            case "LOBBY_CHAT": HandleLobbyChatMessage(message); break;
            case "LOBBY_USER_LIST": HandleUserListMessage(message); break;
            case "USER_INFO": HandleUserInfoMessage(message); break;
            case "SETINFO": HandleSetInfoMessage(message); break;
            case "WINRATE": HandleWinRateMessage(message); break;
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

    public void HandleUserListMessage(string message)
    {
        if (userList == null || userList.userListContent == null)
        {
            Debug.LogWarning("HandleUserListMessage: userList 또는 userListContent가 null입니다. 메시지 무시함.");
            return;
        }


        string[] parts = message.Split("|");
        if (parts.Length < 2)
        {
            Debug.LogWarning("잘못된 LOBBY_USER_LIST 메세지 형식");
            return;
        }

        foreach (Transform child in userList.userListContent)
        {
            Destroy(child.gameObject);
        }

        for (int i = 1; i < parts.Length; i++)
        {
            string[] userInfo = parts[i].Split(',');
            if (userInfo.Length != 2)
            {
                Debug.LogWarning($"유저 정보 파싱 실패: {parts[i]}");
                continue;
            }

            string nickname = userInfo[0];
            int level = 1;
            if (!int.TryParse(userInfo[1], out level))
            {
                Debug.LogWarning($"레벨 파싱 실패:{userInfo[1]}");
                level = 1;
            }

            userList.UpdateUserList(nickname, level);  
        }
    }

    public void HandleRoomListMessage(string message)
    {
        Debug.Log("HandleRoomListMessage 호출: " + message);

        Debug.Log($"roomButtons.Length = {lobbyRoom.roomButtons?.Length ?? -1}");
        Debug.Log($"isOccupied.Length = {lobbyRoom.isOccupied?.Length ?? -1}");

        for (int i = 0; i < lobbyRoom.roomButtons.Length; i++)
        {
            Debug.Log($"roomButtons[{i}] is {(lobbyRoom.roomButtons[i] == null ? "null" : "not null")}");
        }

        if (lobbyRoom.isOccupied == null)
        {
            Debug.LogError("isOccupied 배열이 null입니다!");
        }
        else
        {
            for (int i = 0; i < lobbyRoom.isOccupied.Length; i++)
            {
                Debug.Log($"isOccupied[{i}] = {lobbyRoom.isOccupied[i]}");
            }
        }

        if (!message.StartsWith("ROOM_LIST|"))
        {
            Debug.LogError("ROOM_LIST 메시지 포맷 오류");
            return;
        }

        string data = message.Substring("ROOM_LIST|".Length);
        string[] rooms = data.Split('|', StringSplitOptions.RemoveEmptyEntries);

        Debug.Log($"서버로부터 받은 방 개수: {rooms.Length}");

        // 1) UI 슬롯 모두 초기화
        for (int i = 0; i < lobbyRoom.roomButtons.Length; i++)
        {
            lobbyRoom.ResetRoomButton(lobbyRoom.roomButtons[i]);
            lobbyRoom.isOccupied[i] = false;
        }

        // 2) 받은 방 리스트를 0번 슬롯부터 순서대로 UI에 세팅
        for (int i = 0; i < rooms.Length && i < lobbyRoom.roomButtons.Length; i++)
        {
            string[] f = rooms[i].Split(new[] { ',' }, 5, StringSplitOptions.None);
            if (f.Length < 3)
            {
                Debug.LogWarning($"방 정보 포맷 오류: {rooms[i]}");
                continue;
            }

            string roomName = f[0].Trim();
            string mapName = f[1].Trim();
            bool hasPassword = f[2].Trim() == "1";

            bool isCoop = false;
            string userListStr = string.Empty;

            if (f.Length >= 4)
                isCoop = f[3].Trim() == "1";

            lobbyRoom.roomPasswordMap[roomName] = hasPassword;
            lobbyRoom.roomCoopMap[roomName] = isCoop;

            lobbyRoom.SetRoomButton(lobbyRoom.roomButtons[i], roomName, lobbyRoom.GetSpriteForMap(mapName));
            lobbyRoom.isOccupied[i] = true;

            Debug.Log($"roomButtons[{i}]에 방 '{roomName}' 세팅 완료");
        }

    }

    public void HandleLobbyChatMessage(string message)
    {
        Debug.Log("HandleLobbyChatMessage 호출: " + message);

        string[] parts = message.Split('|');
        if (parts.Length >= 2)
        {
            string[] chatParts = parts[1].Split(':');
            if (chatParts.Length >= 2)
            {
                string userNickname = chatParts[0];
                string chatMessage = string.Join(":", chatParts, 1, chatParts.Length - 1);

                chat.AddChatMessage(userNickname, chatMessage);
            }
            else
            {
                Debug.LogWarning("LOBBY_CHAT 닉네임/메시지 파싱 오류: " + parts[1]);
            }
        }
        else
        {
            Debug.LogWarning("LOBBY_CHAT 메시지 포맷 오류: " + message);
        }
    }

    public void HandleCreateRoomSuccess(string message)
    {
        string[] parts = message.Split('|');
        if (parts.Length < 3) return;

        string roomName = parts[1].Trim();
        string mapName = parts[2].Trim();
        bool isCreator = parts.Length > 3 && parts[3] == "CREATOR";

        // 유저 리스트 파싱
        string userListStr = "";
        if (isCreator)
        {
            if (parts.Length > 4)
                userListStr = parts[4].Trim();
        }
        else
        {
            if (parts.Length > 3)
                userListStr = parts[3].Trim();
        }

        // [추가] 비밀번호 여부 파싱
        bool hasPassword = false;
        if (parts.Length > 5 && parts[5].Trim() == "HAS_PASSWORD")
        {
            hasPassword = true;
        }
        lobbyRoom.roomPasswordMap[roomName] = hasPassword;

        bool isCoop = false;
        if(parts.Length > 6 && parts[6].Trim() == "1")
        {
            isCoop = true;
        }
        lobbyRoom.roomCoopMap[roomName] = isCoop;

        List<string> userList = new List<string>();
        Dictionary<string, int> characterIndexMap = new Dictionary<string, int>();

        if (!string.IsNullOrEmpty(userListStr))
        {
            var userEntries = userListStr.Split(',');
            foreach (var entry in userEntries)
            {
                var up = entry.Split(':');

                string nick = up[0].Trim();
                userList.Add(nick);

                int charIndex = 0;
                if (up.Length >= 2) int.TryParse(up[1].Trim(), out charIndex);
                characterIndexMap[nick] = charIndex;
            }
        }

        NetworkConnector.Instance.CurrentUserList = userList;

        //NetworkConnector.Instance.CurrentUserCharacterIndices = characterIndexMap;
        NetworkConnector.Instance.SetUserCharacterIndices(characterIndexMap);

        Sprite sprite = lobbyRoom.GetSpriteForMap(mapName);

        for (int i = 0; i < lobbyRoom.roomButtons.Length; i++)
        {
            if (!lobbyRoom.isOccupied[i])
            {
                lobbyRoom.SetRoomButton(lobbyRoom.roomButtons[i], roomName, sprite);
                lobbyRoom.isOccupied[i] = true;
                break;
            }
        }

        Debug.Log($"Room '{roomName}' has users: {string.Join(", ", userList)}");

        string rawUserListStr = userListStr; // team 필드(:Blue/:Red)가 포함된 원본
        string hasPwStr = hasPassword ? "HAS_PASSWORD" : "NO_PASSWORD";
        string coopStr = isCoop ? "1" : "0";

        NetworkConnector.Instance.PendingRoomEnterMessage =
            $"ENTER_ROOM_SUCCESS|{roomName}|{mapName}|{rawUserListStr}|{hasPwStr}|{coopStr}";

        if (isCreator)
        {
            NetworkConnector.Instance.CurrentRoomName = roomName;
            NetworkConnector.Instance.SelectedMap = mapName;

            NetworkConnector.Instance.CurrentRoomLeader = NetworkConnector.Instance.UserNickname;

            SceneManager.LoadScene("RoomScene");
        }
    }

    public void HandleEnterRoomSuccess(string message)
    {
        string[] parts = message.Split('|');
        if (parts.Length < 4) return;

        string roomName = parts[1];
        string mapName = parts[2];
        string userListStr = parts[3];
        string coopFlag = (parts.Length >= 6) ? parts[5].Trim() : "0";

        List<string> nicknames = new();

        NetworkConnector.Instance.UserTeams.Clear();

        foreach (string token in userListStr.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var up = token.Split(':'); // nick:idx(:team)
            if (up.Length < 2) continue;

            string nickname = up[0].Trim();
            int charIndex = 0;
            int.TryParse(up[1], out charIndex);

            // 캐릭터 정보 갱신
            NetworkConnector.Instance.SetOrUpdateUserCharacter(nickname, charIndex);
            nicknames.Add(nickname);

            // 팀 정보 저장
            string team = (up.Length >= 3 && !string.IsNullOrWhiteSpace(up[2])) ? up[2].Trim() : "None";
            NetworkConnector.Instance.UserTeams[nickname] = team;
        }

        NetworkConnector.Instance.CurrentRoomName = roomName;
        NetworkConnector.Instance.SelectedMap = mapName;
        NetworkConnector.Instance.CurrentUserList = nicknames;
        NetworkConnector.Instance.CurrentRoomLeader = (nicknames.Count > 0) ? nicknames[0] : null;
        NetworkConnector.Instance.IsCoopMode = (coopFlag == "1");

        Debug.Log($"[입장 성공] 방: {roomName}, 맵: {mapName}, 유저: {string.Join(",", nicknames)}, Coop: {coopFlag}");

        // 방 씬으로 이동
        NetworkConnector.Instance.PendingRoomEnterMessage = message;
        SceneManager.LoadScene("RoomScene");
    }

    public void HandleRoomCreated(string message)
    {
        // 메시지 예: ROOM_CREATED|123|Map1|user1,user2|HAS_PASSWORD
        string[] parts = message.Split('|');
        if (parts.Length < 4) return;

        string roomName = parts[1].Trim();
        string mapName = parts[2].Trim();
        string userListStr = parts[3].Trim();
        bool hasPassword = parts.Length > 4 && parts[4].Trim() == "HAS_PASSWORD";

        bool isCoop = (parts.Length >= 6) ? (parts[5].Trim() == "1") : false;

        lobbyRoom.roomPasswordMap[roomName] = hasPassword;
        lobbyRoom.roomCoopMap[roomName] = isCoop;

        List<string> userList = userListStr.Split(',').Select(u => u.Trim()).ToList();
        Sprite sprite = lobbyRoom.GetSpriteForMap(mapName);

        for (int i = 0; i < lobbyRoom.roomButtons.Length; i++)
        {
            if (!lobbyRoom.isOccupied[i])
            {
                lobbyRoom.SetRoomButton(lobbyRoom.roomButtons[i], roomName, sprite);
                lobbyRoom.isOccupied[i] = true;
                break;
            }
        }

        Debug.Log($"[ROOM_CREATED] {roomName} | 비번 있음: {hasPassword} | 유저: {userListStr}");
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

        int icon = TryInt(p[3], 1);

        int[] equippedEmos = new int[4];
        equippedEmos[0] = TryInt(p[4], -1);
        equippedEmos[1] = TryInt(p[5], -1);
        equippedEmos[2] = TryInt(p[6], -1);
        equippedEmos[3] = TryInt(p[7], -1);

        int balloonIndex = TryInt(p[8], -1);

        userPage.SetUserInfoUI(nickname, level, icon, equippedEmos, balloonIndex);

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

            if (idx >= 0) top3Triples.Add(new int[] { idx, win, lose });
        }

        if (LobbyUserList.LastRequestedNickname != null &&
            !string.Equals(LobbyUserList.LastRequestedNickname, nickname))
        {
            Debug.Log($"[WINRATE] 요청한 닉네임과 다르므로 무시: {nickname}");
            return;
        }

        userPage.SetWinRateUI(nickname, totalWin, totalLose, top3Triples);
    }

    private static int TryInt(string s, int def)
        => int.TryParse(s.Trim(), out var v) ? v : def;
}
