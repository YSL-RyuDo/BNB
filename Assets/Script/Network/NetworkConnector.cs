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
<<<<<<< Updated upstream
=======
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
            case "START_GAME_SUCCESS":
                Debug.Log("게임 시작 조건 충족! 씬 전환...");
                UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
                break;
            case "START_GAME_FAIL":
                Debug.LogWarning("게임 시작 실패: " + message);
                break;
            case "GAME_START":
                Debug.Log("게임 시작 메시지 수신, 씬 전환");
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
                        Debug.LogWarning("MAP_DATA 메시지 파싱 실패");
                        break;
                    }

                    string mapName = parts[1];
                    string mapRawData = parts[2];
                    string spawnRawData = parts[3];

                    Debug.Log($"[MAP_DATA 수신] mapName: {mapName}");

                    MapSystem.Instance.LoadMap(mapName, mapRawData);

                    string[] spawnEntries = spawnRawData.Split(',');

                    foreach (var entry in spawnEntries)
                    {
                        if (string.IsNullOrEmpty(entry))
                            continue;

                        string[] tokens = entry.Split(':');
                        if (tokens.Length != 4)
                        {
                            Debug.LogWarning($"잘못된 스폰 데이터 형식: {entry}");
                            continue;
                        }

                        string playerId = tokens[0];

                        if (!int.TryParse(tokens[1], out int x) ||
                            !int.TryParse(tokens[2], out int y) ||
                            !int.TryParse(tokens[3], out int charIndex))
                        {
                            Debug.LogWarning($"좌표 또는 캐릭터 인덱스 파싱 실패: {entry}");
                            continue;
                        }

                        int layer = (y >= 13) ? 1 : 0;
                        int localY = (layer == 1) ? y - 13 : y;

                        // 캐릭터 인덱스 가져오기
                        NetworkConnector.Instance.CurrentUserCharacterIndices[playerId] = charIndex;

                        CharacterSystem.Instance.SpawnCharacterAt(playerId, charIndex, x, localY, layer);
                    }

                    break;
                }
            case "CHAR_INFO":
                {
                    if (parts.Length < 2)
                    {
                        Debug.LogWarning("CHAR_INFO 메시지 파싱 실패");
                        break;
                    }

                    // parts[1], parts[2], ..., parts[n] 모두 각각 한 명의 플레이어 정보임
                    for (int i = 1; i < parts.Length; i++)
                    {
                        string playerInfo = parts[i];

                        if (string.IsNullOrEmpty(playerInfo))
                            continue;

                        string[] tokens = playerInfo.Split(',');
                        if (tokens.Length != 4)
                        {
                            Debug.LogWarning($"잘못된 CHAR_INFO 데이터 형식: {playerInfo}");
                            continue;
                        }

                        string playerId = tokens[0];
                        if (!int.TryParse(tokens[1], out int charIndex) ||
                            !int.TryParse(tokens[2], out int health) ||
                            !int.TryParse(tokens[3], out int attack))
                        {
                            Debug.LogWarning($"CHAR_INFO 파싱 실패: {playerInfo}");
                            continue;
                        }

                        Debug.Log($"CHAR_INFO - 플레이어: {playerId}, 캐릭터 인덱스: {charIndex}, 체력: {health}");

                        GameSystem.Instance.CreateUserInfoUI(playerId, charIndex, health);
                    }

                    GameSystem.Instance.ApplyTeamLayout();
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
                    // 메시지 예시: EMO_CLICK|nickname|3
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
                        Debug.LogWarning("[WEAPON_ATTACK] 메시지 포맷 오류");
                        break;
                    }

                    string attackerNick = parts[1];
                    int charIndex = int.Parse(parts[2]);

                    // 위치 파싱
                    string[] posTokens = parts[3].Split(',');
                    if (posTokens.Length != 3)
                    {
                        Debug.LogWarning("[WEAPON_ATTACK] 위치 파싱 오류");
                        break;
                    }
                    Vector3 attackPos = new Vector3(
                        float.Parse(posTokens[0]),
                        float.Parse(posTokens[1]),
                        float.Parse(posTokens[2])
                    );

                    // 회전 Y 파싱
                    float rotY = float.Parse(parts[4]);
                    Quaternion attackRot = Quaternion.Euler(0f, rotY, 0f);

                    if (charIndex == 6) // 레이저 무기인 경우
                    {
                        if (parts.Length < 6)
                        {
                            Debug.LogWarning("[WEAPON_ATTACK] 레이저 공격 메시지에 길이 정보가 없음");
                            break;
                        }
                        float laserLength = float.Parse(parts[5]);

                        WeaponSystem.Instance.HandleRemoteLaserAttack(attackerNick, charIndex, attackPos, attackRot, laserLength);
                        Debug.Log($"[WEAPON_ATTACK] {attackerNick} 캐릭터 {charIndex} 레이저 무기 공격 생성됨 (길이: {laserLength})");
                    }
                    else // 일반 무기
                    {
                        WeaponSystem.Instance.HandleRemoteWeaponAttack(attackerNick, charIndex, attackPos, attackRot);
                        Debug.Log($"[WEAPON_ATTACK] {attackerNick} 캐릭터 {charIndex} 무기 공격 생성됨");
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
                        Debug.LogWarning("[MELODY_MOVE] 메시지 포맷 오류");
                        break;
                    }

                    string attackerNick = parts[1];

                    // 위치 파싱
                    string[] posTokens = parts[2].Split(',');
                    if (posTokens.Length != 3)
                    {
                        Debug.LogWarning("[MELODY_MOVE] 위치 파싱 오류");
                        break;
                    }

                    Vector3 pos = new Vector3(
                        float.Parse(posTokens[0]),
                        float.Parse(posTokens[1]),
                        float.Parse(posTokens[2])
                    );

                    // 회전 Y 파싱
                    float rotY = float.Parse(parts[3]);
                    Quaternion rot = Quaternion.Euler(0f, rotY, 0f);

                    // Melody 오브젝트 위치 갱신
                    GameObject melodyObj = GameObject.Find($"{attackerNick}_Melody");
                    if (melodyObj != null)
                    {
                        melodyObj.transform.position = pos;
                        melodyObj.transform.rotation = rot;
                        // Debug.Log($"[MELODY_MOVE] {attackerNick}의 Melody 위치 갱신");
                    }
                    else
                    {
                        Debug.LogWarning($"[MELODY_MOVE] Melody 오브젝트를 찾을 수 없음: {attackerNick}_Melody");
                    }

                    break;
                }
            case "MELODY_DESTROY":
                {
                    string attackerNick = parts[1];
                    GameObject obj = GameObject.Find($"{attackerNick}_Melody");
                    if (obj != null)
                        Destroy(obj);

                    Debug.Log($"[Network] {attackerNick}의 Melody 오브젝트 제거됨");
                    break;
                }
            case "DESTROYWALL":
                {
                    string[] messageParts = message.Split('|');

                    if (messageParts.Length < 2)
                    {
                        Debug.LogWarning("[DESTROYWALL] 메시지 형식 오류: " + message);
                        break;
                    }

                    string wallName = messageParts[1].Trim();

                    GameObject wall = GameObject.Find(wallName);
                    if (wall != null)
                    {
                        Destroy(wall);
                        Debug.Log($"[Map] 벽 파괴: {wallName}");
                    }

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
                    // 예시 메시지: PLAYER_HIT|nickname|10
                    string[] parts1 = message.Split('|');
                    if (parts1.Length < 3)
                    {
                        Debug.LogWarning("[GameSystem] PLAYER_HIT 메시지 형식 오류");
                        break;
                    }

                    string hitPlayer = parts1[1];
                    if (!int.TryParse(parts1[2], out int damage))
                    {
                        Debug.LogWarning("[GameSystem] PLAYER_HIT 데미지 파싱 실패");
                        break;
                    }

                    GameSystem.Instance.DamagePlayer(hitPlayer, damage);
                    Debug.Log($"[GameSystem] {hitPlayer}가 {damage}만큼 피해를 입었습니다");
                    break;
                }
            case "PLAYER_DEAD":
                {
                    string nickname = parts[1];
                    Debug.Log($"[Game] 플레이어 사망 처리: {nickname}");

                    GameSystem.Instance.HandlePlayerDeath(nickname);
                    break;
                }
            case "WIN":
                {
                    string winnerNickname = parts[1].Trim();
                    Debug.Log($"[Game] 승리자: {winnerNickname}");
                    GameSystem.Instance.SetWinner(winnerNickname);
                    GameSystem.Instance.HandleGameResult(winnerNickname);
                    break;
                }
            case "REWARD_RESULT":
                {
                    string[] rewards = message.Substring("REWARD_RESULT|".Length).Split('|');

                    // 유저별 보상 메시지를 저장할 Dictionary
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

                    // GameSystem에 rewardMap 전달
                    GameSystem.Instance.SetRewardMap(rewardMap);

                    // 결과 패널 열기
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
>>>>>>> Stashed changes
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
