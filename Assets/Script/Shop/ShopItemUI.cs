using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ShopUI;

public class ShopItemUI : MonoBehaviour
{
    public Image itemImage;
    public TextMeshProUGUI priceText;
    public Button button;

    private int index;
    private bool owned;
    private int price;
    private string priceType;

    public void Set(Sprite sprite,
        StoreItemData data,
        System.Action<StoreItemData> onClick)
    {
        index = data.index;
        owned = data.owned;
        price = data.price;
        priceType = data.priceType;

        itemImage.sprite = sprite;

        if (data.owned)
        {
            priceText.text = "º¸À¯Áß";
            button.interactable = false;
            button.onClick.RemoveAllListeners();
        }
        else
        {
            priceText.text = $"{data.price:N0} {data.priceType}";

            button.interactable = true;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(data));
        }
    }
}
