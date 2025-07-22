using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

public class RoomSystem : MonoBehaviour
{
    public static RoomSystem Instance;


    public TextMeshProUGUI roomNameText;
    public GameObject[] roomPlayerInfos;

    public Button[] characterChooseButton;
    public Sprite[] characterSprites;
    private int myPlayerIndex = -1;

    public Button startGameButton;
    public Button exitButton;
    //public Dictionary<string, int> characterIndexMap = new Dictionary<string, int>();
    private void Awake()
    {
        Instance = this;
    }

    private async void Start()
    {
        var client = NetworkConnector.Instance;

        if (roomNameText != null)
            roomNameText.text = client.CurrentRoomName;

        // �� �̻� characterIndexMap ��� �� �ϹǷ� ����

        OnEnterRoomSuccess(client.CurrentRoomName, string.Join(",", client.CurrentUserList));
        startGameButton.onClick.AddListener(OnClickStartGame);
        exitButton.onClick.AddListener(OnExitRoomClicked);

        string myNick = client.UserNickname?.Trim();

        // �� �÷��̾� �ε��� ã��
        myPlayerIndex = client.CurrentUserList.FindIndex(nick => nick.Trim() == myNick);

        // ĳ���� ��ư �̺�Ʈ ����
        for (int i = 0; i < characterChooseButton.Length; i++)
        {
            int idx = i; // ĸó ������ ����
            characterChooseButton[i].onClick.AddListener(() => OnClickChooseCharacter(idx));
        }

        // UI ������Ʈ - ĳ���� �ε��� ���� NetworkConnector���� ���� ������
        UpdatePlayerInfoUI(client.CurrentUserList);

        // ������ ĳ���� ���� ��û ������
        string getCharacterMsg = $"GET_CHARACTER|{myNick}\n";
        byte[] getCharacterBytes = Encoding.UTF8.GetBytes(getCharacterMsg);

        var stream = client.Stream;
        if (stream != null && stream.CanWrite)
        {
            await stream.WriteAsync(getCharacterBytes, 0, getCharacterBytes.Length);
        }
    }


    public void HandleUserJoined(string message)
    {
        // ��: "REFRESH_ROOM_SUCCESS|���̸�|���̸�|userA:1,userB:2,userC:0"
        string[] parts = message.Split('|');
        if (parts.Length < 4)
        {
            Debug.LogWarning("REFRESH_ROOM_SUCCESS �Ľ� ����");
            return;
        }

        string roomName = parts[1];
        string mapName = parts[2];
        string[] userTokens = parts[3].Split(',');

        List<string> nicknames = new List<string>();

        // NetworkConnector.Instance.CurrentUserCharacterIndices.Clear();

        foreach (string token in userTokens)
        {
            if (token.Contains(":"))
            {
                var pair = token.Split(':');
                string nickname = pair[0].Trim();
                int charIndex = 0;
                if (!int.TryParse(pair[1], out charIndex))
                {
                    charIndex = 0; // �⺻��
                }
                NetworkConnector.Instance.SetOrUpdateUserCharacter(nickname, charIndex);
                nicknames.Add(nickname);
            }
            else
            {
                string nickname = token.Trim();
                NetworkConnector.Instance.SetOrUpdateUserCharacter(nickname, 0);
                nicknames.Add(nickname);
            }
        }

        // ����
        NetworkConnector.Instance.CurrentRoomName = roomName;
        NetworkConnector.Instance.SelectedMap = mapName;
        NetworkConnector.Instance.CurrentUserList = nicknames;
        NetworkConnector.Instance.CurrentRoomLeader = nicknames.Count > 0 ? nicknames[0] : null;

        RefreshRoomUI(nicknames, roomName);
    }

    public void HandleCharacterList(string message)
    {
        // ��: CHARACTER_LIST|1,0,1,1,0,0,1
        string[] parts = message.Split('|');
        if (parts.Length < 2)
        {
            Debug.LogWarning("[RoomSystem] CHARACTER_LIST �޽��� ���� ����");
            return;
        }

        string[] tokens = parts[1].Split(',');
        if (tokens.Length != characterChooseButton.Length)
        {
            Debug.LogWarning($"[RoomSystem] ĳ���� ���� ����ġ: ��ư={characterChooseButton.Length}, ������={tokens.Length}");
            return;
        }

        for (int i = 0; i < tokens.Length; i++)
        {
            bool hasCharacter = int.TryParse(tokens[i], out int hasChar) && hasChar == 1;

            // ĳ���� ��ư Ŭ�� ���� ����
            characterChooseButton[i].interactable = hasCharacter;

            // ��� �̹��� ó��
            Transform lockImage = characterChooseButton[i].transform.Find("LockImage");
            if (lockImage != null)
            {
                lockImage.gameObject.SetActive(!hasCharacter); // ���� �� �ϸ� ��� �̹��� �ѱ�
            }

            // �ʿ��ϸ� ���� ������ ����
            var buttonImage = characterChooseButton[i].GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = hasCharacter ? Color.white : new Color(1f, 1f, 1f, 0.5f);
            }
        }


        Debug.Log("[RoomSystem] ĳ���� ���� ���� UI �� ��� �ݿ� �Ϸ�");
    }

    public void UpdatePlayerInfoUI(List<string> userList)
    {
        Debug.Log($"UpdatePlayerInfoUI ȣ�� - userList.Count: {userList.Count}");

        var characterIndexMap = NetworkConnector.Instance.CurrentUserCharacterIndices;

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

                    // �⺻���� NetworkConnector���� ���� (���� ����)
                    NetworkConnector.Instance.CurrentUserCharacterIndices[nickname] = 0;

                    imageComponent.sprite = characterSprites[0];
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
        else
        {
            NetworkConnector.Instance.CurrentRoomLeader = null;
            Debug.LogWarning("[���� ����] ������Ʈ�� ���� ����Ʈ�� �������");
        }

        // ������ ���� �г��ӿ� �ش��ϴ� ĳ���� ���� ����
        var currentUsers = new HashSet<string>(updatedUserList);
        var keysToRemove = new List<string>();

        var characterIndexMap = NetworkConnector.Instance.CurrentUserCharacterIndices;

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
        // ���� ������ �ʿ� ���� CurrentUserCharacterIndices�� �̹� �ֽ� ���¶�� ����
        // ���� �ٸ� ������ �����Ͱ� �ִٸ� �װ� �ݿ��ϴ� �ڵ带 ���⿡ �߰�

        string roomName = NetworkConnector.Instance.CurrentRoomName;
        string msg = $"START_GAME|{roomName}\n";

        var stream = NetworkConnector.Instance.Stream;
        byte[] sendBytes = Encoding.UTF8.GetBytes(msg);

        if (stream != null && stream.CanWrite)
        {
            await stream.WriteAsync(sendBytes, 0, sendBytes.Length);
        }
        else
        {
            Debug.LogWarning("��Ʈ���� ���ų� ���� �Ұ��� �����Դϴ�.");
        }
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
        NetworkConnector.Instance.SetOrUpdateUserCharacter(userNickname, characterIndex);

        // UI ��� �ݿ�
        UpdatePlayerInfoUI(NetworkConnector.Instance.CurrentUserList);
    }

}
