using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using System.Threading.Tasks;

public class LobbySender : MonoBehaviour
{
    public TextMeshProUGUI errorText;


    // 유저 정보 요청
    public async void SendGetUserInfo(string nickname)
    {
        string message = $"GET_USER_INFO|{nickname}\n";
        await SendToServer(message);
    }

    // 유저 리스트 요청
    public async void SendGetLobbyUserList()
    {
        string message = "GET_LOBBY_USER_LIST|\n";
        await SendToServer(message);
    }

    // 방 리스트 요청
    public async void SendGetRoomList()
    {
        string message = "GET_ROOM_LIST|\n";
        await SendToServer(message);
    }

    // 방 생성 요청
    public async Task<bool> SendCreateRoom(string roomName, string selectedMap, string password)
    {
        string message = $"CREATE_ROOM|{roomName}|{selectedMap}|{password}\n";
        return await SendToServer(message);
    }

    // 방 입장 요청
    public void SendEnterRoom(string roomName, string password)
    {
        string enterMessage = $"ENTER_ROOM|{roomName}|{password}\n";
        byte[] buffer = Encoding.UTF8.GetBytes(enterMessage);
        NetworkConnector.Instance.Stream.Write(buffer, 0, buffer.Length);
    }

    // 로비 채팅 요청
    public async void SendLobbyChat(string nickname, string message)
    {
        string formatted = $"LOBBY_MESSAGE|{nickname}:{message}\n";
        await SendToServer(formatted);
    }

    // 로그아웃 요청
    public async void SendLogout(string nicname)
    {
        string message = $"LOGOUT|{nicname}\n";
        await SendToServer(message);
    }

    public async void SendGetUserPageInfo(string nickname)
    {
        string message = $"GETINFO|{nickname}\n";
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
