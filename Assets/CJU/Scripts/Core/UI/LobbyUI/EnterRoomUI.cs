using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text;
using UnityEngine.SceneManagement;


public class EnterRoomUI : MonoBehaviour
{
    public TMP_InputField passwordInputField;
    public Button enterButton;
    private void Start()
    {
        enterButton.onClick.AddListener(OnEnterRoomClicked);
    }
    private void OnEnterRoomClicked()
    {
        string roomName = NetworkConnector.Instance.CurrentRoomName;
        string password = passwordInputField.text.Trim();

        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogError("방 이름이 비어있습니다.");
            return;
        }

        string msg = $"ENTER_ROOM|{roomName}|{password}\n";
        SendToServer(msg);

        this.gameObject.SetActive(false);
        passwordInputField.text = "";
    }

    private async void SendToServer(string msg)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(msg);
        await NetworkConnector.Instance.Stream.WriteAsync(bytes, 0, bytes.Length);
    }
}
