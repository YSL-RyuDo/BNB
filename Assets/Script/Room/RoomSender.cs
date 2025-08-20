using System.Collections;
using System.Collections.Generic;
using System.Text;
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

    public async void SendStartCoopGame(string roomName)
    {
        string message = $"START_COOP_GAME|{roomName}\n";
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

    public async void SendTeamChange(string nickname)
    {
        string message = $"TEAMCHANGE|{nickname}\n";
        await SendToServer(message);
    }

    private async Task SendToServer(string message)
    {
        var stream = NetworkConnector.Instance.Stream;
        if (stream != null && stream.CanWrite)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(bytes, 0, bytes.Length);
            await stream.FlushAsync();
        }
        else
        {
            Debug.LogWarning("[RoomSender] 스트림이 유효하지 않습니다.");
        }
    }
}
