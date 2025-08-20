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
                Debug.LogWarning($"[RoomReceiver] 알 수 없는 메시지: {message}");
                break;
        }
    }

    private void HandleUpdateCharacter(string message)
    {
        // 메시지 포맷: UPDATE_CHARACTER|닉네임|캐릭터인덱스
        string[] parts = message.Split('|');
        if (parts.Length < 3)
        {
            Debug.LogWarning("UPDATE_CHARACTER 메시지 포맷 오류: " + message);
            return;
        }

        string nickname = parts[1];
        if (!int.TryParse(parts[2], out int characterIndex))
        {
            Debug.LogWarning("UPDATE_CHARACTER 캐릭터 인덱스 파싱 실패: " + parts[2]);
            return;
        }

        NetworkConnector.Instance.SetOrUpdateUserCharacter(nickname, characterIndex);

        if (roomUI != null)
        {
            roomUI.UpdatePlayerInfoUI(NetworkConnector.Instance.CurrentUserList);
        }
        else
        {
            Debug.LogWarning("roomUI를 찾을 수 없습니다. UI 업데이트 실패");
        }
    }

}
