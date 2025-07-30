using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyReceiver : MonoBehaviour, IMessageHandler
{
    [SerializeField] private LobbyUserInfo userInfo;
    [SerializeField] private LobbyUserList userList;
    [SerializeField] private LobbyChat chat;
    [SerializeField] private LobbyRoom lobbyRoom;

    // 메시지 구독
    void Start()
    {
        NetworkConnector.Instance.LobbyHandler("ROOM_LIST", this);
        NetworkConnector.Instance.LobbyHandler("ROOM_CREATED", this);
        NetworkConnector.Instance.LobbyHandler("CREATE_ROOM_SUCCESS", this);
        NetworkConnector.Instance.LobbyHandler("ENTER_ROOM_SUCCESS", this);
        NetworkConnector.Instance.LobbyHandler("LOBBY_CHAT", this);
        NetworkConnector.Instance.LobbyHandler("LOBBY_USER_LIST", this);
        NetworkConnector.Instance.LobbyHandler("USER_INFO", this);
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
            string[] parts = rooms[i].Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
            {
                Debug.LogWarning("방 정보 포맷 오류: " + rooms[i]);
                continue;
            }

            string roomName = parts[0].Trim();
            string mapName = parts[1].Trim();
            bool hasPassword = parts[2].Trim() == "1";

            lobbyRoom.roomPasswordMap[roomName] = hasPassword;

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

        List<string> userList = new List<string>();
        Dictionary<string, int> characterIndexMap = new Dictionary<string, int>();

        if (!string.IsNullOrEmpty(userListStr))
        {
            var userEntries = userListStr.Split(',');
            foreach (var entry in userEntries)
            {
                var userParts = entry.Split(':');
                if (userParts.Length == 2)
                {
                    string nick = userParts[0].Trim();
                    if (int.TryParse(userParts[1].Trim(), out int charIndex))
                    {
                        userList.Add(nick);
                        characterIndexMap[nick] = charIndex;
                    }
                    else
                    {
                        userList.Add(nick);
                        characterIndexMap[nick] = 0; // 기본값
                        Debug.LogWarning($"캐릭터 인덱스 파싱 실패: {userParts[1]}");
                    }
                }
                else
                {
                    string nick = entry.Trim();
                    userList.Add(nick);
                    characterIndexMap[nick] = 0; // 기본값
                }
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

        if (isCreator)
        {
            NetworkConnector.Instance.CurrentRoomName = roomName;
            NetworkConnector.Instance.SelectedMap = mapName;
            SceneManager.LoadScene("RoomScene");
        }
    }

    public void HandleEnterRoomSuccess(string message)
    {
        string[] parts = message.Split('|');
        if (parts.Length < 3) return;

        string roomName = parts[1];
        string userListStr = parts[2];

        NetworkConnector.Instance.CurrentRoomName = roomName;

        List<string> userList = userListStr.Split(',').ToList();
        NetworkConnector.Instance.CurrentUserList = userList;

        Debug.Log($"[입장 성공] 방: {roomName}, 유저: {userListStr}");

        // 방 씬으로 전환
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

        lobbyRoom.roomPasswordMap[roomName] = hasPassword;

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

}
