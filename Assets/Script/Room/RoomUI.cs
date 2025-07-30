using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RoomUI : MonoBehaviour
{
    public TextMeshProUGUI roomNameText;
    public GameObject[] roomPlayerInfos;

    public Button[] characterChooseButtons;
    public Sprite[] characterSprites;

    public Button startGameButton;
    public Button exitButton;

    private int myPlayerIndex = -1;

    [SerializeField] private RoomSender roomSender;


    private void Start()
    {

        if (!string.IsNullOrEmpty(NetworkConnector.Instance.PendingRoomEnterMessage))
        {
            string msg = NetworkConnector.Instance.PendingRoomEnterMessage;
            NetworkConnector.Instance.PendingRoomEnterMessage = null;

            // RoomReceiver�� �޽����� ó���ϵ��� ���� ȣ��
            FindObjectOfType<RoomReceiver>()?.HandleMessage(msg);
        }

        var client = NetworkConnector.Instance;

        if (roomNameText != null)
            roomNameText.text = client.CurrentRoomName;

        OnEnterRoomSuccess(client.CurrentRoomName, string.Join(",", client.CurrentUserList));
        startGameButton.onClick.AddListener(OnClickStartGame);
        exitButton.onClick.AddListener(OnExitRoomClicked);

        string myNick = client.UserNickname?.Trim();

        // �� �ε��� ã��
        myPlayerIndex = client.CurrentUserList.FindIndex(nick => nick.Trim() == myNick);

        // ĳ���� ��ư �̺�Ʈ ���
        for (int i = 0; i < characterChooseButtons.Length; i++)
        {
            int idx = i;
            characterChooseButtons[i].onClick.AddListener(() => OnClickChooseCharacter(idx));
        }

        // �⺻ UI ����
        UpdatePlayerInfoUI(client.CurrentUserList);

        // ĳ���� ���� ��û
        roomSender.SendGetCharacterInfo(myNick);
    }

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

    public void UpdatePlayerSlots(List<string> userList)
    {
        var characterMap = NetworkConnector.Instance.CurrentUserCharacterIndices;

        for (int i = 0; i < roomPlayerInfos.Length; i++)
        {
            var slot = roomPlayerInfos[i];
            var nameText = slot.transform.Find("Image/NameText")?.GetComponent<TextMeshProUGUI>();
            var image = slot.transform.Find("PlayerCharacterImage")?.GetComponent<Image>();

            if (nameText == null || image == null) continue;

            if (i < userList.Count)
            {
                string nick = userList[i];
                nameText.text = nick;

                if (!characterMap.TryGetValue(nick, out int charIndex))
                {
                    charIndex = 0;
                    NetworkConnector.Instance.CurrentUserCharacterIndices[nick] = 0;
                }

                if (charIndex >= 0 && charIndex < characterSprites.Length)
                    image.sprite = characterSprites[charIndex];
                else
                    image.sprite = null;
            }
            else
            {
                nameText.text = "";
                image.sprite = null;
            }
        }
    }



    private void OnClickChooseCharacter(int characterIndex)
    {
        if (myPlayerIndex < 0 || myPlayerIndex >= roomPlayerInfos.Length)
        {
            Debug.LogWarning("�� ���� �ε����� �ùٸ��� �ʽ��ϴ�.");
            return;
        }

        string nickname = NetworkConnector.Instance.UserNickname;//PlayerPrefs.GetString("nickname")?.Trim();
        string roomName = NetworkConnector.Instance.CurrentRoomName;
        roomSender.SendChooseCharacter(nickname, roomName, characterIndex);
    }

    public void OnClickStartGame()
    {
        // ���� ������ �ʿ� ���� CurrentUserCharacterIndices�� �̹� �ֽ� ���¶�� ����
        // ���� �ٸ� ������ �����Ͱ� �ִٸ� �װ� �ݿ��ϴ� �ڵ带 ���⿡ �߰�

        string roomName = NetworkConnector.Instance.CurrentRoomName;

        roomSender.SendStartGame(roomName);
    }

    public void OnExitRoomClicked()
    {
        string roomName = NetworkConnector.Instance.CurrentRoomName;
        string myNickname = NetworkConnector.Instance.UserNickname;
        
        roomSender.SendExitRoom(roomName, myNickname);

        SceneManager.LoadScene("LobbyScene");
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

    public void UpdateCharacterChoice(string userNickname, int characterIndex)
    {
        NetworkConnector.Instance.SetOrUpdateUserCharacter(userNickname, characterIndex);

        // UI ��� �ݿ�
        UpdatePlayerInfoUI(NetworkConnector.Instance.CurrentUserList);
    }
}
