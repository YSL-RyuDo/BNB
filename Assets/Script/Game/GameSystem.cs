using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class GameSystem : MonoBehaviour
{
    public static GameSystem Instance;

    public CharacterSystem playerManager;
    public Transform userInfoContent;
    public GameObject userInfo;

    private void Awake()
    {
        Instance = this;
    }

    async void Start()
    {
        string roomName = NetworkConnector.Instance.CurrentRoomName;
        string selectedMap = NetworkConnector.Instance.SelectedMap;
        string getMapMsg = $"GET_MAP|{roomName}|{selectedMap}\n";
        byte[] getMapBytes = Encoding.UTF8.GetBytes(getMapMsg);
        await NetworkConnector.Instance.Stream.WriteAsync(getMapBytes, 0, getMapBytes.Length);
        Debug.Log(getMapMsg);
        Debug.Log("[GameSceneInitializer] 서버에 GET_MAP 요청 보냄");
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
}
