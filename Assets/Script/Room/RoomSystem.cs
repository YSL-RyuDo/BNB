using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;

public class RoomSystem : MonoBehaviour
{
    public TextMeshProUGUI roomNameText;
    public GameObject[] roomPlayerInfos;
    public Button startGameButton;
    public Button exitButton;

    private void Start()
    {
        var client = NetworkConnector.Instance;
        if (roomNameText != null)
            roomNameText.text = client.CurrentRoomName;

        UpdatePlayerInfoUI(client.CurrentUserList);
        OnEnterRoomSuccess(client.CurrentRoomName, string.Join(",", client.CurrentUserList));
        startGameButton.onClick.AddListener(OnClickStartGame);
        exitButton.onClick.AddListener(OnExitRoomClicked);
    }
    public void HandleUserJoined(string message)
    {
        var client = NetworkConnector.Instance;
        string[] parts = message.Split('|');
        if (parts.Length >= 3)
        {
            string roomName = parts[1];
            string userList = parts[2];

            client.CurrentRoomName = roomName;
            client.CurrentUserList = new List<string>(userList.Split(','));

            // UI ���� ȣ��
            RoomSystem roomSceneManager = FindObjectOfType<RoomSystem>();
            if (roomSceneManager != null)
            {
                roomSceneManager.RefreshRoomUI(client.CurrentUserList, roomName);
            }
            else
            {
                Debug.LogWarning("RoomSceneManager�� ã�� �� �����ϴ�.");
            }
        }
    }


    public void UpdatePlayerInfoUI(List<string> userList)
    {
        Debug.Log($"UpdatePlayerInfoUI ȣ�� - userList.Count: {userList.Count}");
        for (int i = 0; i < roomPlayerInfos.Length; i++)
        {
            var nameTextTransform = roomPlayerInfos[i].transform.Find("Image/NameText");
            if (nameTextTransform == null)
            {
                Debug.LogWarning($"roomPlayerInfos[{i}]�� 'Image/NameText'�� �����ϴ�.");
                continue;
            }

            var nameText = nameTextTransform.GetComponent<TextMeshProUGUI>();
            if (nameText == null)
            {
                Debug.LogWarning($"roomPlayerInfos[{i}] 'Image/NameText'�� TextMeshProUGUI�� �����ϴ�.");
                continue;
            }

            if (i < userList.Count)
            {
                nameText.text = userList[i];
                Debug.Log($"roomPlayerInfos[{i}]�� ���� �̸� ����: {userList[i]}");
            }
            else
            {
                nameText.text = "";
                Debug.Log($"roomPlayerInfos[{i}]�� �� ���ڿ� ����");
            }
        }
    }

    // �������� ���� ����Ʈ ���� �޽����� ���� �� ȣ���� �޼���
    public void RefreshRoomUI(List<string> updatedUserList, string updatedRoomName = null)
    {
        Debug.Log($"RefreshRoomUI ȣ�� - updatedRoomName: {updatedRoomName}, updatedUserList.Count: {updatedUserList.Count}");

        if (!string.IsNullOrEmpty(updatedRoomName))
        {
            if (roomNameText != null)
            {
                roomNameText.text = updatedRoomName;
                Debug.Log("roomNameText.text ������Ʈ��: " + updatedRoomName);
            }

            NetworkConnector.Instance.CurrentRoomName = updatedRoomName;
        }

        if (updatedUserList.Count > 0)
        {
            NetworkConnector.Instance.CurrentRoomLeader = updatedUserList[0];
            Debug.Log($"[���� ����] ������ {updatedUserList[0]}");
        }

        NetworkConnector.Instance.CurrentUserList = updatedUserList;
        UpdatePlayerInfoUI(updatedUserList);

        OnEnterRoomSuccess(NetworkConnector.Instance.CurrentRoomName, string.Join(",", updatedUserList));
    }



    void OnEnterRoomSuccess(string roomName, string userList)
    {
        string[] users = userList.Split(',');
        string myNick = PlayerPrefs.GetString("nickname")?.Trim();
        string leaderNick = NetworkConnector.Instance.CurrentRoomLeader?.Trim();

        Debug.Log($"�� �г���: '{myNick}', ���� �г���: '{leaderNick}'");

        if (!string.IsNullOrEmpty(myNick) && !string.IsNullOrEmpty(leaderNick) &&
            myNick.Equals(leaderNick, System.StringComparison.OrdinalIgnoreCase))
        {
            startGameButton.gameObject.SetActive(true);
            Debug.Log("�����Դϴ�! ���� ��ư Ȱ��ȭ.");
        }
        else
        {
            startGameButton.gameObject.SetActive(false);
            Debug.Log("������ �ƴմϴ�. ���� ��ư ��Ȱ��ȭ.");
        }
    }



    public async void OnClickStartGame()
    {
        string roomName = NetworkConnector.Instance.CurrentRoomName;
        string msg = $"START_GAME|{roomName}\n";

        var stream = NetworkConnector.Instance.Stream;
        byte[] sendBytes = Encoding.UTF8.GetBytes(msg);

        await stream.WriteAsync(sendBytes, 0, sendBytes.Length);
    }
    public async void OnExitRoomClicked()
    {
        string roomName = NetworkConnector.Instance.CurrentRoomName;
        string myNickname = PlayerPrefs.GetString("nickname");
        string msg = $"EXIT_ROOM|{roomName}|{myNickname}\n";

        var stream = NetworkConnector.Instance.Stream;
        byte[] sendBytes = Encoding.UTF8.GetBytes(msg);
        await stream.WriteAsync(sendBytes, 0, sendBytes.Length);

        UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
    }
}
