using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;

public class RoomSystem : MonoBehaviour
{
    public TextMeshProUGUI roomNameText;
    public GameObject[] roomPlayerInfos;

    public Button[] characterChooseButton;
    public Sprite[] characterSprites;
    private int myPlayerIndex = -1;

    public Button startGameButton;
    public Button exitButton;
    public Dictionary<string, int> characterIndexMap = new Dictionary<string, int>();

    private void Start()
    {
        var client = NetworkConnector.Instance;
        if (roomNameText != null)
            roomNameText.text = client.CurrentRoomName;

        characterIndexMap = new Dictionary<string, int>(client.CurrentUserCharacterIndices);

        OnEnterRoomSuccess(client.CurrentRoomName, string.Join(",", client.CurrentUserList));
        startGameButton.onClick.AddListener(OnClickStartGame);
        exitButton.onClick.AddListener(OnExitRoomClicked);

        foreach (var kvp in client.CurrentUserCharacterIndices)
        {
            characterIndexMap[kvp.Key] = kvp.Value;
        }

        string myNick = NetworkConnector.Instance.UserNickname; //PlayerPrefs.GetString("nickname")?.Trim();
        for (int i = 0; i < client.CurrentUserList.Count; i++)
        {
            if (client.CurrentUserList[i].Trim() == myNick)
            {
                myPlayerIndex = i;
                break;
            }
        }

        // ĳ���� ��ư ����
        for (int i = 0; i < characterChooseButton.Length; i++)
        {
            int idx = i; // ĸó ������ ����
            characterChooseButton[i].onClick.AddListener(() => OnClickChooseCharacter(idx));
        }
        UpdatePlayerInfoUI(client.CurrentUserList);
    }

    public void HandleUserJoined(string message)
    {
        // ��: "REFRESH_ROOM_SUCCESS|���̸�|userA:1,userB:2,userC:0"
        string[] parts = message.Split('|');
        if (parts.Length < 3)
        {
            Debug.LogWarning("REFRESH_ROOM_SUCCESS �Ľ� ����");
            return;
        }

        string roomName = parts[1];
        string[] userTokens = parts[2].Split(',');

        List<string> nicknames = new List<string>();

        foreach (string token in userTokens)
        {
            if (token.Contains(":"))
            {
                string[] pair = token.Split(':');
                string nickname = pair[0].Trim();
                if (!string.IsNullOrEmpty(nickname))
                {
                    nicknames.Add(nickname);

                    if (int.TryParse(pair[1], out int charIndex))
                    {
                        characterIndexMap[nickname] = charIndex;
                        NetworkConnector.Instance.CurrentUserCharacterIndices[nickname] = charIndex;
                    }
                    else
                    {
                        characterIndexMap[nickname] = 0;
                        NetworkConnector.Instance.CurrentUserCharacterIndices[nickname] = 0;
                        Debug.LogWarning($"ĳ���� �ε��� �Ľ� ����: {token}");
                    }
                }
            }
            else
            {
                string nickname = token.Trim();
                if (!string.IsNullOrEmpty(nickname))
                {
                    nicknames.Add(nickname);
                    characterIndexMap[nickname] = 0;
                    NetworkConnector.Instance.CurrentUserCharacterIndices[nickname] = 0;
                }
            }
        }

        // ����
        NetworkConnector.Instance.CurrentRoomName = roomName;
        NetworkConnector.Instance.CurrentUserList = nicknames;

        RefreshRoomUI(nicknames, roomName);
    }


