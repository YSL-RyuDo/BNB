using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.EventSystems;

public class ShopUI : MonoBehaviour
{
    [SerializeField] private ShopSender shopSender;

    public Button exitButton;

    public Button characterButton;
    public Button balloonButton;
    public Button emoButton;
    public Button iconButton;

    public TextMeshProUGUI coin0Text;
    public TextMeshProUGUI coin1Text;

    public Sprite[] characterImages;
    public Sprite[] balloonImages;
    public Sprite[] emoImages;
    public Sprite[] iconImages;

    public GameObject itemPrefab;
    public Transform itemParent;

    public struct StoreItemData
    {
        public int index;
        public bool owned;
        public int price;
        public string priceType;
    }

    private List<StoreItemData> characterItems = new();
    private List<StoreItemData> balloonItems = new();
    private List<StoreItemData> emoItems = new();
    private List<StoreItemData> iconItems = new();

    // Start is called before the first frame update
    void Start()
    {
        exitButton.onClick.AddListener(() => LoadLobbyScene());

        characterButton.onClick.AddListener(ShowCharacterTab);
        balloonButton.onClick.AddListener(ShowBalloonTab);
        emoButton.onClick.AddListener(ShowEmoTab);
        iconButton.onClick.AddListener(ShowIconTab);

        shopSender.SendGetCoin(NetworkConnector.Instance.UserNickname);
        shopSender.SendGetStoreCharList(NetworkConnector.Instance.UserNickname);
        shopSender.SendGetStoreBalloonList(NetworkConnector.Instance.UserNickname);
        shopSender.SendGetStoreEmoList(NetworkConnector.Instance.UserNickname);
        shopSender.SendGetStoreIconList(NetworkConnector.Instance.UserNickname);
    }


    public void SetCoin(int coin0, int coin1)
    {
        coin0Text.text = $"coin0: {coin0:N0}";
        coin1Text.text = $"coin1: {coin1:N0}";
    }

    private void ShowCharacterTab()
    {
        ShowItems(characterItems, characterImages);
    }

    private void ShowBalloonTab()
    {
        ShowItems(balloonItems, balloonImages);
    }

    private void ShowEmoTab()
    {
        ShowItems(emoItems, emoImages);
    }

    private void ShowIconTab()
    {
        ShowItems(iconItems, iconImages);
    }

    private void ShowItems(
    List<StoreItemData> list,
    Sprite[] sprites
)
    {
        ClearItems();

        foreach (var data in list)
        {
            GameObject go = Instantiate(itemPrefab, itemParent);

            ShopItemUI itemUI = go.GetComponent<ShopItemUI>();
            itemUI.Set(
                sprites[data.index],
                data.owned,
                data.price,
                data.priceType
            );
        }
    }

    public void SetCharacterItems(List<StoreItemData> list)
    {
        characterItems = list;
        ShowCharacterTab();
    }

    public void SetBalloonItems(List<StoreItemData> list)
    {
        balloonItems = list;
    }

    public void SetEmoItems(List<StoreItemData> list)
    {
        emoItems = list;
    }

    public void SetIconItems(List<StoreItemData> list)
    {
        iconItems = list;
    }

    private void ClearItems()
    {
        foreach (Transform child in itemParent)
        {
            Destroy(child.gameObject);
        }
    }
    private void LoadLobbyScene()
    {
        SceneManager.LoadScene("LobbyScene");
    }
}
