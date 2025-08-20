using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomReceiver : MonoBehaviour, IMessageHandler
{
    [SerializeField] private RoomUI roomUI;

    private void OnEnable()
    {
        var connector = NetworkConnector.Instance;
        connector.RoomHandler("REFRESH_ROOM_SUCCESS", this);
        connector.RoomHandler("CHARACTER_LIST", this);
        connector.RoomHandler("UPDATE_CHARACTER", this);
    }

    private void OnDisable()
    {
        var connector = NetworkConnector.Instance;
        connector.RemoveRoomHandler("REFRESH_ROOM_SUCCESS", this);
        connector.RemoveRoomHandler("CHARACTER_LIST", this);
        connector.RemoveRoomHandler("UPDATE_CHARACTER", this); 
    }


    public void HandleMessage(string message)
    {
        string command = message.Split('|')[0];
        switch (command)
        {
            case "REFRESH_ROOM_SUCCESS":
                Debug.Log("[RoomReceiver RAW] " + message);
                roomUI.HandleUserJoined(message);
                break;
            case "CHARACTER_LIST":
                roomUI.HandleCharacterList(message);
                break;
            case "UPDATE_CHARACTER":
                HandleUpdateCharacter(message);
                break;
            default:
                Debug.LogWarning($"[RoomReceiver] �� �� ���� �޽���: {message}");
                break;
        }
    }

    private void HandleUpdateCharacter(string message)
    {
        // �޽��� ����: UPDATE_CHARACTER|�г���|ĳ�����ε���
        string[] parts = message.Split('|');
        if (parts.Length < 3)
        {
            Debug.LogWarning("UPDATE_CHARACTER �޽��� ���� ����: " + message);
            return;
        }

        string nickname = parts[1];
        if (!int.TryParse(parts[2], out int characterIndex))
        {
            Debug.LogWarning("UPDATE_CHARACTER ĳ���� �ε��� �Ľ� ����: " + parts[2]);
            return;
        }

        NetworkConnector.Instance.SetOrUpdateUserCharacter(nickname, characterIndex);

        if (roomUI != null)
        {
            roomUI.UpdatePlayerInfoUI(NetworkConnector.Instance.CurrentUserList);
        }
        else
        {
            Debug.LogWarning("roomUI�� ã�� �� �����ϴ�. UI ������Ʈ ����");
        }
    }

}
