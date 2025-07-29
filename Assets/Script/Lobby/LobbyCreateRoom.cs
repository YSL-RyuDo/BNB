using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LobbyCreateRoom : MonoBehaviour
{
    [SerializeField] private LobbySender lobbySender;

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

    public TextMeshProUGUI errorText;
    public GameObject createRoomPanel;
    private bool isCreatingRoom = false;



    // Start is called before the first frame update
    void Start()
    {
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

        bool success = await lobbySender.SendCreateRoom(roomName, selectedMap, password);

        if (!success)
        {
            ShowError("방 생성 요청을 서버에 보내는 데 실패했습니다.");
            isCreatingRoom = false;
            return;
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
