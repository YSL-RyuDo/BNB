using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text;
public class RoomChatManager: MonoBehaviour
{
    public GameObject userChatPrefab;
    public Transform contentParent;

    public TMP_InputField messageInputField;
    public Button sendButton;

    void Start()
    {
        sendButton.onClick.AddListener(OnsendClicked);
    }

    private async void OnsendClicked()
    {
        if (string.IsNullOrEmpty(messageInputField.text))
        {
            return;
        }

        string message = messageInputField.text;
        string nickname = NetworkConnector.Instance.UserNickname;
        string roomName = NetworkConnector.Instance.CurrentRoomName;
        messageInputField.text = string.Empty; // 입력 필드 초기화

        try
        {
            var stream = NetworkConnector.Instance.Stream;
            string sendStr = $"ROOM_MESSAGE|{roomName}:{nickname}:{message}\n";
            byte[] sendBytes = Encoding.UTF8.GetBytes(sendStr);
            await stream.WriteAsync(sendBytes, 0, sendBytes.Length);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[RoomChatManager] 채팅 메시지 전송 중 예외 발생: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public void AddChatMessage(string nickname, string message)
    {
        CreateChatUI($"{nickname}: {message}");
    }

    private void CreateChatUI(string fullMessage)
    {
        GameObject chatItem = Instantiate(userChatPrefab, contentParent);
        chatItem.transform.SetAsLastSibling();

        var chatText = chatItem.GetComponentInChildren<TMP_Text>();
        if (chatText != null)
        {
            chatText.text = fullMessage;
        }
    }
}
