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
using System.Collections;
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

    public bool EnterPacketApplied { get; set; }

    public string UserNickname { get => userNickname; set => userNickname = value; }
    public string SelectedMap { get => selectedMap; set => selectedMap = value; }
    public string CurrentRoomName { get => currentRoomName; set => currentRoomName = value; }
    public string CurrentRoomLeader { get => currentRoomLeader; set => currentRoomLeader = value; }
    public string PendingRoomEnterMessage { get => pendingRoomEnterMessage; set => pendingRoomEnterMessage = value; }

    public Dictionary<string, int> lastCharacterSnapshot { get; private set; } = new();

    private static string s_LastMapName;
    private static string s_LastMapRaw;

    private static bool s_ForceReloadMapNextTime = false;

    //게임 시작 전 캐릭터 인덱스 정보를 저장
    public void CacheCharacterIndicesBeforeGame()
    {
        lastCharacterSnapshot = new Dictionary<string, int>(CurrentUserCharacterIndices);
    }
    public void RestoreCharacterIndicesAfterGame()
    {
        if (lastCharacterSnapshot != null && lastCharacterSnapshot.Count > 0)
        {
            SetUserCharacterIndices(lastCharacterSnapshot);
        }
    }
    private static void PrepareNewRound()
    {
        // 다음 MAP_DATA에서 동일 페이로드라도 반드시 한 번은 LoadMap 하도록 플래그
        s_ForceReloadMapNextTime = true;

        // 이전 라운드 캐시 초기화
        s_SpawnedThisMap.Clear();
        s_LastMapName = null;
        s_LastMapRaw = null;
    }

    private void OnEnable() { SceneManager.sceneLoaded += OnAnySceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= OnAnySceneLoaded; }

    private void OnAnySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GameScene")
            PrepareNewRound();  // 게임씬 들어오면 항상 새 라운드로 간주
    }

    private static readonly HashSet<string> s_SpawnedThisMap = new();

    public List<string> CurrentUserList { get => currentUserList; set => currentUserList = value; }
    public Dictionary<string, int> CurrentUserCharacterIndices =>
        userCharacterEntries.ToDictionary(e => e.nickname, e => e.characterIndex);

    public bool IsCoopMode { get; set; }
    public Dictionary<string, string> UserTeams { get; } = new();

    // 서버에서 온 메시지의 명령어를 키로 하고 해당 명령어를 처리할 객체를 값으로 저장하는 딕셔너리
    private Dictionary<string, List<IMessageHandler>> handlers = new();


    // 로그인 및 회원가입용 핸들러 등록 함수
    // 외부 사용
    public void RegisterHandler(string command, IMessageHandler handler) => AddHandler(command, handler);
    public void LobbyHandler(string command, IMessageHandler handler) => AddHandler(command, handler);
    public void RoomHandler(string command, IMessageHandler handler) => AddHandler(command, handler);

    public void MyPageHandler(string command, IMessageHandler handler) => AddHandler(command, handler);

    private void AddHandler(string command, IMessageHandler handler)
    {
        if (!handlers.ContainsKey(command))
            handlers[command] = new List<IMessageHandler>();

        if (!handlers[command].Contains(handler))
            handlers[command].Add(handler);
    }
    public void RemoveHandler(string command, IMessageHandler handler)
    {
        if (handlers.TryGetValue(command, out var list))
        {
            list.Remove(handler);
            if (list.Count == 0)
                handlers.Remove(command);
        }
    }

    public void RemoveLobbyHandler(string command, IMessageHandler handler) => RemoveHandler(command, handler);
    public void RemoveRoomHandler(string command, IMessageHandler handler) => RemoveHandler(command, handler);

    public void RemoveMyPageHandler(string command, IMessageHandler handler) => RemoveHandler(command, handler);


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

        // 핸들러 등록된게 있으면 해당 핸들러 호츨
        if (handlers.TryGetValue(command, out var handlerList))
        {
            foreach (var handler in handlerList.ToList())
            {
                handler?.HandleMessage(message); 
            }
            return;
        }

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
            case "START_GAME_SUCCESS":
                Debug.Log("게임 시작 조건 충족! 씬 전환...");
                PrepareNewRound();
                CacheCharacterIndicesBeforeGame();
                UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
                break;
            case "START_GAME_FAIL":
                Debug.LogWarning("게임 시작 실패: " + message);
                FindObjectOfType<RoomSender>()?.ResetStartGameLock();
                break;
            case "GAME_START":
                Debug.Log("게임 시작 메시지 수신, 씬 전환");
                PrepareNewRound();
                CacheCharacterIndicesBeforeGame();
                SceneManager.LoadScene("GameScene");
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
                    bool samePayload = (s_LastMapName == mapName) && (s_LastMapRaw == mapRawData);

                    bool mustReload = !samePayload || s_ForceReloadMapNextTime;
                    if (mustReload)
                    {
                        Debug.Log($"[MAP_DATA] 새 라운드/갱신 로드 → {mapName}");
                        MapSystem.Instance.LoadMap(mapName, mapRawData);
                        s_LastMapName = mapName;
                        s_LastMapRaw = mapRawData;
                        s_SpawnedThisMap.Clear();    // 스폰 기록 리셋
                        s_ForceReloadMapNextTime = false; // 강제 로드 1회만
                    }
                    else
                    {
                        Debug.Log("[MAP_DATA] 동일 페이로드 재수신 → LoadMap 스킵");
                    }

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

                        // 이미 스폰한 플레이어는 스킵
                        if (!s_SpawnedThisMap.Add(playerId))
                        {
                            Debug.Log($"[MAP_DATA] 이미 스폰된 플레이어: {playerId} → 스킵");
                            continue;
                        }

                        // 캐릭터 인덱스 저장 (원하시면 이미 있으면 갱신하지 않도록 조건 추가 가능)
                        NetworkConnector.Instance.SetOrUpdateUserCharacter(playerId, charIndex);

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
            //case "WEAPON_ATTACK":
            //    {
            //        if (parts.Length < 5)
            //        {
            //            Debug.LogWarning("[WEAPON_ATTACK] 메시지 포맷 오류");
            //            break;
            //        }

            //        string attackerNick = parts[1];
            //        int charIndex = int.Parse(parts[2]);

            //        // 위치 파싱
            //        string[] posTokens = parts[3].Split(',');
            //        Vector3 attackPos = new Vector3(
            //            float.Parse(posTokens[0]),
            //            float.Parse(posTokens[1]),
            //            float.Parse(posTokens[2])
            //        );

            //        float rotY = float.Parse(parts[4]);
            //        Quaternion attackRot = Quaternion.Euler(0f, rotY, 0f);

            //        GameObject attackerObj = GameObject.Find($"Character_{attackerNick}");
            //        if (attackerObj != null)
            //        {
            //            Animator anim = attackerObj.GetComponent<Animator>();
            //            if (anim != null)
            //            {
            //                anim.SetTrigger("isAttack");
            //            }
            //        }

            //        if (charIndex == 6)
            //        {
            //            float laserLength = float.Parse(parts[5]);
            //            WeaponSystem.Instance.HandleRemoteLaserAttack(
            //                attackerNick, charIndex, attackPos, attackRot, laserLength
            //            );
            //        }
            //        else
            //        {
            //            WeaponSystem.Instance.HandleRemoteWeaponAttack(
            //                attackerNick, charIndex, attackPos, attackRot
            //            );
            //        }

            //        break;
            //    }
            case "WEAPON_ATTACK":
                {
                    if (parts.Length < 5) break;

                    string attackerNick = parts[1];
                    int charIndex = int.Parse(parts[2]);

                    string[] posTokens = parts[3].Split(',');
                    Vector3 attackPos = new Vector3(
                        float.Parse(posTokens[0]),
                        float.Parse(posTokens[1]),
                        float.Parse(posTokens[2])
                    );

                    float rotY = float.Parse(parts[4]);
                    Quaternion attackRot = Quaternion.Euler(0f, rotY, 0f);

                    float extra = -1f;
                    if (charIndex == 6 && parts.Length >= 6)
                        extra = float.Parse(parts[5]);

                    WeaponSystem.Instance.CachePendingAttack(
                        attackerNick,
                        charIndex,
                        attackPos,
                        attackRot,
                        extra
                    );

                    GameObject attackerObj = GameObject.Find($"Character_{attackerNick}");

                    Debug.Log($"[WEAPON_ATTACK] attackerNick=<{attackerNick}> " +
                                $"findName=<Character_{attackerNick}> " +
                                $"found={(attackerObj != null)} " +
                                $"objName={(attackerObj != null ? attackerObj.name : "NULL")} ");
                    if (attackerObj != null)
                    {
                        Animator anim = attackerObj.GetComponent<Animator>();
                        Debug.Log($"[WEAPON_ATTACK] Animator on root={(anim != null)} " + $"rootHasAnimator={(attackerObj.GetComponent<Animator>() != null)} " + $"childHasAnimator={(attackerObj.GetComponentInChildren<Animator>(true) != null)}");
                        if (anim != null)
                        {
                            anim.SetTrigger("isAttack");
                        }
                        LocalPlayerController lpc =
                            attackerObj.GetComponent<LocalPlayerController>();
                        if (lpc != null)
                        {
                            lpc.InvokeSpawnFallbackFromNetwork(attackerNick, 2.0f);
                        }
                    }
                    else
                    {
                        Debug.LogError($"[WEAPON_ATTACK] attackerObj NULL -> 즉시 스폰 발생! attackerNick={attackerNick}");
                        WeaponSystem.Instance.SpawnCachedWeaponIfExists(attackerNick);
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
            case "MELODY_SPAWN":
                {
                    string attackerNick = parts[1];

                    string[] pos = parts[2].Split(',');
                    Vector3 spawnPos = new Vector3(
                        float.Parse(pos[0]),
                        float.Parse(pos[1]),
                        float.Parse(pos[2])
                    );

                    float rotY = float.Parse(parts[3]);
                    Quaternion rot = Quaternion.Euler(0, rotY, 0);

                    WeaponSystem.Instance.SpawnMelodyFromServer(
                        attackerNick, spawnPos, rot
                    );
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
            case "DESTROY_SPELL":
                {
                    string[] messageParts = message.Split('|');

                    if (messageParts.Length < 2)
                    {
                        Debug.LogWarning("[DESTROY_SPELL] 메시지 형식 오류: " + message);
                        break;
                    }

                    string spellName = messageParts[1].Trim();

                    GameObject spellObj = GameObject.Find(spellName);
                    if (spellObj != null)
                    {
                        Destroy(spellObj);
                        Debug.Log($"[Spell] 스펠 제거: {spellName}");
                    }

                    break;
                }
            case "DESTROY_BLOCK":
                MapSystem.Instance.HandleDestroyBlockMessage(message);
                break;

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
            case "TEAM_WIN":
                {
                    // 원문: TEAM_WIN|Blue,a,c
                    var part = message.Split('|');
                    if (part.Length < 2) break;

                    var tokens = part[1].Split(',');
                    var team = tokens[0].Trim();  // "Blue" 또는 "Red"

                    GameSystem.Instance.SetWinnerTeam(team);
                    GameSystem.Instance.OpenResultPanel();      // ← 팀 설정 후에 열기
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
            default:
                Debug.LogWarning("알 수 없는 서버 명령: " + command);
                break;
        }
    }

    private async void HandleGameEndAsync()
    {
        await Task.Delay(1000); // 1초 대기
        UnityEngine.SceneManagement.SceneManager.LoadScene("RoomScene");
    }

    private void OnRoomSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "RoomScene")
        {
            if (lastCharacterSnapshot != null && lastCharacterSnapshot.Count > 0)
            {
                SetUserCharacterIndices(lastCharacterSnapshot);
            }

            if (!string.IsNullOrEmpty(PendingRoomEnterMessage))
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

                            FindObjectOfType<RoomSender>()?.SendGetCharacterInfo(nickname);

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
                PendingRoomEnterMessage = null;

                
            }
            else
            {
                Debug.LogWarning("RoomSystem을 찾을 수 없거나 PendingRoomEnterMessage가 비어 있음");
            }

            FindObjectOfType<RoomSender>()?.ResetStartGameLock();

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
            Debug.LogError("소켓 닫기 실패: " + e.Message);
        }
    }
}
