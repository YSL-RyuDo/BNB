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
    public string CurrentMap { get; set; }
    public bool IsRoomOwner { get; set; }
    public bool IsRoomPassworded { get; set; }

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

            Debug.Log("서버 연결 완료");
        }
        catch (Exception ex)
        {
            Debug.Log("서버 연결 실패");
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
                    Debug.Log("서버 메시지 처리: " + msg);
                    HandleServerMessage(msg);
                }
            }
            completeMessage.Clear();
            completeMessage.Append(splitMessages[splitMessages.Length - 1]);
        }
    }


    private void HandleServerMessage(string message)
    {
        // 메시지 끝에 줄바꿈(\n)이 붙어있을 수 있으니 Trim해서 제거
        message = message.Trim();

        // 메시지를 '|'로 구분해서 명령과 데이터를 분리
        string[] parts = message.Split('|');
        if (parts.Length < 1)
        {
            Debug.LogWarning("잘못된 서버 메시지 형식");
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
                    Debug.LogWarning("LoginSystem을 찾을 수 없습니다.");
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
                    Debug.LogWarning("RegisterSystem을 찾을 수 없습니다.");
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
                LobbyManager lobby = FindObjectOfType<LobbyManager>();
                if (lobby != null)
                    lobby.OnReceiveMessage(message);
                break;
            case "ROOM_CREATED":
                LobbyManager lobbyRoom = FindObjectOfType<LobbyManager>();
                if (lobbyRoom != null)
                    lobbyRoom.OnReceiveMessage(message);
                break;
            case "LOBBY_CHAT":
                LobbyManager lobbyChat = FindObjectOfType<LobbyManager>();
                if (lobbyChat != null)
                {
                    lobbyChat.OnReceiveMessage(message);
                }
                break;
            case "ENTER_ROOM_SUCCESS":
                {
                    LobbyManager lobbyEnterRoom = FindObjectOfType<LobbyManager>();
                    if (lobbyEnterRoom != null)
                    {
                        lobbyEnterRoom.OnReceiveMessage(message);
                    }
                    break;
                }
            default:
                Debug.LogWarning("알 수 없는 서버 명령: " + command);
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
            Debug.LogError("소켓 닫기 실패: " + e.Message);
        }
    }
}
