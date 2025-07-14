using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoomButtonUI : MonoBehaviour
{
    public Image roomImage;
    public TextMeshProUGUI roomNameText;
    public Button joinButton;

    public void Clear()
    {
        roomImage.sprite = null;
        roomNameText.text = null;
        joinButton.onClick.RemoveAllListeners();
    }

    public void SetInfo(string roomName, UnityEngine.Events.UnityAction onClick)
    {
        roomNameText.text = roomName;

        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(onClick);
    }
}
