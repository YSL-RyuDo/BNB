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

    public string CurrentRoomLeader { get; set; }

    public List<string> CurrentUserList = new List<string>();

    public Dictionary<string, int> CurrentUserCharacterIndices = new Dictionary<string, int>();

    public string PendingRoomEnterMessage { get; set; }
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
                LobbySystem lobbySystem = FindObjectOfType<LobbySystem>();
                if (lobbySystem != null)
                    lobbySystem.HandleUserListMessage(message);
                else
                    Debug.LogWarning("LobbySystem�� ã�� �� �����ϴ�.");
                break;
            case "ROOM_LIST":
                LobbySystem roomListManager = FindObjectOfType<LobbySystem>();
                if (roomListManager != null)
                {
                    roomListManager.HandleRoomListMessage(message);
                }
                break;
            case "USER_INFO":
                LobbySystem roomUserInfoManager = FindObjectOfType<LobbySystem>();
                if (roomUserInfoManager != null)
                {
                    roomUserInfoManager.HandleUserInfoMessage(message);
                }
                break;
            case "LOBBY_CHAT":
                LobbySystem lobbyChatSystem = FindObjectOfType<LobbySystem>();
                if (lobbyChatSystem != null)
                {
                    lobbyChatSystem.HandleLobbyChatMessage(message);
                }
                else
                {
                    Debug.LogWarning("LobbySystem�� ã�� �� �����ϴ�.");
                }
                break;
            case "CREATE_ROOM_SUCCESS":
                LobbySystem lobbySystem1 = FindObjectOfType<LobbySystem>();
                if (lobbySystem1 != null)
                    lobbySystem1.HandleCreateRoomSuccess(message);
                break;
            case "ROOM_CREATED":
                {
                    LobbySystem lobbySystem2 = FindObjectOfType<LobbySystem>();
                    if (lobbySystem2 != null)
                    {
                        lobbySystem2.HandleRoomCreated(message); // ���� �Լ��� �и��ϴ� �� ����
                    }
                    break;
                }
            case "ENTER_ROOM_SUCCESS":
                {
                    PendingRoomEnterMessage = message;

                    // �� �ε� �Ϸ� �� ����� �ݹ� ���
                    SceneManager.sceneLoaded += OnRoomSceneLoaded;
                    SceneManager.LoadScene("RoomScene");
                    break;
                }
            case "REFRESH_ROOM_SUCCESS":
                RoomSystem roomSystem = FindObjectOfType<RoomSystem>();
                if (roomSystem != null)
                    roomSystem.HandleUserJoined(message);
                break;
            case "ROOM_CHAT":
                {
                    RoomChatManager roomChatManager = FindObjectOfType<RoomChatManager>();
                    if (roomChatManager != null)
                    {
                        // �޽��� ����: ROOM_CHAT|roomName|userNickname:chatMessage
                        string[] parts2 = message.Split('|');
                        if (parts.Length >= 3)
                        {
                            string roomName = parts2[1];
                            string[] chatParts = parts2[2].Split(':');
                            if (chatParts.Length >= 2)
                            {
                                string userNickname = chatParts[0];
                                string chatMessage = string.Join(":", chatParts, 1, chatParts.Length - 1);

                                roomChatManager.AddChatMessage(userNickname, chatMessage);
                            }
                            else
                            {
                                Debug.LogWarning("ROOM_CHAT �г���/�޽��� �Ľ� ����: " + parts2[2]);
                            }
                        }
                        else
                        {
                            Debug.LogWarning("ROOM_CHAT �޽��� ���� ����: " + message);
                        }
                    }
                    break;
                }
                case "UPDATE_CHARACTER":
                    {
                        // �޽��� ����: UPDATE_CHARACTER|�г���|ĳ�����ε���
                        string[] parts3 = message.Split('|');
                        if (parts3.Length >= 3)
                        {
                            string userNickname = parts3[1];
                            if (int.TryParse(parts3[2], out int characterIndex))
                            {
                                RoomSystem roomSystem2 = FindObjectOfType<RoomSystem>();
                                if (roomSystem2 != null)
                                {
                                    roomSystem2.UpdateCharacterChoice(userNickname, characterIndex);
                                }
                                else
                                {
                                    Debug.LogWarning("RoomSystem ������Ʈ�� ã�� �� �����ϴ�.");
                                }
                            }
                            else
                            {
                                Debug.LogWarning("UPDATE_CHARACTER ĳ���� �ε��� �Ľ� ����: " + parts3[2]);
                            }
                        }
                        else
                        {
                            Debug.LogWarning("UPDATE_CHARACTER �޽��� ���� ����: " + message);
                        }
                        break;
                    }


            default:
                Debug.LogWarning("�� �� ���� ���� ���: " + command);
                break;
        }
    }

    private void OnRoomSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "RoomScene")
        {
            RoomSystem roomSystem = FindObjectOfType<RoomSystem>();
            if (roomSystem != null && !string.IsNullOrEmpty(PendingRoomEnterMessage))
            {
                // �Ľ�: ENTER_ROOM_SUCCESS|roomName|qwe:2,asd:1,zxc:0
                string[] parts = PendingRoomEnterMessage.Split('|');
                if (parts.Length >= 3)
                {
                    string[] userTokens = parts[2].Split(',');
                    foreach (string token in userTokens)
                    {
                        if (token.Contains(":"))
                        {
                            string[] pair = token.Split(':');
                            string nickname = pair[0].Trim();
                            if (int.TryParse(pair[1], out int charIndex))
                            {
                                NetworkConnector.Instance.CurrentUserCharacterIndices[nickname] = charIndex;
                                Debug.Log($"[�ʱ�ȭ] {nickname} �� ĳ���� �ε��� {charIndex}");
                            }
                            else
                            {
                                NetworkConnector.Instance.CurrentUserCharacterIndices[nickname] = 0;
                                Debug.LogWarning($"[�Ľ� ����: �⺻��] {nickname} �� ĳ���� �ε��� 0");
                            }
                        }
                        else
                        {
                            // ĳ���� �ε��� ���� ��� �⺻�� 0
                            string nickname = token.Trim();
                            NetworkConnector.Instance.CurrentUserCharacterIndices[nickname] = 0;
                            Debug.Log($"[�⺻�� ����] {nickname} �� ĳ���� �ε��� 0");
                        }
                    }
                }

                roomSystem.HandleUserJoined(PendingRoomEnterMessage);
                PendingRoomEnterMessage = null;
            }
            else
            {
                Debug.LogWarning("RoomSystem�� ã�� �� ���ų� PendingRoomEnterMessage�� ��� ����");
            }

            SceneManager.sceneLoaded -= OnRoomSceneLoaded;
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
