using System;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.IO;
public class NetworkConnector : MonoBehaviour
{
    private TcpClient _client;
    private NetworkStream _stream;

    public TcpClient Client => _client;
    public NetworkStream Stream => _stream;

    public static NetworkConnector Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        TryConnectToServer();
    }

    private async void TryConnectToServer()
    {
        _client = new TcpClient();

        try
        {
            await _client.ConnectAsync("127.0.0.1", 9000);
            _stream = _client.GetStream();
            StartListening();

            Debug.Log("���� ���� �Ϸ�");
        }
        catch (Exception ex)
        {
            Debug.Log("���� ���� ����");
        }
    }

    private async void StartListening()
    {
        byte[] buffer = new byte[1024];
        StringBuilder completeMessage = new StringBuilder();

        while (_client.Connected)
        {
            int byteCount = await _stream.ReadAsync(buffer, 0, buffer.Length);
            if (byteCount == 0)
                break;

            string chunk = Encoding.UTF8.GetString(buffer, 0, byteCount);
            completeMessage.Append(chunk);

            string messages = completeMessage.ToString();
            string[] splitMessages = messages.Split('\n');

            foreach (string msgRaw in splitMessages)
            {
                string msg = msgRaw.Trim();
                if (!string.IsNullOrEmpty(msg))
                {
                    Debug.Log("���� �޽��� ó��: " + msg);
                    HandleServerMessage(msg);
                }
            }
            completeMessage.Clear();
            completeMessage.Append(splitMessages[splitMessages.Length - 1]);
        }
    }


    private void HandleServerMessage(string message)
    {
        if (message.StartsWith("LOGIN_"))
        {
            LoginUI loginUI = FindObjectOfType<LoginUI>();
            loginUI?.OnLoginResponse(message);
        }
    }

    private void OnApplicationQuit()
    {
        try
        {
            _stream?.Close();
            _client?.Close();
        }
        catch (Exception e)
        {
            Debug.LogError("���� �ݱ� ����: " + e.Message);
        }
    }
}
