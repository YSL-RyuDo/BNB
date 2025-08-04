
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class LobbyRoom : MonoBehaviour
{

    public Button[] roomButtons; // ��ư 6��
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
    public TMP_InputField passwordInputField;
    public TMP_InputField roomNameInputField;
    public Button createButton;
    public Button logoutButton;

    public TextMeshProUGUI errorText;
    public GameObject createRoomPanel;
    private bool isCreatingRoom = false;


    public GameObject enterRoomPanel;
    public TMP_InputField EnterpasswordInputField;
    public Button confirmPasswordButton;
    public string pendingRoomName = "";

    public Dictionary<string, bool> roomPasswordMap = new Dictionary<string, bool>();

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

        createButton.onClick.AddListener(OnCreateButtonClicked);
        confirmPasswordButton.onClick.AddListener(OnConfirmPasswordClicked);
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
        Debug.Log("�� ��ư ���� ȣ��");
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

    public void ResetRoomButton(Button btn)
    {
        var roomNameText = btn.transform.Find("RoomNameText")?.GetComponent<TextMeshProUGUI>();
        if (roomNameText != null)
            roomNameText.text = "";

        var roomImage = btn.transform.Find("RoomImage")?.GetComponent<Image>();
        if (roomImage != null)
            roomImage.sprite = null;

        btn.onClick.RemoveAllListeners();
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
        lobbySender.SendEnterRoom(pendingRoomName, password);
    }

    public void OnRoomButtonClicked(string roomName)
    {
        roomName = roomName.Trim();

        Debug.Log($"�� Ŭ����: {roomName}");
        Debug.Log($"[Map ����] roomPasswordMap Keys: {string.Join(", ", roomPasswordMap.Keys)}");

        if (roomPasswordMap.TryGetValue(roomName, out bool hasPassword))
        {
            Debug.Log($"[��й�ȣ ����?] roomName = {roomName}, hasPassword = {hasPassword}");
            Debug.Log($"roomPasswordMap[{roomName}] = {hasPassword}");
            if (hasPassword)
            {
                pendingRoomName = roomName;
                Debug.Log($"[���õ�] pendingRoomName = {pendingRoomName}");
                enterRoomPanel.SetActive(true);
                return;
            }
        }
        else
        {
            Debug.LogWarning($"roomPasswordMap�� '{roomName}' Ű�� �����ϴ�.");
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

}
