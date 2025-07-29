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
    [SerializeField] private LobbyRoomList roomList;
    [SerializeField] private LobbyChat chat;
    [SerializeField] private LobbyRoomEnter roomEnter;
    [SerializeField] private LobbyCreateRoom createRoom;

    public void HandleMessage(string message)
    {

        string[] parts = message.Split('|');
        string command = parts[0];

        if (message == "ROOM_LIST")
        {
            HandleRoomListMessage(message);
        }
        else if (message == "ROOM_CREATED")
        {
            HandleRoomCreated(message);
        }
        else if (message == "CREATE_ROOM_SUCCESS")
        {
            HandleCreateRoomSuccess(message);
        }
        else if (message == "ENTER_ROOM_SUCCESS")
        {
            HandleEnterRoomSuccess(message);
        }
        else if (message == "LOBBY_CHAT")
        {
            HandleLobbyChatMessage(message);
        }
        else if(message == "LOBBY_USER_LIST")
        {
            HandleUserListMessage(message);
        }
        else if(message == "USER_INFO")
        {
            HandleUserInfoMessage(message);
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
        string[] parts = message.Split("|");
        if (parts.Length < 2)
        {
            Debug.LogWarning("�߸��� LOBBY_USER_LIST �޼��� ����");
            return;
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
        if (!message.StartsWith("ROOM_LIST|"))
        {
            Debug.LogError("ROOM_LIST �޽��� ���� ����");
            return;
        }

        string data = message.Substring("ROOM_LIST|".Length);
        string[] rooms = data.Split('|', StringSplitOptions.RemoveEmptyEntries);

        List<LobbyRoomData> parsedRooms = new();

        foreach (string entry in rooms)
        {
            string[] parts = entry.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) continue;

            string roomName = parts[0].Trim();
            string mapName = parts[1].Trim();
            bool hasPassword = parts[2].Trim() == "1";

            parsedRooms.Add(new LobbyRoomData(roomName, mapName, hasPassword));
        }

        roomEnter.SetRoomPasswordMap(parsedRooms.ToDictionary(r => r.RoomName, r => r.HasPassword));
        roomList.UpdateRoomList(parsedRooms);
        
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
        Debug.Log("HandleCreateRoomSuccessMessage ȣ��: " + message);

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
        roomEnter.roomPasswordMap[roomName] = hasPassword;

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
                        characterIndexMap[nick] = 0; // �⺻��
                        Debug.LogWarning($"ĳ���� �ε��� �Ľ� ����: {userParts[1]}");
                    }
                }
                else
                {
                    string nick = entry.Trim();
                    userList.Add(nick);
                    characterIndexMap[nick] = 0; // �⺻��
                }
            }
        }

        NetworkConnector.Instance.CurrentUserList = userList;
        NetworkConnector.Instance.SetUserCharacterIndices(characterIndexMap);
        NetworkConnector.Instance.CurrentRoomName = roomName;
        NetworkConnector.Instance.SelectedMap = mapName;

        Debug.Log($"[CreateRoomSuccess] mapName={mapName}");

        Sprite sprite = createRoom.GetSpriteForMap(mapName);

        roomList.AddNewRoomButton(roomName, mapName, isCreator);

        Debug.Log($"Room '{roomName}' has users: {string.Join(", ", userList)}");

        if (isCreator)
        {
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

        Debug.Log($"[���� ����] ��: {roomName}, ����: {userListStr}");

        // �� ������ ��ȯ
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

        roomEnter.roomPasswordMap[roomName] = hasPassword;

        Debug.Log($"[RoomCreated] mapName={mapName}");

        Sprite sprite = createRoom.GetSpriteForMap(mapName);

        roomList.AddNewRoomButton(roomName, mapName, isCreator: false);

        Debug.Log($"[ROOM_CREATED] {roomName} | ��� ����: {hasPassword} | ����: {userListStr}");
    }

}
