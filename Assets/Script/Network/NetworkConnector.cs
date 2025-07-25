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
using System.Linq;
public class NetworkConnector : MonoBehaviour
{
    private TcpClient _client;
    private NetworkStream _stream;

    public TcpClient Client => _client;
    public NetworkStream Stream => _stream;

    public static NetworkConnector Instance { get; private set; }
    [Header("Room Info")]
    [SerializeField] private string userNickname;
    [SerializeField] private string selectedMap;
    [SerializeField] private string currentRoomName;
    [SerializeField] private string currentRoomLeader;
    [SerializeField] private string pendingRoomEnterMessage;

    [Header("User Lists")]
    [SerializeField] private List<string> currentUserList = new List<string>();
    [SerializeField] private List<UserCharacterEntry> userCharacterEntries = new List<UserCharacterEntry>();

    public string UserNickname { get => userNickname; set => userNickname = value; }
    public string SelectedMap { get => selectedMap; set => selectedMap = value; }
    public string CurrentRoomName { get => currentRoomName; set => currentRoomName = value; }
    public string CurrentRoomLeader { get => currentRoomLeader; set => currentRoomLeader = value; }
    public string PendingRoomEnterMessage { get => pendingRoomEnterMessage; set => pendingRoomEnterMessage = value; }

    public List<string> CurrentUserList { get => currentUserList; set => currentUserList = value; }
    public Dictionary<string, int> CurrentUserCharacterIndices =>
        userCharacterEntries.ToDictionary(e => e.nickname, e => e.characterIndex);

    [Serializable]
    public class UserCharacterEntry
    {
        public string nickname;
        public int characterIndex;
    }
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
            case "START_GAME_SUCCESS":
                Debug.Log("���� ���� ���� ����! �� ��ȯ...");
                UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
                break;
            case "START_GAME_FAIL":
                Debug.LogWarning("���� ���� ����: " + message);
                break;
            case "GAME_START":
                Debug.Log("���� ���� �޽��� ����, �� ��ȯ");
                UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
                break;
            case "CHARACTER_LIST":
                {
                    RoomSystem.Instance.HandleCharacterList(message);
                    break;
                }

            case "MAP_DATA":
                {
                    if (parts.Length < 4)
                    {
                        Debug.LogWarning("MAP_DATA �޽��� �Ľ� ����");
                        break;
                    }

                    string mapName = parts[1];
                    string mapRawData = parts[2];
                    string spawnRawData = parts[3];

                    Debug.Log($"[MAP_DATA ����] mapName: {mapName}");

                    MapSystem.Instance.LoadMap(mapName, mapRawData);

                    string[] spawnEntries = spawnRawData.Split(',');

                    foreach (var entry in spawnEntries)
                    {
                        if (string.IsNullOrEmpty(entry))
                            continue;

                        string[] tokens = entry.Split(':');
                        if (tokens.Length != 4)
                        {
                            Debug.LogWarning($"�߸��� ���� ������ ����: {entry}");
                            continue;
                        }

                        string playerId = tokens[0];

                        if (!int.TryParse(tokens[1], out int x) ||
                            !int.TryParse(tokens[2], out int y) ||
                            !int.TryParse(tokens[3], out int charIndex))
                        {
                            Debug.LogWarning($"��ǥ �Ǵ� ĳ���� �ε��� �Ľ� ����: {entry}");
                            continue;
                        }

                        int layer = (y >= 13) ? 1 : 0;
                        int localY = (layer == 1) ? y - 13 : y;

                        // ĳ���� �ε��� ��������
                        NetworkConnector.Instance.CurrentUserCharacterIndices[playerId] = charIndex;

                        CharacterSystem.Instance.SpawnCharacterAt(playerId, charIndex, x, localY, layer);
                    }

                    break;
                }
            case "CHAR_INFO":
                {
                    if (parts.Length < 2)
                    {
                        Debug.LogWarning("CHAR_INFO �޽��� �Ľ� ����");
                        break;
                    }

                    // parts[1], parts[2], ..., parts[n] ��� ���� �� ���� �÷��̾� ������
                    for (int i = 1; i < parts.Length; i++)
                    {
                        string playerInfo = parts[i];

                        if (string.IsNullOrEmpty(playerInfo))
                            continue;

                        string[] tokens = playerInfo.Split(',');
                        if (tokens.Length != 4)
                        {
                            Debug.LogWarning($"�߸��� CHAR_INFO ������ ����: {playerInfo}");
                            continue;
                        }

                        string playerId = tokens[0];
                        if (!int.TryParse(tokens[1], out int charIndex) ||
                            !int.TryParse(tokens[2], out int health) ||
                            !int.TryParse(tokens[3], out int attack))
                        {
                            Debug.LogWarning($"CHAR_INFO �Ľ� ����: {playerInfo}");
                            continue;
                        }

                        Debug.Log($"CHAR_INFO - �÷��̾�: {playerId}, ĳ���� �ε���: {charIndex}, ü��: {health}");

                        GameSystem.Instance.CreateUserInfoUI(playerId, charIndex, health);
                    }

                    break;
                }
            case "BALLOON_LIST":
                {
                    BalloonSystem.Instance.HandleBalloonMessage(message);
                    break;
                }
            case "EMO_LIST":
                {
                    EmoticonSystem.Instance.HandleEmoticonMessage(message);
                    break;
                }
            case "EMO_CLICK":
                {
                    // �޽��� ����: EMO_CLICK|nickname|3
                    string[] parts7 = message.Split('|');
                    if (parts7.Length < 3) break;

                    string senderNickname = parts7[1];
                    if (!int.TryParse(parts7[2], out int emoIndex)) break;

                    EmoticonSystem.Instance.ShowUserEmoticon(senderNickname, emoIndex);
                    break;
                }

