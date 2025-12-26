using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemUI : MonoBehaviour
{
    public Image itemImage;
    public TextMeshProUGUI priceText;

    public void Set(Sprite sprite, bool owned, int price, string priceType)
    {
        itemImage.sprite = sprite;

        if (owned)
        {
            priceText.text = "º¸À¯Áß";
        }
        else
        {
            priceText.text = $"{price:N0} {priceType}";
        }
    }
}
