using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

//using UnityEngine.UIElements;


public class GameSystem : MonoBehaviour
{
    public static GameSystem Instance;

    public CharacterSystem playerManager;
    public Transform userInfoContent;
    public GameObject userInfo;
    private HashSet<string> deadPlayers = new HashSet<string>(); // 중복 방지용

    public GameObject gameResultPanel;           // 인스펙터에서 할당
    public GameObject coopGameResultPanel;
    public TextMeshProUGUI winnerText;           // WinnerText 오브젝트
    public TextMeshProUGUI coopWinnerText;           // WinnerText 오브젝트
    public GameObject userResultPrefab;          // UserResult 프리팹
    public Transform userResultParent;
    public Transform coopUserResultParent;
    public Button lobbyButton;
    public Button coopLobbyButton;
    private string winnerNickname = "";
    private string winnerTeam = "";
    private Dictionary<string, RewardData> rewardMap = new Dictionary<string, RewardData>();
    private Dictionary<string, float> lastHitTimes = new Dictionary<string, float>(); // 닉네임 → 마지막 피격 시간
    private float hitCooldown = 1.0f; // 무적 시간 (초)

    private Color blueTeamColor = Color.blue;
    private Color redTeamColor = Color.red;
    private Color soloColor = Color.black;

    private static bool startRanOnce = false;
    private static bool mapRequestedOnce = false;

