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
    public TextMeshProUGUI winnerText;           // WinnerText 오브젝트
    public GameObject userResultPrefab;          // UserResult 프리팹
    public Transform userResultParent;
    public Button lobbyButton;
    private string winnerNickname = "";
    private Dictionary<string, RewardData> rewardMap = new Dictionary<string, RewardData>();
    
    private void Awake()
    {
        Instance = this;
    }

    async void Start()
    {
        gameResultPanel.SetActive(false);
        string nickName = NetworkConnector.Instance.UserNickname;
        string currentRoomLeader = NetworkConnector.Instance.CurrentRoomLeader;
        string roomName = NetworkConnector.Instance.CurrentRoomName;
        string selectedMap = NetworkConnector.Instance.SelectedMap;

        if(nickName == currentRoomLeader)
        {
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



        GameObject uiObj = Instantiate(userInfo, userInfoContent);
        uiObj.name = $"UserInfo_{playerId}";

        TMPro.TextMeshProUGUI nameText = uiObj.transform.Find("UserNameText")?.GetComponent<TMPro.TextMeshProUGUI>();
        TMPro.TextMeshProUGUI healthText = uiObj.transform.Find("UserHealthText")?.GetComponent<TMPro.TextMeshProUGUI>();
        UnityEngine.UI.Slider userHealthBar = uiObj.transform.Find("UserHealthBar")?.GetComponent<UnityEngine.UI.Slider>();

        if (nameText != null)
            nameText.text = playerId;

        if (healthText != null)
            healthText.text = $"HP: {health}";

        if (userHealthBar != null)
        {
            userHealthBar.maxValue = health;  // 최대 체력을 셋팅 (필요시)
            userHealthBar.value = health;     // 현재 체력 표시
        }
    }

    public void DamagePlayer(string nickname, int damage)
    {
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

        // 체력 텍스트를 "패배"로 변경
        GameObject uiObj = GameObject.Find($"UserInfo_{nickname}");
        if (uiObj != null)
        {
            TMPro.TextMeshProUGUI healthText = uiObj.transform.Find("UserHealthText")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (healthText != null)
            {
                healthText.text = "패배";
            }
        }

        // 캐릭터 비활성화
        GameObject target = GameObject.Find($"Character_{nickname}");
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
        gameResultPanel.SetActive(true);
        winnerText.text = $"Winner {winnerNickname}";

        foreach (Transform child in userResultParent)
        {
            Destroy(child.gameObject);
        }

        foreach (string nick in NetworkConnector.Instance.CurrentUserList)
        {
            GameObject obj = Instantiate(userResultPrefab, userResultParent);
            obj.name = $"UserResult_{nick}";

            var nameText = obj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var levelText = obj.transform.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
            var expText = obj.transform.Find("EXPText")?.GetComponent<TextMeshProUGUI>();
            var coin1Text = obj.transform.Find("Coin1Text")?.GetComponent<TextMeshProUGUI>();
            var coin2Text = obj.transform.Find("Coin2Text")?.GetComponent<TextMeshProUGUI>();
            var lobbyToggle = obj.transform.Find("LobbyToggle")?.GetComponent<Toggle>();
            var lobbyButton = obj.transform.Find("LobbyButton")?.GetComponent<Button>();
            if (nameText != null)
            {
                nameText.text = nick;
                nameText.color = (nick == winnerNickname) ? UnityEngine.Color.yellow : UnityEngine.Color.black;
            }

            if (lobbyToggle != null)
            {
                lobbyToggle.isOn = false;
                lobbyToggle.interactable = true;

                lobbyToggle.onValueChanged.AddListener(isOn =>
                {
                    if (isOn)
                    {
                        _ = SendReadyToExitAsync(nick);
                    }
                });
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
        }
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

        // userResultParent는 UserResult UI가 붙는 부모 Transform
        Transform targetUserResult = userResultParent.Find($"UserResult_{nickname}");
        if (targetUserResult != null)
        {
            Toggle lobbyToggle = targetUserResult.Find("LobbyToggle")?.GetComponent<Toggle>();
            if (lobbyToggle != null)
            {
                lobbyToggle.isOn = true;         // 토글 켜기
                lobbyToggle.interactable = false; // 유저가 직접 조작 못하게 막기
            }
        }
        else
        {
            Debug.LogWarning($"UserResult_{nickname} UI를 찾을 수 없습니다.");
        }
    }

}
