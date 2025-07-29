using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyRoomEnter : MonoBehaviour
{
    [SerializeField] private LobbySender lobbySender;


    public GameObject enterRoomPanel;
    public TMP_InputField EnterpasswordInputField;
    public Button confirmPasswordButton;
    private string pendingRoomName = "";

    public Dictionary<string, bool> roomPasswordMap = new();

    void Start()
    {
        confirmPasswordButton.onClick.AddListener(OnConfirmPasswordClicked);
    }

    public void SetRoomPasswordMap(Dictionary<string, bool> map)
    {
        roomPasswordMap = map;
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

        lobbySender.SendEnterRoom(roomName, " ");
    }

}
