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
    private HashSet<string> deadPlayers = new HashSet<string>(); // �ߺ� ������

    public GameObject gameResultPanel;           // �ν����Ϳ��� �Ҵ�
    public TextMeshProUGUI winnerText;           // WinnerText ������Ʈ
    public GameObject userResultPrefab;          // UserResult ������
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
            Debug.Log("[GameSceneInitializer] ������ GET_MAP ��û ����");
        }

        string getEmoMsg = $"GET_EMO|{nickName}\n";
        byte[] getEmoBytes = Encoding.UTF8.GetBytes(getEmoMsg);
        await NetworkConnector.Instance.Stream.WriteAsync(getEmoBytes, 0, getEmoBytes.Length);
        Debug.Log("[GameSceneInitializer] ������ GET_EMO ��û ����");

        string getBalloonMsg = $"GET_BALLOON|{nickName}\n";
        byte[] getBalloonBytes = Encoding.UTF8.GetBytes(getBalloonMsg);
        await NetworkConnector.Instance.Stream.WriteAsync(getBalloonBytes, 0, getBalloonBytes.Length);

        lobbyButton.onClick.AddListener(() => OnLobbyButtonClicked(nickName));
    }

    public void HandleMoveResult(string message)
    {
        // �޽��� ����: MOVE_RESULT|username,x,z
        var parts = message.Split('|');
        if (parts.Length < 2)
        {
            Debug.LogWarning("MOVE_RESULT �޽��� �Ľ� ����");
            return;
        }

        string data = parts[1]; // username,x,z
        string[] subParts = data.Split(',');

        if (subParts.Length < 3)
        {
            Debug.LogWarning("MOVE_RESULT ��ǥ �Ľ� ����");
            return;
        }

        string username = subParts[0];
        if (!float.TryParse(subParts[1], out float x) || !float.TryParse(subParts[2], out float z))
        {
            Debug.LogWarning("MOVE_RESULT ��ǥ �Ľ� ����");
            return;
        }

        string objectName = $"Character_{username}";
        GameObject playerObj = GameObject.Find(objectName);
        if (playerObj != null)
        {
            Vector3 currentPos = playerObj.transform.position;
            Vector3 newPos = new Vector3(x, currentPos.y, z);

            // �̵� ���� ���� (���� ��ġ���� �� ��ġ��)
            Vector3 direction = newPos - currentPos;

            if (direction.sqrMagnitude > 0.001f) // ������ ��ȿ�� ���� ȸ��
            {
                // Y�� �������θ� ȸ�� (���� ȸ��)
                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                playerObj.transform.rotation = targetRotation;
            }

            // ��ġ ����
            playerObj.transform.position = newPos;
        }
        else
        {
            Debug.LogWarning($"�÷��̾� ������Ʈ�� ã�� �� ����: {objectName}");
        }
    }

    public void CreateUserInfoUI(string playerId, int charIndex, int health)
    {
        if (userInfo == null || userInfoContent == null)
        {
            Debug.LogWarning("userInfo �Ǵ� userInfoContent�� null�Դϴ�.");
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
            userHealthBar.maxValue = health;  // �ִ� ü���� ���� (�ʿ��)
            userHealthBar.value = health;     // ���� ü�� ǥ��
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
            Debug.Log($"[GameSystem] {nickname} ĳ���� ü�� 0, ������ ��� ��Ŷ ����");
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
            Debug.Log($"[GameSystem] ��� ��Ŷ ���� �Ϸ�: {msg.Trim()}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GameSystem] ��� ��Ŷ ���� ����: {ex.Message}");
        }
    }

    public void HandlePlayerDeath(string nickname)
    {
        deadPlayers.Add(nickname);

        // ü�� �ؽ�Ʈ�� "�й�"�� ����
        GameObject uiObj = GameObject.Find($"UserInfo_{nickname}");
        if (uiObj != null)
        {
            TMPro.TextMeshProUGUI healthText = uiObj.transform.Find("UserHealthText")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (healthText != null)
            {
                healthText.text = "�й�";
            }
        }

        // ĳ���� ��Ȱ��ȭ
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
                healthText.text = "�¸�";
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
            Debug.LogWarning($"UserResult_{targetNickname}�� ã�� �� �����ϴ�.");
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
            Debug.Log("HandleReadyToExitMessage ȣ�� �� ������Ʈ�� �̹� �ı���");
            return;
        }

        // �޽��� ����: READY_TO_EXIT|nickname
        var parts = message.Split('|');
        if (parts.Length < 2)
        {
            Debug.LogWarning("READY_TO_EXIT �޽��� �Ľ� ����");
            return;
        }

        string nickname = parts[1];

        // userResultParent�� UserResult UI�� �ٴ� �θ� Transform
        Transform targetUserResult = userResultParent.Find($"UserResult_{nickname}");
        if (targetUserResult != null)
        {
            Toggle lobbyToggle = targetUserResult.Find("LobbyToggle")?.GetComponent<Toggle>();
            if (lobbyToggle != null)
            {
                lobbyToggle.isOn = true;         // ��� �ѱ�
                lobbyToggle.interactable = false; // ������ ���� ���� ���ϰ� ����
            }
        }
        else
        {
            Debug.LogWarning($"UserResult_{nickname} UI�� ã�� �� �����ϴ�.");
        }
    }

}
