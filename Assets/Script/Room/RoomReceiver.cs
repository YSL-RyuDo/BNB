using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class RoomReceiver : MonoBehaviour, IMessageHandler
{
    [SerializeField] private RoomUI roomUI; 
    [SerializeField] private RoomChatManager roomChatManager;


    private void Start()
    {
        NetworkConnector.Instance.RoomHandler("REFRESH_ROOM_SUCCESS", this);
        NetworkConnector.Instance.RoomHandler("CHARACTER_LIST", this);
        NetworkConnector.Instance.RoomHandler("UPDATE_CHARACTER", this);
        NetworkConnector.Instance.RoomHandler("ENTER_ROOM_SUCCESS", this);
        NetworkConnector.Instance.RoomHandler("ROOM_CHAT", this);
    }


    public void HandleMessage(string message)
    {
        string[] parts = message.Split('|');
        string command = message.Split('|')[0];

        switch (command)
        {
            case "REFRESH_ROOM_SUCCESS":
                HandleUserJoined(message);
                break;
            case "CHARACTER_LIST":
                HandleCharacterList(message);
                break;
            case "UPDATE_CHARACTER":
                HandleUpdateCharacter(message);
                break;
            case "ENTER_ROOM_SUCCESS":
                HandleEnterRoomSuccess(message);  
                break;
            case "ROOM_CHAT":
                HandleRoomChat(message);
                break;
        }
    }

    private void HandleUserJoined(string message)
    {
        // ��: "REFRESH_ROOM_SUCCESS|���̸�|���̸�|userA:1,userB:2,userC:0"
        string[] parts = message.Split('|');
        if (parts.Length < 4)
        {
            Debug.LogWarning("REFRESH_ROOM_SUCCESS �Ľ� ����");
            return;
        }

        string roomName = parts[1];
        string mapName = parts[2];
        string[] userTokens = parts[3].Split(';');

        List<string> nicknames = new List<string>();

        // NetworkConnector.Instance.CurrentUserCharacterIndices.Clear();

        foreach (string token in userTokens)
        {
            if (token.Contains(":"))
            {
                var pair = token.Split(':');
                string nickname = pair[0].Trim();
                int charIndex = 0;
                if (!int.TryParse(pair[1], out charIndex))
                {
                    charIndex = 0; // �⺻��
                }
                NetworkConnector.Instance.SetOrUpdateUserCharacter(nickname, charIndex);
                nicknames.Add(nickname);
            }
            else
            {
                string nickname = token.Trim();
                NetworkConnector.Instance.SetOrUpdateUserCharacter(nickname, 0);
                nicknames.Add(nickname);
            }
        }

        // ����
        NetworkConnector.Instance.CurrentRoomName = roomName;
        NetworkConnector.Instance.SelectedMap = mapName;
        NetworkConnector.Instance.CurrentUserList = nicknames;
        NetworkConnector.Instance.CurrentRoomLeader = nicknames.Count > 0 ? nicknames[0] : null;

        Debug.Log($"HandleUserJoined: ���� �� = {nicknames.Count}, ���� = {(nicknames.Count > 0 ? nicknames[0] : "����")}");
        roomUI.RefreshRoomUI(nicknames, roomName);
    }

    private void HandleCharacterList(string message)
    {
        // ��: CHARACTER_LIST|1,0,1,1,0,0,1
        string[] parts = message.Split('|');
        if (parts.Length < 2)
        {
            Debug.LogWarning("[RoomSystem] CHARACTER_LIST �޽��� ���� ����");
            return;
        }

        string[] tokens = parts[1].Split(',');
        if (tokens.Length != roomUI.characterChooseButtons.Length)
        {
            Debug.LogWarning($"[RoomSystem] ĳ���� ���� ����ġ: ��ư={roomUI.characterChooseButtons.Length}, ������={tokens.Length}");
            return;
        }

        for (int i = 0; i < tokens.Length; i++)
        {
            bool hasCharacter = int.TryParse(tokens[i], out int hasChar) && hasChar == 1;

            // ĳ���� ��ư Ŭ�� ���� ����
            roomUI.characterChooseButtons[i].interactable = hasCharacter;

            // ��� �̹��� ó��
            Transform lockImage = roomUI.characterChooseButtons[i].transform.Find("LockImage");
            if (lockImage != null)
            {
                lockImage.gameObject.SetActive(!hasCharacter); // ���� �� �ϸ� ��� �̹��� �ѱ�
            }

            // �ʿ��ϸ� ���� ������ ����
            var buttonImage = roomUI.characterChooseButtons[i].GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = hasCharacter ? Color.white : new Color(1f, 1f, 1f, 0.5f);
            }
        }


        Debug.Log("[RoomSystem] ĳ���� ���� ���� UI �� ��� �ݿ� �Ϸ�");
    }

    private void HandleUpdateCharacter(string message)
    {
        string[] parts = message.Split('|');
        if (parts.Length < 3) return;

        string nick = parts[1];
        if (int.TryParse(parts[2], out int charIndex))
        {
            NetworkConnector.Instance.SetOrUpdateUserCharacter(nick, charIndex);
            roomUI.UpdatePlayerSlots(NetworkConnector.Instance.CurrentUserList);
        }
    }

    private void HandleRoomChat(string message)
    {
        string[] parts2 = message.Split('|');
        if (parts2.Length >= 3)
        {
            string roomName = parts2[1];
            string[] chatParts = parts2[2].Split(':');
            if (chatParts.Length >= 2)
            {
                string userNickname = chatParts[0];
                string chatMessage = string.Join(":", chatParts, 1, chatParts.Length - 1);

                roomChatManager.AddChatMessage(userNickname, chatMessage);
            }
            else
            {
                Debug.LogWarning("ROOM_CHAT �г���/�޽��� �Ľ� ����: " + parts2[2]);
            }
        }
        else
        {
            Debug.LogWarning("ROOM_CHAT �޽��� ���� ����: " + message);
        }
    }

    private void HandleEnterRoomSuccess(string message)
    {
        // ���� �޽���: ENTER_ROOM_SUCCESS|a|Map1|b:0,a:0
        string[] parts = message.Split('|');
        if (parts.Length < 4)
        {
            Debug.LogWarning("ENTER_ROOM_SUCCESS �Ľ� ����");
            return;
        }

        string roomName = parts[1];
        string mapName = parts[2];
        string[] userTokens = parts[3].Split(',');

        List<string> nicknames = new List<string>();

        foreach (string token in userTokens)
        {
            if (token.Contains(":"))
            {
                var pair = token.Split(':');
                string nickname = pair[0].Trim();
                int charIndex = 0;
                if (!int.TryParse(pair[1], out charIndex)) charIndex = 0;
                NetworkConnector.Instance.SetOrUpdateUserCharacter(nickname, charIndex);
                nicknames.Add(nickname);
            }
        }

        NetworkConnector.Instance.CurrentRoomName = roomName;
        NetworkConnector.Instance.SelectedMap = mapName;
        NetworkConnector.Instance.CurrentUserList = nicknames;
        NetworkConnector.Instance.CurrentRoomLeader = nicknames.Count > 0 ? nicknames[0] : null;

        roomUI.RefreshRoomUI(nicknames, roomName);  // UI ����
    }

}
