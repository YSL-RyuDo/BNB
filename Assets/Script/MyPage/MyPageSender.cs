using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using System.Threading.Tasks;

public class MyPageSender : MonoBehaviour
{
    public TextMeshProUGUI errorText;


    // 유저 정보 요청
    public async void SendGetUserInfo(string nickname)
    {
        string message = $"GET_USER_INFO|{nickname}\n";
        await SendToServer(message);
    }

    public async void SendGetEmoticon(string nickname)
    {
        string message = $"GET_EMO|{nickname}\n";
        await SendToServer(message);
    }

    public async Task<bool> SendToServer(string message)
    {
        var stream = NetworkConnector.Instance.Stream;

        if (stream == null)
        {
            ShowError("서버에 연결되어 있지 않습니다.");
            return false;
        }

        try
        {
            byte[] sendBytes = System.Text.Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(sendBytes, 0, sendBytes.Length);
            await stream.FlushAsync();
            return true;
        }
        catch (System.Exception ex)
        {
            ShowError("서버 전송 오류: " + ex.Message);
            return false;
        }
    }

    private void ShowError(string message)
    {
        if (errorText != null)
            errorText.text = message;
    }
}