            case "MOVE_RESULT":
                {
                    GameSystem gameMoveSystem = FindObjectOfType<GameSystem>();
                    if (gameMoveSystem != null)
                    {
                        gameMoveSystem.HandleMoveResult(message);
                    }
                    break;
                }

            case "WEAPON_ATTACK":
                {
                    if (parts.Length < 5)
                    {
                        Debug.LogWarning("[WEAPON_ATTACK] �޽��� ���� ����");
                        break;
                    }

                    string attackerNick = parts[1];
                    int charIndex = int.Parse(parts[2]);

                    // ��ġ �Ľ�
                    string[] posTokens = parts[3].Split(',');
                    if (posTokens.Length != 3)
                    {
                        Debug.LogWarning("[WEAPON_ATTACK] ��ġ �Ľ� ����");
                        break;
                    }
                    Vector3 attackPos = new Vector3(
                        float.Parse(posTokens[0]),
                        float.Parse(posTokens[1]),
                        float.Parse(posTokens[2])
                    );

                    // ȸ�� Y �Ľ�
                    float rotY = float.Parse(parts[4]);
                    Quaternion attackRot = Quaternion.Euler(0f, rotY, 0f);

                    if (charIndex == 6) // ������ ������ ���
                    {
                        if (parts.Length < 6)
                        {
                            Debug.LogWarning("[WEAPON_ATTACK] ������ ���� �޽����� ���� ������ ����");
                            break;
                        }
                        float laserLength = float.Parse(parts[5]);

                        WeaponSystem.Instance.HandleRemoteLaserAttack(attackerNick, charIndex, attackPos, attackRot, laserLength);
                        Debug.Log($"[WEAPON_ATTACK] {attackerNick} ĳ���� {charIndex} ������ ���� ���� ������ (����: {laserLength})");
                    }
                    else // �Ϲ� ����
                    {
                        WeaponSystem.Instance.HandleRemoteWeaponAttack(attackerNick, charIndex, attackPos, attackRot);
                        Debug.Log($"[WEAPON_ATTACK] {attackerNick} ĳ���� {charIndex} ���� ���� ������");
                    }

                    break;
                }
            case "DAMAGE":
                {
                    string targetNick = parts[1];
                    int damage = int.Parse(parts[2]);

                    GameSystem.Instance.DamagePlayer(targetNick, damage);
                    break;
                }
            case "MELODY_MOVE":
                {
                    if (parts.Length < 4)
                    {
                        Debug.LogWarning("[MELODY_MOVE] �޽��� ���� ����");
                        break;
                    }

                    string attackerNick = parts[1];

                    // ��ġ �Ľ�
                    string[] posTokens = parts[2].Split(',');
                    if (posTokens.Length != 3)
                    {
                        Debug.LogWarning("[MELODY_MOVE] ��ġ �Ľ� ����");
                        break;
                    }

                    Vector3 pos = new Vector3(
                        float.Parse(posTokens[0]),
                        float.Parse(posTokens[1]),
                        float.Parse(posTokens[2])
                    );

                    // ȸ�� Y �Ľ�
                    float rotY = float.Parse(parts[3]);
                    Quaternion rot = Quaternion.Euler(0f, rotY, 0f);

                    // Melody ������Ʈ ��ġ ����
                    GameObject melodyObj = GameObject.Find($"{attackerNick}_Melody");
                    if (melodyObj != null)
                    {
                        melodyObj.transform.position = pos;
                        melodyObj.transform.rotation = rot;
                        // Debug.Log($"[MELODY_MOVE] {attackerNick}�� Melody ��ġ ����");
                    }
                    else
                    {
                        Debug.LogWarning($"[MELODY_MOVE] Melody ������Ʈ�� ã�� �� ����: {attackerNick}_Melody");
                    }

                    break;
                }
            case "MELODY_DESTROY":
                {
                    string attackerNick = parts[1];
                    GameObject obj = GameObject.Find($"{attackerNick}_Melody");
                    if (obj != null)
                        Destroy(obj);

                    Debug.Log($"[Network] {attackerNick}�� Melody ������Ʈ ���ŵ�");
                    break;
                }