    private readonly Dictionary<string, Vector3> lastPosMap = new();
    [SerializeField] private float walkThreshold = 0.001f;
    private readonly Dictionary<string, float> lastMoveRecvTime = new();
    [SerializeField] private float stopTimeout = 0.1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;


    }

    async void Start()
    {
        if (startRanOnce) return;
        startRanOnce = true;

        //
        gameResultPanel.SetActive(false);
        string nickName = NetworkConnector.Instance.UserNickname;
        string currentRoomLeader = NetworkConnector.Instance.CurrentRoomLeader;
        string roomName = NetworkConnector.Instance.CurrentRoomName;
        string selectedMap = NetworkConnector.Instance.SelectedMap;

        if (nickName == currentRoomLeader && !mapRequestedOnce)
        {
            mapRequestedOnce = true;
            string getMapMsg = $"GET_MAP|{roomName}|{selectedMap}\n";
            byte[] getMapBytes = Encoding.UTF8.GetBytes(getMapMsg);
            await NetworkConnector.Instance.Stream.WriteAsync(getMapBytes, 0, getMapBytes.Length);
            Debug.Log(getMapMsg);
            Debug.Log("[GameSceneInitializer] 서버에 GET_MAP 요청 보냄");
        }

        string getEmoMsg = $"GET_EMO|{nickName}\n";
        byte[] getEmoBytes = Encoding.UTF8.GetBytes(getEmoMsg);
        await NetworkConnector.Instance.Stream.WriteAsync(getEmoBytes, 0, getEmoBytes.Length);
        Debug.Log("[GameSceneInitializer] 서버에 GET_EMO 요청 보냄");

        string getBalloonMsg = $"GET_BALLOON|{nickName}\n";
        byte[] getBalloonBytes = Encoding.UTF8.GetBytes(getBalloonMsg);
        await NetworkConnector.Instance.Stream.WriteAsync(getBalloonBytes, 0, getBalloonBytes.Length);

        lobbyButton.onClick.AddListener(() => OnLobbyButtonClicked(nickName));
        coopLobbyButton.onClick.AddListener(() => OnCoopLobbyButtonClicked(nickName));
    }

    void Update()
    {
        float now = Time.time;

        if (lastMoveRecvTime.Count == 0) return;
        var keys = new List<string>(lastMoveRecvTime.Keys);

        foreach (var username in keys)
        {
            if (!lastMoveRecvTime.TryGetValue(username, out float lastTime))
                continue;

            if (now - lastTime > stopTimeout)
            {
                GameObject obj = GameObject.Find($"Character_{username}");
                if (obj == null) continue;

                Animator anim = obj.GetComponent<Animator>();
                if (anim == null) continue;

                anim.SetBool("isWalk", false);
            }
        }
    }

    public void HandleMoveResult(string message)
    {
        // 메시지 포맷: MOVE_RESULT|username,x,z
        var parts = message.Split('|');
        if (parts.Length < 2)
        {
            Debug.LogWarning("MOVE_RESULT 메시지 파싱 실패");
            return;
        }

        string data = parts[1]; // username,x,z
        string[] subParts = data.Split(',');

        if (subParts.Length < 3)
        {
            Debug.LogWarning("MOVE_RESULT 좌표 파싱 실패");
            return;
        }

        string username = subParts[0];
        if (!float.TryParse(subParts[1], out float x) || !float.TryParse(subParts[2], out float z))
        {
            Debug.LogWarning("MOVE_RESULT 좌표 파싱 실패");
            return;
        }

        string objectName = $"Character_{username}";
        GameObject playerObj = GameObject.Find(objectName);
        if (playerObj != null)
        {
            Vector3 currentPos = playerObj.transform.position;
            Vector3 newPos = new Vector3(x, currentPos.y, z);

            Animator anim = playerObj.GetComponent<Animator>();
            if (anim != null)
            {
                lastMoveRecvTime[username] = Time.time;

                if (!lastPosMap.TryGetValue(username, out Vector3 lastPos))
                {
                    lastPosMap[username] = newPos;
                    anim.SetBool("isWalk", false);
                }
                else
                {
                    bool isWalk = (newPos - lastPos).sqrMagnitude > walkThreshold;
                    anim.SetBool("isWalk", isWalk);
                    lastPosMap[username] = newPos;
                }
            }
            else
            {
                lastPosMap[username] = newPos;
                lastMoveRecvTime[username] = Time.time;
            }

            // 이동 방향 벡터 (현재 위치에서 새 위치로)
            Vector3 direction = newPos - currentPos;

            if (direction.sqrMagnitude > 0.001f) // 방향이 유효할 때만 회전
            {
                // Y축 기준으로만 회전 (평면상 회전)
                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                playerObj.transform.rotation = targetRotation;
            }

            // 위치 갱신
            playerObj.transform.position = newPos;
        }
        else
        {
            Debug.LogWarning($"플레이어 오브젝트를 찾을 수 없음: {objectName}");
        }
    }

    public void CreateUserInfoUI(string playerId, int charIndex, int health)
    {
        if (userInfo == null || userInfoContent == null)
        {
            Debug.LogWarning("userInfo 또는 userInfoContent가 null입니다.");
            return;
        }

        if (string.IsNullOrEmpty(playerId)) return;

        playerId = playerId.Trim();

        var existing = userInfoContent.Find($"UserInfo_{playerId}");
        if (existing != null)
        {
            return;
        }

        GameObject uiObj = Instantiate(userInfo, userInfoContent);
        uiObj.name = $"UserInfo_{playerId}";
        Image characterImage = uiObj.transform.Find("CharacterImage")?.GetComponent<Image>();
        TextMeshProUGUI nameText = uiObj.transform.Find("UserNameText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI healthText = uiObj.transform.Find("UserHealthText")?.GetComponent<TextMeshProUGUI>();
        Slider userHealthBar = uiObj.transform.Find("UserHealthBar")?.GetComponent<Slider>();

        if (nameText != null)
        {
            nameText.text = playerId;


            var nc = NetworkConnector.Instance;
            if (nc != null && nc.IsCoopMode && nc.UserTeams != null)
            {
                string team;
                if (nc.UserTeams.TryGetValue(playerId, out team))
                {
                    if (team == "Blue") nameText.color = blueTeamColor;
                    else if (team == "Red") nameText.color = redTeamColor;
                    else nameText.color = soloColor;
                }
                else
                {
                    nameText.color = soloColor;
                }
            }
            else
            {
                nameText.color = soloColor;
            }
        }
        if (healthText != null)
            healthText.text = $"HP: {health}";

        if (userHealthBar != null)
        {
            userHealthBar.maxValue = health;  // 최대 체력을 셋팅 (필요시)
            userHealthBar.value = health;     // 현재 체력 표시
        }

        if (characterImage != null && CharacterSystem.Instance != null)
        {
            Sprite[] aliveImages = CharacterSystem.Instance.characterAliveImage;

            if (charIndex >= 0 && charIndex < aliveImages.Length)
            {
                characterImage.sprite = aliveImages[charIndex];
            }
            else
            {
                Debug.LogWarning($"charIndex {charIndex} 가 aliveImages 범위를 벗어남");
            }
        }
    }

    public void ApplyTeamLayout()
    {
        var nc = NetworkConnector.Instance;
        if (nc == null || userInfoContent == null) return;

        // 유저 목록(들어온 순서)과 팀 맵
        var userList = nc.CurrentUserList;       // a,b,c,d 순서
        var teamMap = nc.UserTeams;             // nickname -> "Blue"/"Red"/"None"

        if (userList == null || teamMap == null) return;

        List<string> blues = new();
        List<string> reds = new();
        List<string> none = new();

        foreach (var nick in userList)
        {
            if (!teamMap.TryGetValue(nick, out var team) || string.IsNullOrEmpty(team) || team == "None")
                none.Add(nick);
            else if (team == "Blue")
                blues.Add(nick);
            else if (team == "Red")
                reds.Add(nick);
            else
                none.Add(nick);
        }

        SetSiblingForList(reds);
        SetSiblingForList(blues);
        SetSiblingForList(none);
    }

    void SetSiblingForList(List<string> list)
    {
        int sibling = 0;
        foreach (var nick in list)
        {
            var t = userInfoContent.Find($"UserInfo_{nick}");
            if (t != null) t.SetSiblingIndex(sibling++);
        }
    }

    public void DamagePlayer(string nickname, int damage)
    {
        float now = Time.time;

        // 최근 피격 시간 확인
        if (lastHitTimes.TryGetValue(nickname, out float lastTime))
        {
            if (now - lastTime < hitCooldown)
            {
                Debug.Log($"[GameSystem] {nickname} 피격 쿨타임 중 (무시)");
                return; // 쿨타임 동안은 무시
            }
        }

        // 피격 시간 갱신
        lastHitTimes[nickname] = now;

        GameObject uiObj = GameObject.Find($"UserInfo_{nickname}");
        if (uiObj == null) return;

        TMPro.TextMeshProUGUI healthText = uiObj.transform.Find("UserHealthText")?.GetComponent<TMPro.TextMeshProUGUI>();
        UnityEngine.UI.Slider healthBar = uiObj.transform.Find("UserHealthBar")?.GetComponent<UnityEngine.UI.Slider>();

        if (healthText == null || healthBar == null) return;

        int currentHp = Mathf.RoundToInt(healthBar.value);
        int newHp = Mathf.Max(currentHp - damage, 0);
        healthBar.value = newHp;
        healthText.text = $"HP: {newHp}";

        if (newHp <= 0)
        {
            Debug.Log($"[GameSystem] {nickname} 캐릭터 체력 0, 서버에 사망 패킷 전송");
            SendDeathPacket(nickname);
        }
    }

    private async void SendDeathPacket(string nickname)
    {
        string msg = $"DEAD|{nickname}\n";
        byte[] bytes = Encoding.UTF8.GetBytes(msg);

        try
        {
            await NetworkConnector.Instance.Stream.WriteAsync(bytes, 0, bytes.Length);
            Debug.Log($"[GameSystem] 사망 패킷 전송 완료: {msg.Trim()}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GameSystem] 사망 패킷 전송 실패: {ex.Message}");
        }
    }

    public void HandlePlayerDeath(string nickname)
    {
        deadPlayers.Add(nickname);

        GameObject uiObj = GameObject.Find($"UserInfo_{nickname}");
        if (uiObj != null)
        {
            TMPro.TextMeshProUGUI healthText = uiObj.transform.Find("UserHealthText")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (healthText != null)
            {
                healthText.text = "패배";
            }

            Image characterImage = uiObj.transform.Find("CharacterImage")?.GetComponent<Image>();
            if (characterImage != null && CharacterSystem.Instance != null)
            {
                int charIndex = 0;
                if (NetworkConnector.Instance.CurrentUserCharacterIndices.TryGetValue(nickname, out int index))
                {
                    charIndex = index;
                }

                Sprite[] daathImages = CharacterSystem.Instance.characterDeathImage;

                if (charIndex >= 0 && charIndex < daathImages.Length)
                {
                    characterImage.sprite = daathImages[charIndex];
                }
                else
                {
                    Debug.LogWarning($"charIndex {charIndex} 가 aliveImages 범위를 벗어남");

                }
            }
        }

        // 캐릭터 비활성화
        GameObject target = GameObject.Find($"Character_{nickname}");
        Animator anim = target.GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetTrigger("isDeath");
        }

        if (target != null)
        {
            target.SetActive(false);
        }
    }
    public void HandleGameResult(string winnerNickname)
    {
        GameObject uiObj = GameObject.Find($"UserInfo_{winnerNickname}");
        if (uiObj != null)
        {
            TMPro.TextMeshProUGUI healthText = uiObj.transform.Find("UserHealthText")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (healthText != null)
            {
                healthText.text = "승리";
            }
        }
    }

    public void SetRewardMap(Dictionary<string, RewardData> map)
    {
        rewardMap = map;
    }

    public void SetWinner(string winner)
    {
        winnerNickname = winner;
    }

    public void OpenResultPanel()
    {
        var nc = NetworkConnector.Instance;
        bool isCoop = (nc != null && nc.IsCoopMode);

        // 패널 활성/비활성
        if (isCoop && coopGameResultPanel != null)
        {
            coopGameResultPanel.SetActive(true);
            if (gameResultPanel != null) gameResultPanel.SetActive(false);
        }
        else
        {
            if (gameResultPanel != null) gameResultPanel.SetActive(true);
            if (coopGameResultPanel != null) coopGameResultPanel.SetActive(false);
        }

        if (userResultParent != null)
            foreach (Transform child in userResultParent) Destroy(child.gameObject);
        if (coopUserResultParent != null)
            foreach (Transform child in coopUserResultParent) Destroy(child.gameObject);

        if (!isCoop)
        {
            if (winnerText != null)
                winnerText.text = $"Winner {winnerNickname}";


            foreach (string nick in nc.CurrentUserList)
            {
                GameObject obj = Instantiate(userResultPrefab, userResultParent);
                obj.name = $"UserResult_{nick}";
                var characterImage = obj.transform.Find("CharacterImage")?.GetComponent<Image>();
                var nameText = obj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
                var levelText = obj.transform.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
                var expText = obj.transform.Find("EXPText")?.GetComponent<TextMeshProUGUI>();
                var coin1Text = obj.transform.Find("Coin1Text")?.GetComponent<TextMeshProUGUI>();
                var coin2Text = obj.transform.Find("Coin2Text")?.GetComponent<TextMeshProUGUI>();
                var lobbyToggle = obj.transform.Find("LobbyToggle")?.GetComponent<Toggle>();

                if (characterImage != null && CharacterSystem.Instance != null)
                {
                    int charIndex = 0;
                    nc.CurrentUserCharacterIndices.TryGetValue(nick, out charIndex);

                    bool isWinner = (nick == winnerNickname);
                    var imgs = isWinner ? CharacterSystem.Instance.characterWinImage
                                        : CharacterSystem.Instance.characterDeathImage;

                    if (charIndex >= 0 && charIndex < imgs.Length)
                        characterImage.sprite = imgs[charIndex];
                    else
                        Debug.LogWarning($"[결과 이미지] 범위 초과: {charIndex}");
                }

                if (nameText != null)
                {
                    nameText.text = nick;
                    nameText.color = (nick == winnerNickname) ? Color.yellow : Color.black;
                }

                if (rewardMap != null && rewardMap.TryGetValue(nick, out RewardData data))
                {
                    if (levelText != null) levelText.text = $"Lv {data.level}";
                    if (expText != null) expText.text = $"EXP {data.exp}";
                    if (coin1Text != null) coin1Text.text = $"Coins {data.coin0:N0}";
                    if (coin2Text != null) coin2Text.text = $"Medals {data.coin1:N0}";
                }
                else
                {
                    if (levelText != null) levelText.text = "Lv 1";
                    if (expText != null) expText.text = "EXP 0";
                    if (coin1Text != null) coin1Text.text = "Coins 0";
                    if (coin2Text != null) coin2Text.text = "Medals 0";
                }

                if (lobbyToggle != null)
                {
                    lobbyToggle.isOn = false;
                    lobbyToggle.interactable = true;
                    lobbyToggle.onValueChanged.AddListener(isOn => { if (isOn) _ = SendReadyToExitAsync(nick); });
                }
            }

            return;
        }

        if (string.IsNullOrEmpty(winnerTeam))
        {
            Debug.LogWarning("[Result] 코옵인데 winnerTeam이 아직 설정되지 않았습니다. TEAM_WIN 수신 후 OpenResultPanel을 다시 호출해야 합니다.");
            return; 
        }

        if (coopWinnerText != null)
        {
            coopWinnerText.text = $"Winner {winnerTeam}";
            if (winnerTeam == "Blue") coopWinnerText.color = blueTeamColor;
            else if (winnerTeam == "Red") coopWinnerText.color = redTeamColor;
            else coopWinnerText.color = soloColor;
        }

        var teamMap = nc.UserTeams;
        List<string> winners = new();
        List<string> losers = new();

        foreach (var nick in nc.CurrentUserList)
        {
            if (teamMap != null && teamMap.TryGetValue(nick, out var t) && t == winnerTeam) winners.Add(nick);
            else losers.Add(nick);
        }

        void CreateEntry(string nick, bool isWinnerSide)
        {
            GameObject obj = Instantiate(userResultPrefab, coopUserResultParent);
            obj.name = $"UserResult_{nick}";
            var characterImage = obj.transform.Find("CharacterImage")?.GetComponent<Image>();
            var nameText = obj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var levelText = obj.transform.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
            var expText = obj.transform.Find("EXPText")?.GetComponent<TextMeshProUGUI>();
            var coin1Text = obj.transform.Find("Coin1Text")?.GetComponent<TextMeshProUGUI>();
            var coin2Text = obj.transform.Find("Coin2Text")?.GetComponent<TextMeshProUGUI>();
            var lobbyToggle = obj.transform.Find("LobbyToggle")?.GetComponent<Toggle>();

            if (characterImage != null && CharacterSystem.Instance != null)
            {
                int charIndex = 0; nc.CurrentUserCharacterIndices.TryGetValue(nick, out charIndex);
                var imgs = isWinnerSide ? CharacterSystem.Instance.characterWinImage
                                        : CharacterSystem.Instance.characterDeathImage;
                if (charIndex >= 0 && charIndex < imgs.Length) characterImage.sprite = imgs[charIndex];
            }

            if (nameText != null)
            {
                nameText.text = nick;
                nameText.color = isWinnerSide ? Color.yellow : Color.black;
            }

            if (rewardMap != null && rewardMap.TryGetValue(nick, out RewardData data))
            {
                if (levelText != null) levelText.text = $"Lv {data.level}";
                if (expText != null) expText.text = $"EXP {data.exp}";
                if (coin1Text != null) coin1Text.text = $"Coins {data.coin0:N0}";
                if (coin2Text != null) coin2Text.text = $"Medals {data.coin1:N0}";
            }
            else
            {
                if (levelText != null) levelText.text = "Lv 1";
                if (expText != null) expText.text = "EXP 0";
                if (coin1Text != null) coin1Text.text = "Coins 0";
                if (coin2Text != null) coin2Text.text = "Medals 0";
            }

            if (lobbyToggle != null)
            {
                lobbyToggle.isOn = false;
                lobbyToggle.interactable = true;
                lobbyToggle.onValueChanged.AddListener(isOn => { if (isOn) _ = SendReadyToExitAsync(nick); });
            }
        }

        foreach (var nick in winners) CreateEntry(nick, true);
        foreach (var nick in losers) CreateEntry(nick, false);

    }

    public void OnLobbyButtonClicked(string targetNickname)
    {
        string targetName = $"UserResult_{targetNickname}";
        Transform targetUserResult = userResultParent.Find(targetName);

        if (targetUserResult != null)
        {
            var toggle = targetUserResult.Find("LobbyToggle")?.GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.isOn = true;
                toggle.interactable = false;
            }
        }
        else
        {
            Debug.LogWarning($"UserResult_{targetNickname}를 찾을 수 없습니다.");
        }
    }

    public void OnCoopLobbyButtonClicked(string targetNickname)
    {
        string targetName = $"UserResult_{targetNickname}";
        Transform targetUserResult = coopUserResultParent.Find(targetName);

        if (targetUserResult != null)
        {
            var toggle = targetUserResult.Find("LobbyToggle")?.GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.isOn = true;
                toggle.interactable = false;
            }
        }
        else
        {
            Debug.LogWarning($"UserResult_{targetNickname}를 찾을 수 없습니다.");
        }
    }


    private async Task SendReadyToExitAsync(string nickname)
    {
        string readyMsg = $"READY_TO_EXIT|{nickname}\n";
        byte[] readyBytes = Encoding.UTF8.GetBytes(readyMsg);

        var stream = NetworkConnector.Instance.Stream;
        if (stream != null && stream.CanWrite)
        {
            await stream.WriteAsync(readyBytes, 0, readyBytes.Length);
        }
    }

    public void HandleReadyToExitMessage(string message)
    {
        if (this == null || gameObject == null || userResultParent == null)
        {
            Debug.Log("HandleReadyToExitMessage 호출 시 오브젝트가 이미 파괴됨");
            return;
        }

        // 메시지 포맷: READY_TO_EXIT|nickname
        var parts = message.Split('|');
        if (parts.Length < 2)
        {
            Debug.LogWarning("READY_TO_EXIT 메시지 파싱 실패");
            return;
        }

        string nickname = parts[1];

        Transform t1 = userResultParent != null ? userResultParent.Find($"UserResult_{nickname}") : null;

        Transform t2 = coopUserResultParent != null ? coopUserResultParent.Find($"UserResult_{nickname}") : null;

        Transform target = t1 != null ? t1 : t2;

        if (target != null)
        {
            Toggle lobbyToggle = target.Find("LobbyToggle")?.GetComponent<Toggle>();
            if (lobbyToggle != null)
            {
                lobbyToggle.isOn = true;
                lobbyToggle.interactable = false;
            }
        }
        else
        {
            Debug.LogWarning($"UserResult_{nickname} UI를 찾을 수 없습니다.");
        }
    }
    public void SetWinnerTeam(string team)
    {
        winnerTeam = team;
    }

    private void OnDestroy()
    {
        startRanOnce = false;
        mapRequestedOnce = false;
    }
}