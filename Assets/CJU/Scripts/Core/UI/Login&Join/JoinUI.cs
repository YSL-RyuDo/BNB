using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

public class JoinUI : MonoBehaviour
{
    public TMP_InputField nicknameInputField;
    public TMP_InputField idInputField;
    public TMP_InputField passwordInputField;
    public Button joinButton;

    void Start()
    {
        joinButton.onClick.AddListener(OnJoinClicked);
    }

    private async void OnJoinClicked()
    {
        string id = idInputField.text.Trim();
        string password = passwordInputField.text.Trim();
        string nickname = nicknameInputField.text.Trim();

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(nickname))
        {
            Debug.LogWarning("��� �ʵ带 �Է��ϼ���.");
            return;
        }

        string packet = $"REGISTER|{id},{password},{nickname}\n";
        byte[] bytes = Encoding.UTF8.GetBytes(packet);
        await NetworkConnector.Instance.Stream.WriteAsync(bytes, 0, bytes.Length);
        Debug.Log("ȸ������ ��Ŷ ����: " + packet);
    }

}
