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
    public Button[] roomButtons; // 버튼 6개
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
            Debug.LogWarning("잘못된 LOBBY_USER_LIST 메세지 형식");
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
                Debug.LogWarning($"유저 정보 파싱 실패: {parts[i]}");
                continue;
            }

            string nickname = userInfo[0];
            int level = 1;
            if (!int.TryParse(userInfo[1], out level))
            {
                Debug.LogWarning($"레벨 파싱 실패:{userInfo[1]}");
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
        Debug.Log("HandleUserInfoMessage 호출: " + message);

        if (!message.StartsWith("USER_INFO|"))
        {
            Debug.LogError("USER_INFO 메시지 포맷 오류");
            return;
        }

        string data = message.Substring("USER_INFO|".Length).Trim();

        string[] parts = data.Split(',');

        if (parts.Length < 3)
        {
            Debug.LogError("USER_INFO 데이터 파싱 실패: " + data);
            return;
        }

        string nickname = parts[0].Trim();
        int level;
        float exp;

        if (!int.TryParse(parts[1].Trim(), out level))
        {
            Debug.LogError("레벨 파싱 실패: " + parts[1]);
            level = 1; // 기본값
        }

        if (!float.TryParse(parts[2].Trim(), out exp))
        {
            Debug.LogError("경험치 파싱 실패: " + parts[2]);
            exp = 0f;
        }

        Debug.Log($"유저 정보 - 닉네임: {nickname}, 레벨: {level}, 경험치: {exp}");

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
        Debug.Log("HandleRoomListMessage 호출: " + message);

        Debug.Log($"roomButtons.Length = {roomButtons?.Length ?? -1}");
        Debug.Log($"isOccupied.Length = {isOccupied?.Length ?? -1}");

        for (int i = 0; i < roomButtons.Length; i++)
        {
            Debug.Log($"roomButtons[{i}] is {(roomButtons[i] == null ? "null" : "not null")}");
        }

        if (isOccupied == null)
        {
            Debug.LogError("isOccupied 배열이 null입니다!");
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
            Debug.LogError("ROOM_LIST 메시지 포맷 오류");
            return;
        }

        string data = message.Substring("ROOM_LIST|".Length);
        string[] rooms = data.Split('|', StringSplitOptions.RemoveEmptyEntries);

        Debug.Log($"서버로부터 받은 방 개수: {rooms.Length}");

        // 1) UI 슬롯 모두 초기화
        for (int i = 0; i < roomButtons.Length; i++)
        {
            ResetRoomButton(roomButtons[i]);
            isOccupied[i] = false;
        }

        // 2) 받은 방 리스트를 0번 슬롯부터 순서대로 UI에 세팅
        for (int i = 0; i < rooms.Length && i < roomButtons.Length; i++)
        {
            string[] parts = rooms[i].Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
            {
                Debug.LogWarning("방 정보 포맷 오류: " + rooms[i]);
                continue;
            }

            string roomName = parts[0].Trim();
            string mapName = parts[1].Trim();
            bool hasPassword = parts[2].Trim() == "1";

            roomPasswordMap[roomName] = hasPassword;

            SetRoomButton(roomButtons[i], roomName, GetSpriteForMap(mapName));
            isOccupied[i] = true;

            Debug.Log($"roomButtons[{i}]에 방 '{roomName}' 세팅 완료");
        }
    }

    public void HandleCreateRoomSuccess(string message)
    {
        string[] parts = message.Split('|');
        if (parts.Length < 3) return;

        string roomName = parts[1].Trim();
        string mapName = parts[2].Trim();
        bool isCreator = parts.Length > 3 && parts[3] == "CREATOR";

        // 유저 리스트 파싱
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

        // [추가] 비밀번호 여부 파싱
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
                        characterIndexMap[nick] = 0; // 기본값
                        Debug.LogWarning($"캐릭터 인덱스 파싱 실패: {userParts[1]}");
                    }
                }
                else
                {
                    string nick = entry.Trim();
                    userList.Add(nick);
                    characterIndexMap[nick] = 0; // 기본값
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
        // 메시지 예: ROOM_CREATED|123|Map1|user1,user2|HAS_PASSWORD
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

        Debug.Log($"[ROOM_CREATED] {roomName} | 비번 있음: {hasPassword} | 유저: {userListStr}");
    }


    public void HandleLobbyChatMessage(string message)
    {
        Debug.Log("HandleLobbyChatMessage 호출: " + message);

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
                Debug.LogWarning("LOBBY_CHAT 닉네임/메시지 파싱 오류: " + parts[1]);
            }
        }
        else
        {
            Debug.LogWarning("LOBBY_CHAT 메시지 포맷 오류: " + message);
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

        Debug.Log($"[입장 성공] 방: {roomName}, 유저: {userListStr}");

        // 방 씬으로 전환
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
            Debug.Log($"맵 이미지 세팅 완료: {sprite?.name}");
        }
        else
        {
            Debug.LogWarning("RoomImage Image 컴포넌트 못 찾음");
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

        Debug.Log($"방 클릭됨: {roomName}");

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
            Debug.LogWarning($"roomPasswordMap에 '{roomName}' 키가 없습니다.");
        }

        SendEnterRoom(roomName, ""); // 비밀번호 없이 바로 입장 시도
    }

    private void OnConfirmPasswordClicked()
    {
        string password = EnterpasswordInputField.text.Trim();

        if (string.IsNullOrEmpty(pendingRoomName))
        {
            Debug.LogWarning("입장할 방 이름이 설정되지 않았습니다.");
            return;
        }

        Debug.Log($"비밀번호 입력: {password}, 입장 시도할 방: {pendingRoomName}");
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
            ShowError("방 이름을 입력해주세요.");
            isCreatingRoom = false;
            return;
        }

        if (previewImage == null || previewImage.sprite == null)
        {
            ShowError("맵을 선택해주세요.");
            isCreatingRoom = false;
            return;
        }

        if (passwordToggle.isOn)
        {
            if (passwordInputField == null || string.IsNullOrWhiteSpace(passwordInputField.text))
            {
                ShowError("비밀번호를 입력해주세요.");
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
                ShowError("서버에 연결되어 있지 않습니다.");
                isCreatingRoom = false;
                return;
            }

            byte[] sendBytes = System.Text.Encoding.UTF8.GetBytes(packet);
            await stream.WriteAsync(sendBytes, 0, sendBytes.Length);

            // 성공적으로 보냈으면 isCreatingRoom false는 finally에서 처리
        }
        catch (System.Exception ex)
        {
            ShowError("서버 전송 오류: " + ex.Message);
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

        messageInputField.text = string.Empty; // 입력 필드 초기화

        try
        {
            var stream = NetworkConnector.Instance.Stream;
            string sendStr = $"LOBBY_MESSAGE|{nickname}:{message}\n";
            byte[] sendBytes = Encoding.UTF8.GetBytes(sendStr);
            await stream.WriteAsync(sendBytes, 0, sendBytes.Length);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LobbyChatManager] 채팅 메시지 전송 중 예외 발생: {ex.Message}\n{ex.StackTrace}");
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
                Debug.LogError("닉네임이 설정되지 않았습니다. 로그아웃 메시지를 보낼 수 없습니다.");
                return;
            }

            string logoutMsg = $"LOGOUT|{nickname}\n";
            byte[] quitMsg = Encoding.UTF8.GetBytes(logoutMsg);
            await stream.WriteAsync(quitMsg, 0, quitMsg.Length);
            await stream.FlushAsync();

            if (NetworkConnector.Instance.CurrentUserList.Contains(nickname))
            {
                NetworkConnector.Instance.CurrentUserList.Remove(nickname);
                Debug.Log($"[로그아웃] 유저 제거됨: {nickname}");
            }

            NetworkConnector.Instance.UserNickname = null;

            UnityEngine.SceneManagement.SceneManager.LoadScene("LoginScene");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("로그아웃 중 오류 발생: " + ex.Message);
        }
    }

}
