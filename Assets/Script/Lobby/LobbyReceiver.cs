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

    // �޽��� ����
    private readonly string[] commands =
    {
        "ROOM_LIST", "ROOM_CREATED", "CREATE_ROOM_SUCCESS",
        "ENTER_ROOM_SUCCESS", "LOBBY_CHAT", "LOBBY_USER_LIST", "USER_INFO"
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

    // ������ �޽��� ó��
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

    public void HandleUserListMessage(string message)
    {
        if (userList == null || userList.userListContent == null)
        {
            Debug.LogWarning("HandleUserListMessage: userList �Ǵ� userListContent�� null�Դϴ�. �޽��� ������.");
            return;
        }


        string[] parts = message.Split("|");
        if (parts.Length < 2)
        {
            Debug.LogWarning("�߸��� LOBBY_USER_LIST �޼��� ����");
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
                Debug.LogWarning($"���� ���� �Ľ� ����: {parts[i]}");
                continue;
            }

            string nickname = userInfo[0];
            int level = 1;
            if (!int.TryParse(userInfo[1], out level))
            {
                Debug.LogWarning($"���� �Ľ� ����:{userInfo[1]}");
                level = 1;
            }

            userList.UpdateUserList(nickname, level);  
        }
    }

    public void HandleRoomListMessage(string message)
    {
        Debug.Log("HandleRoomListMessage ȣ��: " + message);

        Debug.Log($"roomButtons.Length = {lobbyRoom.roomButtons?.Length ?? -1}");
        Debug.Log($"isOccupied.Length = {lobbyRoom.isOccupied?.Length ?? -1}");

        for (int i = 0; i < lobbyRoom.roomButtons.Length; i++)
        {
            Debug.Log($"roomButtons[{i}] is {(lobbyRoom.roomButtons[i] == null ? "null" : "not null")}");
        }

        if (lobbyRoom.isOccupied == null)
        {
            Debug.LogError("isOccupied �迭�� null�Դϴ�!");
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
            Debug.LogError("ROOM_LIST �޽��� ���� ����");
            return;
        }

        string data = message.Substring("ROOM_LIST|".Length);
        string[] rooms = data.Split('|', StringSplitOptions.RemoveEmptyEntries);

        Debug.Log($"�����κ��� ���� �� ����: {rooms.Length}");

        // 1) UI ���� ��� �ʱ�ȭ
        for (int i = 0; i < lobbyRoom.roomButtons.Length; i++)
        {
            lobbyRoom.ResetRoomButton(lobbyRoom.roomButtons[i]);
            lobbyRoom.isOccupied[i] = false;
        }

        // 2) ���� �� ����Ʈ�� 0�� ���Ժ��� ������� UI�� ����
        for (int i = 0; i < rooms.Length && i < lobbyRoom.roomButtons.Length; i++)
        {
            string[] f = rooms[i].Split(new[] { ',' }, 5, StringSplitOptions.None);
            if (f.Length < 3)
            {
                Debug.LogWarning($"�� ���� ���� ����: {rooms[i]}");
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

            Debug.Log($"roomButtons[{i}]�� �� '{roomName}' ���� �Ϸ�");
        }

    }

    public void HandleLobbyChatMessage(string message)
    {
        Debug.Log("HandleLobbyChatMessage ȣ��: " + message);

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
                Debug.LogWarning("LOBBY_CHAT �г���/�޽��� �Ľ� ����: " + parts[1]);
            }
        }
        else
        {
            Debug.LogWarning("LOBBY_CHAT �޽��� ���� ����: " + message);
        }
    }

    public void HandleCreateRoomSuccess(string message)
    {
        string[] parts = message.Split('|');
        if (parts.Length < 3) return;

        string roomName = parts[1].Trim();
        string mapName = parts[2].Trim();
        bool isCreator = parts.Length > 3 && parts[3] == "CREATOR";

        // ���� ����Ʈ �Ľ�
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

        // [�߰�] ��й�ȣ ���� �Ľ�
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

        string rawUserListStr = userListStr; // team �ʵ�(:Blue/:Red)�� ���Ե� ����
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

            // ĳ���� ���� ����
            NetworkConnector.Instance.SetOrUpdateUserCharacter(nickname, charIndex);
            nicknames.Add(nickname);

            // �� ���� ����
            string team = (up.Length >= 3 && !string.IsNullOrWhiteSpace(up[2])) ? up[2].Trim() : "None";
            NetworkConnector.Instance.UserTeams[nickname] = team;
        }

        NetworkConnector.Instance.CurrentRoomName = roomName;
        NetworkConnector.Instance.SelectedMap = mapName;
        NetworkConnector.Instance.CurrentUserList = nicknames;
        NetworkConnector.Instance.CurrentRoomLeader = (nicknames.Count > 0) ? nicknames[0] : null;
        NetworkConnector.Instance.IsCoopMode = (coopFlag == "1");

        Debug.Log($"[���� ����] ��: {roomName}, ��: {mapName}, ����: {string.Join(",", nicknames)}, Coop: {coopFlag}");

        // �� ������ �̵�
        NetworkConnector.Instance.PendingRoomEnterMessage = message;
        SceneManager.LoadScene("RoomScene");
    }

    public void HandleRoomCreated(string message)
    {
        // �޽��� ��: ROOM_CREATED|123|Map1|user1,user2|HAS_PASSWORD
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

        Debug.Log($"[ROOM_CREATED] {roomName} | ��� ����: {hasPassword} | ����: {userListStr}");
    }

}
