using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoomButtonUI : MonoBehaviour
{
    public Image roomImage;
    public TextMeshProUGUI roomNameText;

    public void SetInfo(string roomName, Sprite thumbnail)
    {
        roomNameText.text = roomName;
        if (thumbnail != null)
            roomImage.sprite = thumbnail;
    }
}
