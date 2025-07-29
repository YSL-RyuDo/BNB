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

        lobbySender.SendEnterRoom(roomName, " ");
    }

}
