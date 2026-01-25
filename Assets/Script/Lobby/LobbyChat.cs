using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyChat : MonoBehaviour
{
    [SerializeField] private LobbySender lobbySender;

    public GameObject userChatPrefab;
    public Transform contentParent;

    public TMP_InputField messageInputField;
    public Button sendButton;


    // Start is called before the first frame update
    void Start()
    {
        sendButton.onClick.AddListener(OnsendClicked);
    }

    private void OnsendClicked()
    {
        ButtonSoundManager.Instance?.PlayClick();
        if (string.IsNullOrEmpty(messageInputField.text))
        {
            return;
        }

        string message = messageInputField.text;
        string nickname = NetworkConnector.Instance.UserNickname;

        messageInputField.text = string.Empty; // 입력 필드 초기화

        lobbySender.SendLobbyChat(nickname, message);
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
