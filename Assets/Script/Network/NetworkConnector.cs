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
    public string UserNickname { get; set; }
    public string SelectedMap { get; set; }
    public string CurrentRoomName { get; set; }

    public List<string> CurrentUserList = new List<string>();
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
        // �޽��� ���� �ٹٲ�(\n)�� �پ����� �� ������ Trim�ؼ� ����
        message = message.Trim();

        // �޽����� '|'�� �����ؼ� ��ɰ� �����͸� �и�
        string[] parts = message.Split('|');
        if (parts.Length < 1)
        {
            Debug.LogWarning("�߸��� ���� �޽��� ����");
            return;
        }

        string command = parts[0].Trim();
        string data = parts.Length > 1 ? parts[1] : "";

        switch (command)
        {
            case "LOGIN_SUCCESS":
            case "WRONG_PASSWORD":
            case "ID_NOT_FOUND":
                LoginSystem loginSystem = FindObjectOfType<LoginSystem>();
                if (loginSystem != null)
                    loginSystem.HandleLoginMessage(message);
                else
                    Debug.LogWarning("LoginSystem�� ã�� �� �����ϴ�.");
                break;
            case "REGISTER_SUCCESS":
            case "DUPLICATE_ID":
            case "DUPLICATE_NICK":
            case "EMPTY_PASSWORD":
            case "FILE_WRITE_ERROR":
            case "REGISTER_ERROR":
                RegisterSystem registerSystem = FindObjectOfType<RegisterSystem>();
                if (registerSystem != null)
                    registerSystem.HandleRegisterMessage(message);
                else
                    Debug.LogWarning("RegisterSystem�� ã�� �� �����ϴ�.");
                break;

            case "LOBBY_USER_LIST":
                LobbyManager lobbyUserList = FindObjectOfType<LobbyManager>();
                if (lobbyUserList != null)
                {
                    lobbyUserList.OnReceiveMessage(message);
                    Debug.Log(message);
                }
                break;
            case "ROOM_LIST":
                LobbyManager lobbyRoomList = FindObjectOfType<LobbyManager>();
                if (lobbyRoomList != null)
                {
                    lobbyRoomList.OnReceiveMessage(message);
                    Debug.Log(message);
                }
                break;
            case "USER_INFO":
                LobbyManager lobbyUserInfo = FindObjectOfType<LobbyManager>();
                if (lobbyUserInfo != null)
                {
                    lobbyUserInfo.OnReceiveMessage(message);
                }
                break;
            case "CREATE_ROOM_SUCCESS":
                break;
            case "ROOM_CREATED":
                break;
            case "LOBBY_CHAT":
                LobbyManager lobbyChat = FindObjectOfType<LobbyManager>();
                if (lobbyChat != null)
                {
                    lobbyChat.OnReceiveMessage(message);
                }
                break;
            default:
                Debug.LogWarning("�� �� ���� ���� ���: " + command);
                break;
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
