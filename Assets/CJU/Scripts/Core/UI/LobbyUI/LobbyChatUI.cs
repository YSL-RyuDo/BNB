using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

public class LobbyChatUI : MonoBehaviour
{
    public TMP_InputField chatInputField;
    public Button sendButton;
    public Transform chatContent;
    public GameObject chatTextPrefab;

    // Start is called before the first frame update
    void Start()
    {
        sendButton.onClick.AddListener(OnSendButtonClicked);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnSendButtonClicked()
    {
        string message = chatInputField.text.Trim();
        if (string.IsNullOrEmpty(message)) return;

        string nickname = NetworkConnector.Instance.UserNickname;
        string fullMessage = $"LOBBY_MESSAGE|{nickname}:{message}\n";

        SendToServer(fullMessage);

        chatInputField.text = "";
        chatInputField.ActivateInputField();
    }
    public void AddChatMessage(string msg)
    {
        GameObject textObj = Instantiate(chatTextPrefab, chatContent);
        TextMeshProUGUI textComp = textObj.GetComponent<TextMeshProUGUI>();
        textComp.text = msg;

        Canvas.ForceUpdateCanvases();
        ScrollRect scroll = chatContent.GetComponentInParent<ScrollRect>();
        scroll.verticalNormalizedPosition = 0f;
    }

    private async void SendToServer(string msg)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(msg);
        await NetworkConnector.Instance.Stream.WriteAsync(bytes, 0, bytes.Length);
        Debug.Log($"[To Server] {msg}");
    }
}
