using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class LobbyRoomList : MonoBehaviour
{
    [SerializeField] private LobbySender lobbySender;

    public Button[] roomButtons; // 버튼 6개
    public bool[] isOccupied;

    [SerializeField] private LobbyCreateRoom createRoom;
    [SerializeField] private LobbyRoomEnter roomEnter;

    private void Awake()
    {

        isOccupied = new bool[roomButtons.Length];
    }

    private void Start()
    {

        lobbySender.SendGetRoomList();
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
            var sprite = createRoom.GetSpriteForMap(data.MapName);
            SetRoomButton(roomButtons[i], data.RoomName, sprite);
            isOccupied[i] = true;
        }
    }

    public void SetRoomButton(Button btn, string roomName, Sprite sprite)
    {
        var nameText = btn.transform.Find("RoomNameText")?.GetComponent<TextMeshProUGUI>();
        var image = btn.transform.Find("RoomImage")?.GetComponent<Image>();

        if (nameText != null) nameText.text = roomName;
        if (image != null) image.sprite = sprite;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => roomEnter.OnRoomButtonClicked(roomName));
        btn.gameObject.SetActive(true);
    }

    private void ResetRoomButton(Button btn)
    {
        var nameText = btn.transform.Find("RoomNameText")?.GetComponent<TextMeshProUGUI>();
        var image = btn.transform.Find("RoomImage")?.GetComponent<Image>();

        if (nameText != null) nameText.text = "";
        if (image != null) image.sprite = null;
        btn.onClick.RemoveAllListeners();
    }

    public void AddNewRoomButton(string roomName, string mapName, bool isCreator)
    {
        int index = Array.FindIndex(isOccupied, occupied => !occupied);

        if (index == -1)
        {
            Debug.LogWarning("빈 방 슬롯이 없습니다.");
            return;
        }

        Sprite sprite = createRoom.GetSpriteForMap(mapName);

        SetRoomButton(roomButtons[index], roomName, sprite);

        isOccupied[index] = true;

        Debug.Log($"AddNewRoomButton: {roomName} / {mapName} / Index: {index} / isCreator: {isCreator}");
    }
}
