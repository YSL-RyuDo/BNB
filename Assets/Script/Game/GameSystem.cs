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
        Debug.Log("[GameSceneInitializer] ������ GET_MAP ��û ����");
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
}
