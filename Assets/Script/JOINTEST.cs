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
                Debug.LogWarning("서버 스트림이 유효하지 않습니다.");
                return;
            }

            if (string.IsNullOrEmpty(userNickname))
            {
                Debug.LogWarning("닉네임을 입력하세요.");
                return;
            }
            NetworkConnector.Instance.UserNickname = userNickname;
            // 저장된 userNickname과 type 조합하여 메시지 생성
            string sendStr = $"JOIN|{userNickname},{type}\n";

            byte[] sendBytes = Encoding.UTF8.GetBytes(sendStr);
            await stream.WriteAsync(sendBytes, 0, sendBytes.Length);

            Debug.Log($"보냄: {sendStr.Trim()}");
        }
        catch (IOException e)
        {
            Debug.LogError("서버로 메시지 전송 중 IOException 발생: " + e.Message);
        }
        catch (System.Exception e)
        {
            Debug.LogError("서버로 메시지 전송 중 예외 발생: " + e.Message);
        }
    }
}