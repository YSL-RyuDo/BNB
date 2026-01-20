using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Xml.Serialization;


public class LobbyRoom : MonoBehaviour
{

    public Button[] roomButtons; // 버튼 6개
    //public bool[] isOccupied;
    public bool[] isOccupied;



    [System.Serializable]
    public class MapInfo
    {
        public string mapName;
        public Sprite previewSprite;
        public Button mapButton;
    }

    public List<MapInfo> mapList = new List<MapInfo>();

    public Image previewImage;

    public Toggle passwordToggle;
    public Toggle teamToggle;
    public TMP_InputField passwordInputField;
    public TMP_InputField roomNameInputField;
    public Button createButton;
    public Button logoutButton;
    public Button myPageButton;
    public Button shopButton;

    public Sprite coopSprite;
    public Sprite passwordSprite;
    public Sprite soloSprite;
    public Sprite nonPasswordSprite;

    public TextMeshProUGUI errorText;
    public GameObject createRoomPanel;
    private bool isCreatingRoom = false;


    public GameObject enterRoomPanel;
    public TMP_InputField EnterpasswordInputField;
    public Button confirmPasswordButton;
    public string pendingRoomName = "";

    private bool isTeamGame = false;

    public Dictionary<string, bool> roomPasswordMap = new Dictionary<string, bool>();
    public Dictionary<string, bool> roomCoopMap = new Dictionary<string, bool>();

    [SerializeField]
    private LobbySender lobbySender;

    private void Awake()
    {

        isOccupied = new bool[roomButtons.Length];
    }


    // Start is called before the first frame update
    private void Start()
    {
        lobbySender.SendGetRoomList();

        foreach (var map in mapList)
        {
            var localMap = map;

            map.mapButton.onClick.AddListener(() =>
            {
                previewImage.sprite = localMap.previewSprite;
                NetworkConnector.Instance.SelectedMap = localMap.mapName;
            });
        }

        passwordToggle.onValueChanged.AddListener(OnPasswordToggleChanged);
        OnPasswordToggleChanged(passwordToggle.isOn);

        myPageButton.onClick.AddListener(LoadMyPageScene);
        shopButton.onClick.AddListener(LoadShopScene);

        createButton.onClick.AddListener(OnCreateButtonClicked);
        confirmPasswordButton.onClick.AddListener(OnConfirmPasswordClicked);

        logoutButton.onClick.AddListener(OnLogoutButtonClicked);
    }

    public void UpdateRoomList(List<LobbyRoomData> rooms)
    {
        for (int i = 0; i < roomButtons.Length; i++)
        {
            ResetRoomButton(roomButtons[i]);
            isOccupied[i] = false;
        }

        for (int i = 0; i < rooms.Count && i < roomButtons.Length; i++)
        {
            var data = rooms[i];
            var sprite = GetSpriteForMap(data.MapName);
            SetRoomButton(roomButtons[i], data.RoomName, sprite);
            isOccupied[i] = true;
        }
    }

    public void SetRoomButton(Button btn, string roomName, Sprite sprite)
    {
        Debug.Log("룸 버튼 세팅 호출");

        bool hasPassword = roomPasswordMap.TryGetValue(roomName, out var hp) && hp;
        bool isCoop = roomCoopMap.TryGetValue(roomName, out var coop) && coop;

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

        var coopImage = btn.transform.Find("CoopImage")?.GetComponent<Image>();
        if (coopImage != null)
        {
            if (roomCoopMap[roomName] == true)
            {
                coopImage.sprite = coopSprite;
            }      
            else
            {
                coopImage.sprite = soloSprite;
            }
        }

        var passwordImage = btn.transform.Find("PasswordImage")?.GetComponent<Image>();
        if (passwordImage != null)
        {
            if(roomPasswordMap[roomName] == true)
            { 
                passwordImage.sprite = passwordSprite; 
            }
            else
            {
                passwordImage.sprite = nonPasswordSprite;
            }
        }

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnRoomButtonClicked(roomName));
        btn.gameObject.SetActive(true);
    }

    public void ResetRoomButton(Button btn)
    {
        var roomNameText = btn.transform.Find("RoomNameText")?.GetComponent<TextMeshProUGUI>();
        if (roomNameText != null)
            roomNameText.text = "";

        var roomImage = btn.transform.Find("RoomImage")?.GetComponent<Image>();
        if (roomImage != null)
            roomImage.sprite = null;

        var coopImage = btn.transform.Find("CoopImage")?.GetComponent<Image>();
        if (coopImage != null)
            coopImage.sprite = null;

        var passwordImage = btn.transform.Find("PasswordImage")?.GetComponent<Image>();
        if (passwordImage != null)
            passwordImage.sprite = null;

        btn.onClick.RemoveAllListeners();
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
        lobbySender.SendEnterRoom(pendingRoomName, password);
    }

    public void OnRoomButtonClicked(string roomName)
    {
        roomName = roomName.Trim();

        Debug.Log($"방 클릭됨: {roomName}");
        Debug.Log($"[Map 상태] roomPasswordMap Keys: {string.Join(", ", roomPasswordMap.Keys)}");

        if (roomPasswordMap.TryGetValue(roomName, out bool hasPassword))
        {
            Debug.Log($"[비밀번호 있음?] roomName = {roomName}, hasPassword = {hasPassword}");
            Debug.Log($"roomPasswordMap[{roomName}] = {hasPassword}");
            if (hasPassword)
            {
                pendingRoomName = roomName;
                Debug.Log($"[세팅됨] pendingRoomName = {pendingRoomName}");
                enterRoomPanel.SetActive(true);
                return;
            }
        }
        else
        {
            Debug.LogWarning($"roomPasswordMap에 '{roomName}' 키가 없습니다.");
        }

        lobbySender.SendEnterRoom(roomName, "");
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

        if (teamToggle.isOn)
        {
            isTeamGame = true;
        }
        else
        {
            isTeamGame = false;
        }

        if (createRoomPanel != null)
        {
            createRoomPanel.SetActive(false);
        }
        string roomName = roomNameInputField.text.Trim();
        string selectedMap = NetworkConnector.Instance.SelectedMap;
        string password = passwordToggle.isOn ? passwordInputField.text.Trim() : "";

        string packet = $"CREATE_ROOM|{roomName}|{selectedMap}|{password}|{isTeamGame}";

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

    private void OnLogoutButtonClicked()
    {
        string nickname = NetworkConnector.Instance.UserNickname;

        if (string.IsNullOrEmpty(nickname))
        {
            Debug.LogWarning("닉네임이 없어 로그아웃을 보낼 수 없습니다.");
            return;
        }

        lobbySender.SendLogout(nickname);

        SceneManager.LoadScene("LoginScene");
    }

    public Sprite GetSpriteForMap(string mapName)
    {
        Sprite sprite = null;
        switch (mapName)
        {
            case "Map1": sprite = mapList[0].previewSprite; break;
            case "Map2": sprite = mapList[1].previewSprite; break;
            case "Map3": sprite = mapList[2].previewSprite; break;
            default: sprite = null; break;
        }
        Debug.Log($"GetSpriteForMap: mapName={mapName}, sprite={(sprite == null ? "null" : sprite.name)}");
        return sprite;
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

    private void LoadMyPageScene()
    {
        SceneManager.LoadScene("MyPageScene");
    }

    private void LoadShopScene()
    {
        SceneManager.LoadScene("ShopScene");
    }
}