            case "PLACE_BALLOON_RESULT":
                {
                    BalloonSystem.Instance.HandleBalloonResult(message);
                    break;
                }
            case "REMOVE_BALLOON":
                {
                    BalloonSystem.Instance.HandleRemoveBalloon(message);
                    break;
                }
            case "WATER_SPREAD":
                {
                    BalloonSystem.Instance.HandleWaterSpread(message);
                    break;

                }
            case "PLAYER_HIT":
                {
                    // ���� �޽���: PLAYER_HIT|nickname|10
                    string[] parts1 = message.Split('|');
                    if (parts1.Length < 3)
                    {
                        Debug.LogWarning("[GameSystem] PLAYER_HIT �޽��� ���� ����");
                        break;
                    }

                    string hitPlayer = parts1[1];
                    if (!int.TryParse(parts1[2], out int damage))
                    {
                        Debug.LogWarning("[GameSystem] PLAYER_HIT ������ �Ľ� ����");
                        break;
                    }

                    GameSystem.Instance.DamagePlayer(hitPlayer, damage);
                    Debug.Log($"[GameSystem] {hitPlayer}�� {damage}��ŭ ���ظ� �Ծ����ϴ�");
                    break;
                }
            case "PLAYER_DEAD":
                {
                    string nickname = parts[1];
                    Debug.Log($"[Game] �÷��̾� ��� ó��: {nickname}");

                    GameSystem.Instance.HandlePlayerDeath(nickname);
                    break;
                }
            case "WIN":
                {
                    string winnerNickname = parts[1].Trim();
                    Debug.Log($"[Game] �¸���: {winnerNickname}");
                    GameSystem.Instance.SetWinner(winnerNickname);
                    GameSystem.Instance.HandleGameResult(winnerNickname);
                    break;
                }
            case "REWARD_RESULT":
                {
                    string[] rewards = message.Substring("REWARD_RESULT|".Length).Split('|');

                    // ������ ���� �޽����� ������ Dictionary
                    Dictionary<string, RewardData> rewardMap = new Dictionary<string, RewardData>();

                    foreach (string reward in rewards)
                    {
                        string[] resultToken = reward.Split(',');
                        string nick = resultToken[0];

                        int level = 1, exp = 0, coin0 = 0, coin1 = 0;

                        foreach (string kvStr in resultToken.Skip(1))
                        {
                            var kv = kvStr.Split(':');
                            if (kv.Length != 2) continue;

                            switch (kv[0])
                            {
                                case "level": level = int.Parse(kv[1]); break;
                                case "exp": exp = int.Parse(kv[1]); break;
                                case "money0": coin0 = int.Parse(kv[1]); break;
                                case "money1": coin1 = int.Parse(kv[1]); break;
                            }
                        }

                        rewardMap[nick] = new RewardData(level, exp, coin0, coin1);
                    }

                    // GameSystem�� rewardMap ����
                    GameSystem.Instance.SetRewardMap(rewardMap);

                    // ��� �г� ����
                    GameSystem.Instance.OpenResultPanel();

                    break;
                }
            case "READY_TO_EXIT":
                {
                    GameSystem.Instance.HandleReadyToExitMessage(message);
                    break;
                }
            case "GAME_END":
                {
                    HandleGameEndAsync();
                    break;
                }
            default:
                Debug.LogWarning("�� �� ���� ���� ���: " + command);
                break;
        }
    }

    private async void HandleGameEndAsync()
    {
        await Task.Delay(1000); // 1�� ���
        UnityEngine.SceneManagement.SceneManager.LoadScene("RoomScene");
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

    public void SetUserCharacterIndices(Dictionary<string, int> dict)
    {
        userCharacterEntries = dict.Select(pair => new UserCharacterEntry
        {
            nickname = pair.Key,
            characterIndex = pair.Value
        }).ToList();
    }

    public void SetOrUpdateUserCharacter(string nickname, int characterIndex)
    {
        var entry = userCharacterEntries.Find(e => e.nickname == nickname);
        if (entry != null)
        {
            entry.characterIndex = characterIndex;
        }
        else
        {
            userCharacterEntries.Add(new UserCharacterEntry() { nickname = nickname, characterIndex = characterIndex });
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
