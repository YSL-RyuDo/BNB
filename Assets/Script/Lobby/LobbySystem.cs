using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class LobbySystem : MonoBehaviour
{
    [Header("UserInfo")]
    public TextMeshProUGUI userNameText;
    public TextMeshProUGUI userLevelText;
    public Slider userExpBar;
    public TextMeshProUGUI userExpText;
    private float maxExp = 100;

    [Header("UserList")]
    public GameObject userPrefab;
    public Transform userListContent;

    [Header("RoomList")]
    public Button[] roomButtons; // ��ư 6��
    //public bool[] isOccupied;
    public bool[] isOccupied;

    [Header("CreateRoom")]
    public Image previewImage;
    public Button map1Button;
    public Sprite map1Sprite;
    public Button map2Button;
    public Sprite map2Sprite;
    public Button map3Button;
    public Sprite map3Sprite;

    public Toggle passwordToggle;
    public TMP_InputField passwordInputField;
    public TMP_InputField roomNameInputField;
    public Button createButton;
    public Button logoutButton;

    public TextMeshProUGUI errorText;
    public GameObject createRoomPanel;
    private bool isCreatingRoom = false;

    [Header("EnterRoom")]
    public GameObject enterRoomPanel;
    public TMP_InputField EnterpasswordInputField;
    public Button confirmPasswordButton;
    public string pendingRoomName = "";
    [Header("LobbyChat")]
    public GameObject userChatPrefab;
    public Transform contentParent;

    public TMP_InputField messageInputField;
    public Button sendButton;

    public Dictionary<string, bool> roomPasswordMap = new Dictionary<string, bool>();
    // Start is called before the first frame update
    private void Awake()
    {
        isOccupied = new bool[roomButtons.Length];
    }

    private async void Start()
    {
        if (NetworkConnector.Instance != null)
        {

            var stream = NetworkConnector.Instance.Stream;

            userExpBar.interactable = false;
            string sendUserInfoStr = $"GET_USER_INFO|{NetworkConnector.Instance.UserNickname}\n";
            byte[] sendUserInfoBytes = Encoding.UTF8.GetBytes(sendUserInfoStr);
            await stream.WriteAsync(sendUserInfoBytes, 0, sendUserInfoBytes.Length);

            string sendUserStr = "GET_LOBBY_USER_LIST|\n";
            byte[] sendUserBytes = Encoding.UTF8.GetBytes(sendUserStr);
            await stream.WriteAsync(sendUserBytes, 0, sendUserBytes.Length);

            string sendRoomStr = "GET_ROOM_LIST|\n";
            byte[] sendRoomBytes = Encoding.UTF8.GetBytes(sendRoomStr);
            await stream.WriteAsync(sendRoomBytes, 0, sendRoomBytes.Length);



        }

        map1Button.onClick.AddListener(SelectMap1);
        map2Button.onClick.AddListener(SelectMap2);
        map3Button.onClick.AddListener(SelectMap3);

        passwordToggle.onValueChanged.AddListener(OnPasswordToggleChanged);
        OnPasswordToggleChanged(passwordToggle.isOn);

        createButton.onClick.AddListener(OnCreateButtonClicked);

        sendButton.onClick.AddListener(OnsendClicked);
        logoutButton.onClick.AddListener(OnLogoutClicked);

        confirmPasswordButton.onClick.AddListener(OnConfirmPasswordClicked);

    }

    public void HandleUserListMessage(string message)
    {
        string[] parts = message.Split("|");
        if (parts.Length < 2)
        {
            Debug.LogWarning("�߸��� LOBBY_USER_LIST �޼��� ����");
            return;
        }

        foreach (Transform child in userListContent)
        {
            Destroy(child.gameObject);
        }

        for (int i = 1; i < parts.Length; i++)
        {
            string[] userInfo = parts[i].Split(',');
            if (userInfo.Length != 2)
            {
                Debug.LogWarning($"���� ���� �Ľ� ����: {parts[i]}");
                continue;
            }

            string nickname = userInfo[0];
            int level = 1;
            if (!int.TryParse(userInfo[1], out level))
            {
                Debug.LogWarning($"���� �Ľ� ����:{userInfo[1]}");
                level = 1;
            }

            GameObject item = Instantiate(userPrefab, userListContent);
            TMP_Text[] texts = item.GetComponentsInChildren<TMP_Text>();
            foreach (var text in texts)
            {
                if (text.name == "NicknameText") text.text = nickname;
                else if (text.name == "LevelText") text.text = $"Lv. {level}";
            }
        }
    }

    public void HandleUserInfoMessage(string message)
    {
        Debug.Log("HandleUserInfoMessage ȣ��: " + message);

        if (!message.StartsWith("USER_INFO|"))
        {
            Debug.LogError("USER_INFO �޽��� ���� ����");
            return;
        }

        string data = message.Substring("USER_INFO|".Length).Trim();

        string[] parts = data.Split(',');

        if (parts.Length < 3)
        {
            Debug.LogError("USER_INFO ������ �Ľ� ����: " + data);
            return;
        }

        string nickname = parts[0].Trim();
        int level;
        float exp;

        if (!int.TryParse(parts[1].Trim(), out level))
        {
            Debug.LogError("���� �Ľ� ����: " + parts[1]);
            level = 1; // �⺻��
        }

        if (!float.TryParse(parts[2].Trim(), out exp))
        {
            Debug.LogError("����ġ �Ľ� ����: " + parts[2]);
            exp = 0f;
        }

        Debug.Log($"���� ���� - �г���: {nickname}, ����: {level}, ����ġ: {exp}");

        userNameText.text = nickname;
        userLevelText.text = $"Level {level}";
        maxExp = level * 100;
        float currentExp = Mathf.Clamp(exp, 0f, maxExp);
        float percent = (maxExp > 0f) ? (currentExp / maxExp) * 100f : 0f;
        userExpBar.maxValue = maxExp;
        userExpBar.value = currentExp;

        userExpText.text = $"{percent:0.0}%";
    }

    public void HandleRoomListMessage(string message)
    {
        Debug.Log("HandleRoomListMessage ȣ��: " + message);

        Debug.Log($"roomButtons.Length = {roomButtons?.Length ?? -1}");
        Debug.Log($"isOccupied.Length = {isOccupied?.Length ?? -1}");

        for (int i = 0; i < roomButtons.Length; i++)
        {
            Debug.Log($"roomButtons[{i}] is {(roomButtons[i] == null ? "null" : "not null")}");
        }

        if (isOccupied == null)
        {
            Debug.LogError("isOccupied �迭�� null�Դϴ�!");
        }
        else
        {
            for (int i = 0; i < isOccupied.Length; i++)
            {
                Debug.Log($"isOccupied[{i}] = {isOccupied[i]}");
            }
        }

        if (!message.StartsWith("ROOM_LIST|"))
        {
            Debug.LogError("ROOM_LIST �޽��� ���� ����");
            return;
        }

        string data = message.Substring("ROOM_LIST|".Length);
        string[] rooms = data.Split('|', StringSplitOptions.RemoveEmptyEntries);

        Debug.Log($"�����κ��� ���� �� ����: {rooms.Length}");

        // 1) UI ���� ��� �ʱ�ȭ
        for (int i = 0; i < roomButtons.Length; i++)
        {
            ResetRoomButton(roomButtons[i]);
            isOccupied[i] = false;
        }

        // 2) ���� �� ����Ʈ�� 0�� ���Ժ��� ������� UI�� ����
        for (int i = 0; i < rooms.Length && i < roomButtons.Length; i++)
        {
            string[] parts = rooms[i].Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
            {
                Debug.LogWarning("�� ���� ���� ����: " + rooms[i]);
                continue;
            }

            string roomName = parts[0].Trim();
            string mapName = parts[1].Trim();
            bool hasPassword = parts[2].Trim() == "1";

            roomPasswordMap[roomName] = hasPassword;

            SetRoomButton(roomButtons[i], roomName, GetSpriteForMap(mapName));
            isOccupied[i] = true;

            Debug.Log($"roomButtons[{i}]�� �� '{roomName}' ���� �Ϸ�");
        }
    }

    public void HandleCreateRoomSuccess(string message)
    {
        string[] parts = message.Split('|');
        if (parts.Length < 3) return;

        string roomName = parts[1].Trim();
        string mapName = parts[2].Trim();
        bool isCreator = parts.Length > 3 && parts[3] == "CREATOR";

        // ���� ����Ʈ �Ľ�
        string userListStr = "";
        if (isCreator)
        {
            if (parts.Length > 4)
                userListStr = parts[4].Trim();
        }
        else
        {
            if (parts.Length > 3)
                userListStr = parts[3].Trim();
        }

        // [�߰�] ��й�ȣ ���� �Ľ�
        bool hasPassword = false;
        if (parts.Length > 5 && parts[5].Trim() == "HAS_PASSWORD")
        {
            hasPassword = true;
        }
        roomPasswordMap[roomName] = hasPassword;

        List<string> userList = new List<string>();
        Dictionary<string, int> characterIndexMap = new Dictionary<string, int>();

        if (!string.IsNullOrEmpty(userListStr))
        {
            var userEntries = userListStr.Split(',');
            foreach (var entry in userEntries)
            {
                var userParts = entry.Split(':');
                if (userParts.Length == 2)
                {
                    string nick = userParts[0].Trim();
                    if (int.TryParse(userParts[1].Trim(), out int charIndex))
                    {
                        userList.Add(nick);
                        characterIndexMap[nick] = charIndex;
                    }
                    else
                    {
                        userList.Add(nick);
                        characterIndexMap[nick] = 0; // �⺻��
                        Debug.LogWarning($"ĳ���� �ε��� �Ľ� ����: {userParts[1]}");
                    }
                }
                else
                {
                    string nick = entry.Trim();
                    userList.Add(nick);
                    characterIndexMap[nick] = 0; // �⺻��
                }
            }
        }

        NetworkConnector.Instance.CurrentUserList = userList;

        //NetworkConnector.Instance.CurrentUserCharacterIndices = characterIndexMap;
        NetworkConnector.Instance.SetUserCharacterIndices(characterIndexMap);

        Sprite sprite = GetSpriteForMap(mapName);

        for (int i = 0; i < roomButtons.Length; i++)
        {
            if (!isOccupied[i])
            {
                SetRoomButton(roomButtons[i], roomName, sprite);
                isOccupied[i] = true;
                break;
            }
        }

        Debug.Log($"Room '{roomName}' has users: {string.Join(", ", userList)}");

        if (isCreator)
        {
            NetworkConnector.Instance.CurrentRoomName = roomName;
            NetworkConnector.Instance.SelectedMap = mapName;
            SceneManager.LoadScene("RoomScene");
        }
    }

    public void HandleRoomCreated(string message)
    {
        // �޽��� ��: ROOM_CREATED|123|Map1|user1,user2|HAS_PASSWORD
        string[] parts = message.Split('|');
        if (parts.Length < 4) return;

        string roomName = parts[1].Trim();
        string mapName = parts[2].Trim();
        string userListStr = parts[3].Trim();
        bool hasPassword = parts.Length > 4 && parts[4].Trim() == "HAS_PASSWORD";

        roomPasswordMap[roomName] = hasPassword;

        List<string> userList = userListStr.Split(',').Select(u => u.Trim()).ToList();
        Sprite sprite = GetSpriteForMap(mapName);

        for (int i = 0; i < roomButtons.Length; i++)
        {
            if (!isOccupied[i])
            {
                SetRoomButton(roomButtons[i], roomName, sprite);
                isOccupied[i] = true;
                break;
            }
        }

        Debug.Log($"[ROOM_CREATED] {roomName} | ��� ����: {hasPassword} | ����: {userListStr}");
    }


    public void HandleLobbyChatMessage(string message)
    {
        Debug.Log("HandleLobbyChatMessage ȣ��: " + message);

        string[] parts = message.Split('|');
        if (parts.Length >= 2)
        {
            string[] chatParts = parts[1].Split(':');
            if (chatParts.Length >= 2)
            {
                string userNickname = chatParts[0];
                string chatMessage = string.Join(":", chatParts, 1, chatParts.Length - 1);

                AddChatMessage(userNickname, chatMessage);
            }
            else
            {
                Debug.LogWarning("LOBBY_CHAT �г���/�޽��� �Ľ� ����: " + parts[1]);
            }
        }
        else
        {
            Debug.LogWarning("LOBBY_CHAT �޽��� ���� ����: " + message);
        }
    }

    public void HandleEnterRoomSuccess(string message)
    {
        string[] parts = message.Split('|');
        if (parts.Length < 3) return;

        string roomName = parts[1];
        string userListStr = parts[2];

        NetworkConnector.Instance.CurrentRoomName = roomName;

        List<string> userList = userListStr.Split(',').ToList();
        NetworkConnector.Instance.CurrentUserList = userList;

        Debug.Log($"[���� ����] ��: {roomName}, ����: {userListStr}");

        // �� ������ ��ȯ
        SceneManager.LoadScene("RoomScene");
    }

    private void SetRoomButton(Button btn, string roomName, Sprite sprite)
    {
        var roomNameText = btn.transform.Find("RoomNameText")?.GetComponent<TextMeshProUGUI>();
        if (roomNameText != null)
            roomNameText.text = roomName;

        var roomImage = btn.transform.Find("RoomImage")?.GetComponent<Image>();
        if (roomImage != null)
        {
            roomImage.sprite = sprite;
            Debug.Log($"�� �̹��� ���� �Ϸ�: {sprite?.name}");
        }
        else
        {
            Debug.LogWarning("RoomImage Image ������Ʈ �� ã��");
        }

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnRoomButtonClicked(roomName));
        btn.gameObject.SetActive(true);
    }

    private Sprite GetSpriteForMap(string mapName)
    {
        Sprite sprite = null;
        switch (mapName)
        {
            case "Map1": sprite = map1Sprite; break;
            case "Map2": sprite = map2Sprite; break;
            case "Map3": sprite = map3Sprite; break;
            default: sprite = null; break;
        }
        Debug.Log($"GetSpriteForMap: mapName={mapName}, sprite={(sprite == null ? "null" : sprite.name)}");
        return sprite;
    }

    private void ResetRoomButton(Button btn)
    {
        var roomNameText = btn.transform.Find("RoomNameText")?.GetComponent<TextMeshProUGUI>();
        if (roomNameText != null)
            roomNameText.text = "";

        var roomImage = btn.transform.Find("RoomImage")?.GetComponent<Image>();
        if (roomImage != null)
            roomImage.sprite = null;

        btn.onClick.RemoveAllListeners();
    }

    private void OnRoomButtonClicked(string roomName)
    {
        roomName = roomName.Trim();

        Debug.Log($"�� Ŭ����: {roomName}");

        if (roomPasswordMap.TryGetValue(roomName, out bool hasPassword))
        {
            Debug.Log($"roomPasswordMap[{roomName}] = {hasPassword}");
            if (hasPassword)
            {
                pendingRoomName = roomName;
                enterRoomPanel.SetActive(true);
                return;
            }
        }
        else
        {
            Debug.LogWarning($"roomPasswordMap�� '{roomName}' Ű�� �����ϴ�.");
        }

        SendEnterRoom(roomName, ""); // ��й�ȣ ���� �ٷ� ���� �õ�
    }

    private void OnConfirmPasswordClicked()
    {
        string password = EnterpasswordInputField.text.Trim();

        if (string.IsNullOrEmpty(pendingRoomName))
        {
            Debug.LogWarning("������ �� �̸��� �������� �ʾҽ��ϴ�.");
            return;
        }

        Debug.Log($"��й�ȣ �Է�: {password}, ���� �õ��� ��: {pendingRoomName}");
        SendEnterRoom(pendingRoomName, password);
    }


    private void SendEnterRoom(string roomName, string password)
    {
        string enterMessage = $"ENTER_ROOM|{roomName}|{password}\n";
        byte[] buffer = Encoding.UTF8.GetBytes(enterMessage);
        NetworkConnector.Instance.Stream.Write(buffer, 0, buffer.Length);
    }

    public void SelectMap1()
    {
        previewImage.sprite = map1Sprite;
        NetworkConnector.Instance.SelectedMap = "Map1";
    }

    public void SelectMap2()
    {
        previewImage.sprite = map2Sprite;
        NetworkConnector.Instance.SelectedMap = "Map2";
    }
    public void SelectMap3()
    {
        previewImage.sprite = map3Sprite;
        NetworkConnector.Instance.SelectedMap = "Map3";
    }

    private void OnPasswordToggleChanged(bool isOn)
    {
        Debug.Log($"Toggle Changed: {isOn}");

        passwordInputField.interactable = isOn;
        passwordInputField.readOnly = !isOn;

        if (!isOn)
        {
            passwordInputField.text = "";
            passwordInputField.DeactivateInputField();
            EventSystem.current.SetSelectedGameObject(null);
        }

        if (passwordInputField.textComponent != null)
            passwordInputField.textComponent.raycastTarget = isOn;

        var image = passwordInputField.GetComponent<Image>();
        if (image != null)
            image.color = isOn ? Color.white : new Color(0.85f, 0.85f, 0.85f);
    }

    private async void OnCreateButtonClicked()
    {
        if (isCreatingRoom) return;
        isCreatingRoom = true;

        ClearError();

        if (roomNameInputField == null || string.IsNullOrWhiteSpace(roomNameInputField.text))
        {
            ShowError("�� �̸��� �Է����ּ���.");
            isCreatingRoom = false;
            return;
        }

        if (previewImage == null || previewImage.sprite == null)
        {
            ShowError("���� �������ּ���.");
            isCreatingRoom = false;
            return;
        }

        if (passwordToggle.isOn)
        {
            if (passwordInputField == null || string.IsNullOrWhiteSpace(passwordInputField.text))
            {
                ShowError("��й�ȣ�� �Է����ּ���.");
                isCreatingRoom = false;
                return;
            }
        }

        if (createRoomPanel != null)
        {
            createRoomPanel.SetActive(false);
        }
        string roomName = roomNameInputField.text.Trim();
        string selectedMap = NetworkConnector.Instance.SelectedMap;
        string password = passwordToggle.isOn ? passwordInputField.text.Trim() : "";

        string packet = $"CREATE_ROOM|{roomName}|{selectedMap}|{password}";

        try
        {
            var stream = NetworkConnector.Instance.Stream;

            if (stream == null)
            {
                ShowError("������ ����Ǿ� ���� �ʽ��ϴ�.");
                isCreatingRoom = false;
                return;
            }

            byte[] sendBytes = System.Text.Encoding.UTF8.GetBytes(packet);
            await stream.WriteAsync(sendBytes, 0, sendBytes.Length);

            // ���������� �������� isCreatingRoom false�� finally���� ó��
        }
        catch (System.Exception ex)
        {
            ShowError("���� ���� ����: " + ex.Message);
        }
        finally
        {
            isCreatingRoom = false;
        }
    }

    private void ShowError(string message)
    {
        if (errorText != null)
            errorText.text = message;
    }

    private void ClearError()
    {
        if (errorText != null)
            errorText.text = "";
    }

    private async void OnsendClicked()
    {
        if (string.IsNullOrEmpty(messageInputField.text))
        {
            return;
        }

        string message = messageInputField.text;
        string nickname = NetworkConnector.Instance.UserNickname;

        messageInputField.text = string.Empty; // �Է� �ʵ� �ʱ�ȭ

        try
        {
            var stream = NetworkConnector.Instance.Stream;
            string sendStr = $"LOBBY_MESSAGE|{nickname}:{message}\n";
            byte[] sendBytes = Encoding.UTF8.GetBytes(sendStr);
            await stream.WriteAsync(sendBytes, 0, sendBytes.Length);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LobbyChatManager] ä�� �޽��� ���� �� ���� �߻�: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public void AddChatMessage(string nickname, string message)
    {
        CreateChatUI($"{nickname}: {message}");
    }

    private void CreateChatUI(string fullMessage)
    {
        GameObject chatItem = Instantiate(userChatPrefab, contentParent);
        chatItem.transform.SetAsLastSibling();

        var chatText = chatItem.GetComponentInChildren<TMP_Text>();
        if (chatText != null)
        {
            chatText.text = fullMessage;
        }
    }

    private async void OnLogoutClicked()
    {
        try
        {
            var stream = NetworkConnector.Instance.Stream;

            string nickname = NetworkConnector.Instance.UserNickname;
            if (string.IsNullOrEmpty(nickname))
            {
                Debug.LogError("�г����� �������� �ʾҽ��ϴ�. �α׾ƿ� �޽����� ���� �� �����ϴ�.");
                return;
            }

            string logoutMsg = $"LOGOUT|{nickname}\n";
            byte[] quitMsg = Encoding.UTF8.GetBytes(logoutMsg);
            await stream.WriteAsync(quitMsg, 0, quitMsg.Length);
            await stream.FlushAsync();

            if (NetworkConnector.Instance.CurrentUserList.Contains(nickname))
            {
                NetworkConnector.Instance.CurrentUserList.Remove(nickname);
                Debug.Log($"[�α׾ƿ�] ���� ���ŵ�: {nickname}");
            }

            NetworkConnector.Instance.UserNickname = null;

            UnityEngine.SceneManagement.SceneManager.LoadScene("LoginScene");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("�α׾ƿ� �� ���� �߻�: " + ex.Message);
        }
    }

}
