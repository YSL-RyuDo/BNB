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
            Debug.Log("[RoomSender] START_GAME 중복 요청 무시");
            return;
        }

        // 스트림 상태 사전 체크
        var stream = NetworkConnector.Instance?.Stream;
        if (stream == null || !stream.CanWrite)
        {
            Debug.LogWarning("[RoomSender] Stream 불가. START_GAME 전송 취소");
            return;
        }

        startGameLocked = true; // ★ 먼저 잠금
        string message = $"START_GAME|{roomName}\n";

        // 실패 시 락 해제되도록 직접 보냄(예외 처리)
        try
        {
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(bytes, 0, bytes.Length);
            await stream.FlushAsync();
        }
        catch (System.Exception ex)
        {
            startGameLocked = false; // 실패하면 재시도 허용
            Debug.LogError($"[RoomSender] START_GAME 송신 실패: {ex.Message}");
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
            Debug.LogWarning("[RoomSender] 스트림이 유효하지 않습니다.");
        }
    }
}
