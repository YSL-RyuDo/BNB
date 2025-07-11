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
                LobbySystem lobbySystem = FindObjectOfType<LobbySystem>();
                if (lobbySystem != null)
                    lobbySystem.HandleUserListMessage(message);
                else
                    Debug.LogWarning("LobbySystem을 찾을 수 없습니다.");
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
                    Debug.LogWarning("LobbySystem을 찾을 수 없습니다.");
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
                        lobbySystem2.HandleRoomCreated(message); // 별도 함수로 분리하는 게 안전
                    }
                    break;
                }
            case "ENTER_ROOM_SUCCESS":
                {
                    PendingRoomEnterMessage = message;

                    // 씬 로드 완료 후 실행될 콜백 등록
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
                        // 메시지 포맷: ROOM_CHAT|roomName|userNickname:chatMessage
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
                                Debug.LogWarning("ROOM_CHAT 닉네임/메시지 파싱 오류: " + parts2[2]);
                            }
                        }
                        else
                        {
                            Debug.LogWarning("ROOM_CHAT 메시지 포맷 오류: " + message);
                        }
                    }
                    break;
                }
                case "UPDATE_CHARACTER":
                    {
                        // 메시지 포맷: UPDATE_CHARACTER|닉네임|캐릭터인덱스
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
                                    Debug.LogWarning("RoomSystem 컴포넌트를 찾을 수 없습니다.");
                                }
                            }
                            else
                            {
                                Debug.LogWarning("UPDATE_CHARACTER 캐릭터 인덱스 파싱 실패: " + parts3[2]);
                            }
                        }
                        else
                        {
                            Debug.LogWarning("UPDATE_CHARACTER 메시지 포맷 오류: " + message);
                        }
                        break;
                    }


            default:
                Debug.LogWarning("알 수 없는 서버 명령: " + command);
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
                // 파싱: ENTER_ROOM_SUCCESS|roomName|qwe:2,asd:1,zxc:0
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
                                Debug.Log($"[초기화] {nickname} → 캐릭터 인덱스 {charIndex}");
                            }
                            else
                            {
                                NetworkConnector.Instance.CurrentUserCharacterIndices[nickname] = 0;
                                Debug.LogWarning($"[파싱 실패: 기본값] {nickname} → 캐릭터 인덱스 0");
                            }
                        }
                        else
                        {
                            // 캐릭터 인덱스 없는 경우 기본값 0
                            string nickname = token.Trim();
                            NetworkConnector.Instance.CurrentUserCharacterIndices[nickname] = 0;
                            Debug.Log($"[기본값 설정] {nickname} → 캐릭터 인덱스 0");
                        }
                    }
                }

                roomSystem.HandleUserJoined(PendingRoomEnterMessage);
                PendingRoomEnterMessage = null;
            }
            else
            {
                Debug.LogWarning("RoomSystem을 찾을 수 없거나 PendingRoomEnterMessage가 비어 있음");
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
            Debug.LogError("소켓 닫기 실패: " + e.Message);
        }
    }
}
