using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopReceiver : MonoBehaviour, IMessageHandler
{
    [SerializeField] private ShopUI shopUI;

    private readonly string[] commands =
  {
        "COIN_INFO", "STORE_CHAR_LIST", "STORE_BALLOON_LIST", "STORE_EMO_LIST", "STORE_ICON_LIST"
    };

    private void OnEnable()
    {
        foreach (string command in commands)
        {
            NetworkConnector.Instance.MyPageHandler(command, this);
        }
    }

    private void OnDisable()
    {
        foreach (string command in commands)
        {
            NetworkConnector.Instance.RemoveMyPageHandler(command, this);
        }
    }

    public void HandleMessage(string message)
    {

        string[] parts = message.Split('|');
        string command = message.Split('|')[0];

        switch (command)
        {
            case "COIN_INFO": HandleCoinInfoMessage(message); break;
            case "STORE_CHAR_LIST":
                HandleStoreCharList(message);
                break;

            case "STORE_BALLOON_LIST":
                HandleStoreBalloonList(message);
                break;

            case "STORE_EMO_LIST":
                HandleStoreEmoList(message);
                break;

            case "STORE_ICON_LIST":
                HandleStoreIconList(message);
                break;
        }
    }

    private List<ShopUI.StoreItemData> ParseStoreItemList(string message)
    {
        List<ShopUI.StoreItemData> list = new();

        string[] split = message.Split('|');

        for (int i = 1; i < split.Length; i++)
        {
            string itemStr = split[i];

            if (string.IsNullOrWhiteSpace(itemStr))
                continue;

            if (itemStr == "\n")
                continue;

            string[] data = itemStr.Split(',');
            if (data.Length < 4)
                continue;

            list.Add(new ShopUI.StoreItemData
            {
                index = int.Parse(data[0]),
                owned = data[1] == "1",
                price = int.Parse(data[2]),
                priceType = data[3]
            });
        }

        return list;
    }

    private void HandleStoreCharList(string message)
    {
        var list = ParseStoreItemList(message);
        shopUI.SetCharacterItems(list);
    }

    private void HandleStoreBalloonList(string message)
    {
        var list = ParseStoreItemList(message);
        shopUI.SetBalloonItems(list);
    }

    private void HandleStoreEmoList(string message)
    {
        var list = ParseStoreItemList(message);
        shopUI.SetEmoItems(list);
    }

    private void HandleStoreIconList(string message)
    {
        var list = ParseStoreItemList(message);
        shopUI.SetIconItems(list);
    }

    private void HandleCoinInfoMessage(string message)
    {
        string[] split = message.Split('|');
        if (split.Length < 2)
            return;

        string[] data = split[1].Split(',');
        if (data.Length < 3)
            return;

        int coin0;
        int coin1;

        if (!int.TryParse(data[1], out coin0)) return;
        if (!int.TryParse(data[2], out coin1)) return;

        shopUI.SetCoin(coin0, coin1);
    }
}
