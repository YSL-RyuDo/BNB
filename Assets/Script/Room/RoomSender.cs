using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Threading.Tasks;

public class RoomSender : MonoBehaviour
{
    private bool startGameLocked = false;

    public void ResetStartGameLock() => startGameLocked = false;

    public async void SendChooseCharacter(string roomName, string nickname, int characterIndex)
    {
        string message = $"CHOOSE_CHARACTER|{roomName}|{nickname}|{characterIndex}\n";
        await SendToServer(message);
    }

    public async void SendStartGame(string roomName)
    {
        if (startGameLocked)
        {
            Debug.Log("[RoomSender] START_GAME �ߺ� ��û ����");
            return;
        }

        // ��Ʈ�� ���� ���� üũ
        var stream = NetworkConnector.Instance?.Stream;
        if (stream == null || !stream.CanWrite)
        {
            Debug.LogWarning("[RoomSender] Stream �Ұ�. START_GAME ���� ���");
            return;
        }

        startGameLocked = true; // �� ���� ���
        string message = $"START_GAME|{roomName}\n";

        // ���� �� �� �����ǵ��� ���� ����(���� ó��)
        try
        {
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(bytes, 0, bytes.Length);
            await stream.FlushAsync();
        }
        catch (System.Exception ex)
        {
            startGameLocked = false; // �����ϸ� ��õ� ���
            Debug.LogError($"[RoomSender] START_GAME �۽� ����: {ex.Message}");
        }
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
            Debug.LogWarning("[RoomSender] ��Ʈ���� ��ȿ���� �ʽ��ϴ�.");
        }
    }
}
