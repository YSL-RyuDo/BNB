using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using System.Threading.Tasks;


public class ShopSender : MonoBehaviour
{
    // 보유 리스트
    public async void SendGetCoin(string nickname)
    {
        string message = $"GET_COIN|{nickname}\n";
        await SendToServer(message);
    }

    public async void SendGetStoreCharList(string nickname)
    {
        string message = $"GET_STORE_CHAR_LIST|{nickname}\n";
        await SendToServer(message);
    }

    public async void SendGetStoreBalloonList(string nickname)
    {
        string message = $"GET_STORE_BALLOON_LIST|{nickname}\n";
        await SendToServer(message);
    }

    public async void SendGetStoreEmoList(string nickname)
    {
        string message = $"GET_STORE_EMO_LIST|{nickname}\n";
        await SendToServer(message);
    }

    public async void SendGetStoreIconList(string nickname)
    {
        string message = $"GET_STORE_ICON_LIST|{nickname}\n";
        await SendToServer(message);
    }


    public async void SendBuyChar(string nickname, int index)
    {
        string message = $"BUY_CHAR|{nickname},{index}\n";
        await SendToServer(message);
    }


    public async void SendBuyBalloon(string nickname, int index)
    {
        string message = $"BUY_BALLOON|{nickname},{index}\n";
        await SendToServer(message);
    }


    public async void SendBuyEmo(string nickname, int index)
    {
        string message = $"BUY_EMO|{nickname},{index}\n";
        await SendToServer(message);
    }


    public async void SendBuyIcon(string nickname, int index)
    {
        string message = $"BUY_ICON|{nickname},{index}\n";
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
