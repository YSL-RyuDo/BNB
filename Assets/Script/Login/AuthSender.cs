using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Threading.Tasks;

// 로그인 및 회원가입 정보 인증용 메시지 송신 클래스
public static class AuthSender
{
    // 로그인 요청
    public static async Task SendLoginRequest(string id, string pw)
    {
        string message = $"LOGIN|{id},{pw}\n";
        await SendToServer(message);
        
    }

    // 회원가입 요청
    public static async Task SendRegisterRequest(string id, string pw, string nickname)
    {
        string message = $"REGISTER|{id},{pw},{nickname}\n";
        await SendToServer(message);

    }

    // 서버로 메시지 보내는 함수
    public static async Task SendToServer(string message)
    {
        try
        {
            var stream = NetworkConnector.Instance.Stream;
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            await Task.Delay(100);
            await stream.WriteAsync(bytes, 0, bytes.Length);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("종료 메시지 전송 실패: " + ex.Message);
        }
    }

}
