using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using System.Threading.Tasks;

public class MyPageSender : MonoBehaviour
{


    // 유저 정보 요청
    public async void SendGetInfo(string nickname)
    {
        string message = $"GETINFO|{nickname}\n";
        await SendToServer(message);
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
