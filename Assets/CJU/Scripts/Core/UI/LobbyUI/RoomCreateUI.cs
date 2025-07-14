using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

public class RoomCreateUI : MonoBehaviour
{
    public TMP_InputField roomNameInputField;
    public TMP_InputField roomPasswordInputField;

    public Toggle passwordToggle;

    public Button openChooseMapPanelButton;
    public GameObject chooseMapPanel;
    public Button[] mapButtons;

    public Button createRoomButton;
    public GameObject createRoomPanel;
    public Button exitCreateRoomPanelButton;
    public Button exitChooseMapPanelButton;

    private string selectedMap = null;


    // Start is called before the first frame update
    void Start()
    {
        passwordToggle.onValueChanged.AddListener(OnPasswordToggleChanged);
        openChooseMapPanelButton.onClick.AddListener(() => chooseMapPanel.SetActive(true));

        for (int i = 0; i < mapButtons.Length; i++)
        {
            int index = i;
            mapButtons[i].onClick.AddListener(() =>
            {
                selectedMap = $"Map{index + 1}";
                chooseMapPanel.SetActive(false);
                Debug.Log($"[맵 선택됨] {selectedMap}");
            });
        }

        createRoomButton.onClick.AddListener(OnCreateRoomClicked);
        exitCreateRoomPanelButton.onClick.AddListener(() =>{ createRoomPanel.SetActive(false); });
        exitChooseMapPanelButton.onClick.AddListener(() =>{ chooseMapPanel.SetActive(false); });
    }

    private void OnPasswordToggleChanged(bool isOn)
    {
        roomPasswordInputField.interactable = isOn;
        if (!isOn) roomPasswordInputField.text = "";
    }

    private void OnCreateRoomClicked()
    {
        string roomName = roomNameInputField.text.Trim();
        string password = passwordToggle.isOn ? roomNameInputField.text.Trim() : "";

        if (string.IsNullOrEmpty(roomName) || string.IsNullOrEmpty(selectedMap))
        {
            Debug.LogWarning("방 이름과 맵은 반드시 입력되어야 합니다.");
            return;
        }

        string msg = $"CREATE_ROOM|{roomName}|{selectedMap}|{password}\n";
        SendToServer(msg);
        Debug.Log($"[Send] {msg}");
    }

    private async void SendToServer(string msg)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(msg);
        await NetworkConnector.Instance.Stream.WriteAsync(bytes, 0, bytes.Length);
    }
}
