using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class RoomSender : MonoBehaviour
{
    public async void SendChooseCharacter(string roomName, string nickname, int characterIndex)
    {
        string message = $"CHOOSE_CHARACTER|{roomName}|{nickname}|{characterIndex}\n";
        await SendToServer(message);
    }
    public async void SendStartGame(string roomName)
    {
        string message = $"START_GAME|{roomName}\n";
        await SendToServer(message);
    }

    public async void SendExitRoom(string roomName, string nickname)
    {
        string message = $"EXIT_ROOM|{roomName}|{nickname}\n";
        await SendToServer(message);
    }

    public async void SendGetCharacterInfo(string nickname)
    {
        string message = $"GET_CHARACTER|{nickname}\n";
        await SendToServer(message);
    }

    public async void SendRoomChat(string roomName, string nickname, string message)
    {
        string msg = $"ROOM_MESSAGE|{roomName}:{nickname}:{message}\n";
        await SendToServer(msg);
    }

    public async Task<bool> SendToServer(string message)
    {
        var stream = NetworkConnector.Instance.Stream;

        if (stream == null)
        {
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
            return false;
        }
    }
}