    public void UpdatePlayerInfoUI(List<string> userList)
    {
        Debug.Log($"UpdatePlayerInfoUI ȣ�� - userList.Count: {userList.Count}");
        for (int i = 0; i < roomPlayerInfos.Length; i++)
        {
            var nameTextTransform = roomPlayerInfos[i].transform.Find("Image/NameText");
            var characterImageTransform = roomPlayerInfos[i].transform.Find("PlayerCharacterImage");

            if (nameTextTransform == null || characterImageTransform == null)
                continue;

            var nameText = nameTextTransform.GetComponent<TextMeshProUGUI>();
            var imageComponent = characterImageTransform.GetComponent<Image>();

            if (nameText == null || imageComponent == null)
                continue;

            if (i < userList.Count)
            {
                string nickname = userList[i];
                nameText.text = nickname;

                if (characterIndexMap.TryGetValue(nickname, out int characterIndex))
                {
                    Debug.Log($"[{i}] ĳ���� �ε���: {characterIndex}");
                    if (characterIndex >= 0 && characterIndex < characterSprites.Length)
                    {
                        imageComponent.sprite = characterSprites[characterIndex];
                    }
                    else
                    {
                        Debug.LogWarning($"ĳ���� �ε����� ��������Ʈ �迭 ������ ���: {characterIndex}");
                        imageComponent.sprite = null;
                    }
                }
                else
                {
                    Debug.LogWarning($"characterIndexMap�� {nickname} Ű ���� -> �⺻�� ����");
                    characterIndex = 0;
                    characterIndexMap[nickname] = 0;
                }


                Debug.Log($"roomPlayerInfos[{i}]�� ���� �̸�/ĳ���� ����: {nickname} / {characterIndex}");
            }
            else
            {
                nameText.text = "";
                imageComponent.sprite = null; // ���� ����
                Debug.Log($"roomPlayerInfos[{i}]�� �� ���� �ʱ�ȭ");
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

        // ������ ���� �г��ӿ� �ش��ϴ� ĳ���� ���� ����
        var currentUsers = new HashSet<string>(updatedUserList);
        var keysToRemove = new List<string>();

        foreach (var kvp in characterIndexMap)
        {
            if (!currentUsers.Contains(kvp.Key))
                keysToRemove.Add(kvp.Key);
        }

        foreach (var key in keysToRemove)
            characterIndexMap.Remove(key);


        NetworkConnector.Instance.CurrentUserList = updatedUserList;
        UpdatePlayerInfoUI(updatedUserList);

        OnEnterRoomSuccess(NetworkConnector.Instance.CurrentRoomName, string.Join(",", updatedUserList));
    }

    private async void OnClickChooseCharacter(int characterIndex)
    {
        if (myPlayerIndex < 0 || myPlayerIndex >= roomPlayerInfos.Length)
        {
            Debug.LogWarning("�� ���� �ε����� �ùٸ��� �ʽ��ϴ�.");
            return;
        }

        string nickname = NetworkConnector.Instance.UserNickname;//PlayerPrefs.GetString("nickname")?.Trim();
        string roomName = NetworkConnector.Instance.CurrentRoomName;
        string msg = $"CHOOSE_CHARACTER|{roomName}|{nickname}|{characterIndex}\n";

        var stream = NetworkConnector.Instance.Stream;
        byte[] sendBytes = Encoding.UTF8.GetBytes(msg);
        await stream.WriteAsync(sendBytes, 0, sendBytes.Length);

        Debug.Log($"ĳ���� ���� ����: {msg.Trim()}");
    }

    void OnEnterRoomSuccess(string roomName, string userList)
    {
        string[] users = userList.Split(',');
        string myNick = NetworkConnector.Instance.UserNickname;//PlayerPrefs.GetString("nickname")?.Trim();
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
        NetworkConnector.Instance.CurrentUserCharacterIndices.Clear();
        foreach (var kvp in characterIndexMap)
        {
            NetworkConnector.Instance.CurrentUserCharacterIndices[kvp.Key] = kvp.Value;
        }

        string roomName = NetworkConnector.Instance.CurrentRoomName;
        string msg = $"START_GAME|{roomName}\n";

        var stream = NetworkConnector.Instance.Stream;
        byte[] sendBytes = Encoding.UTF8.GetBytes(msg);

        await stream.WriteAsync(sendBytes, 0, sendBytes.Length);
    }
    public async void OnExitRoomClicked()
    {
        string roomName = NetworkConnector.Instance.CurrentRoomName;
        string myNickname = NetworkConnector.Instance.UserNickname;
        string msg = $"EXIT_ROOM|{roomName}|{myNickname}\n";

        var stream = NetworkConnector.Instance.Stream;
        byte[] sendBytes = Encoding.UTF8.GetBytes(msg);
        await stream.WriteAsync(sendBytes, 0, sendBytes.Length);

        UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
    }


    public void UpdateCharacterChoice(string userNickname, int characterIndex)
    {
        characterIndexMap[userNickname] = characterIndex;

        // �ٷ� UI �ݿ��� ����������, ��ü UI �簻�� �� �ڵ� �ݿ��ǵ��� �ص� ��
        UpdatePlayerInfoUI(NetworkConnector.Instance.CurrentUserList);
    }
}
