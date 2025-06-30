using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Text;

public class JOINTEST : MonoBehaviour
{
    public Button button1;
    public Button button2;
    public Button button3;
    public TMP_InputField nickname;
    private string userNickname = "";
    void Start()
    {
        nickname.onValueChanged.AddListener(OnNicknameChanged);
        button1.onClick.AddListener(() => SendToServerAsync(0));
        button2.onClick.AddListener(() => SendToServerAsync(1));
        button3.onClick.AddListener(() => SendToServerAsync(2));
    }

    private void OnNicknameChanged(string newName)
    {
        userNickname = newName.Trim();
    }

    async void SendToServerAsync(int type)
    {
        try
        {
            var stream = NetworkConnector.Instance.Stream;
            if (stream == null || !stream.CanWrite)
            {
                Debug.LogWarning("���� ��Ʈ���� ��ȿ���� �ʽ��ϴ�.");
                return;
            }

            if (string.IsNullOrEmpty(userNickname))
            {
                Debug.LogWarning("�г����� �Է��ϼ���.");
                return;
            }
            NetworkConnector.Instance.UserNickname = userNickname;
            // ����� userNickname�� type �����Ͽ� �޽��� ����
            string sendStr = $"JOIN|{userNickname},{type}\n";

            byte[] sendBytes = Encoding.UTF8.GetBytes(sendStr);
            await stream.WriteAsync(sendBytes, 0, sendBytes.Length);

            Debug.Log($"����: {sendStr.Trim()}");
        }
        catch (IOException e)
        {
            Debug.LogError("������ �޽��� ���� �� IOException �߻�: " + e.Message);
        }
        catch (System.Exception e)
        {
            Debug.LogError("������ �޽��� ���� �� ���� �߻�: " + e.Message);
        }
    }
}