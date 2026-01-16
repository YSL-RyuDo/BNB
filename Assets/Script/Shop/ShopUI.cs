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
    public Button buyButton;

    public TextMeshProUGUI coin0Text;
    public TextMeshProUGUI coin1Text;

    public GameObject[] characterModelings;
    public RawImage characterModelingIamge;
    private GameObject currentCharacterModel;
    public TextMeshProUGUI characterPriceText3D;

    public Image characterImage;
    public TextMeshProUGUI characterPriceText2D;

    public Sprite[] characterImages;
    public Sprite[] balloonImages;
    public Sprite[] emoImages;
    public Sprite[] iconImages;

    public GameObject itemPrefab;
    public Transform itemParent;

    public GameObject background3D;
    public GameObject background2D;
    public GameObject buyTab;
    public Image buyTabImage;

    public Toggle ownedToggle;

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

    private enum ShopTab { Character, Balloon, Emo, Icon}
    private ShopTab currentTab;

    private int selectedIndex = -1;
    private bool selectedOwned;

    // Start is called before the first frame update
    void Start()
    {
        exitButton.onClick.AddListener(() => LoadLobbyScene());

        characterButton.onClick.AddListener(ShowCharacterTab);
        balloonButton.onClick.AddListener(ShowBalloonTab);
        emoButton.onClick.AddListener(ShowEmoTab);
        iconButton.onClick.AddListener(ShowIconTab);
        buyButton.onClick.AddListener(BuyItem);

        shopSender.SendGetCoin(NetworkConnector.Instance.UserNickname);
        shopSender.SendGetStoreCharList(NetworkConnector.Instance.UserNickname);
        shopSender.SendGetStoreBalloonList(NetworkConnector.Instance.UserNickname);
        shopSender.SendGetStoreEmoList(NetworkConnector.Instance.UserNickname);
        shopSender.SendGetStoreIconList(NetworkConnector.Instance.UserNickname);

        ownedToggle.onValueChanged.AddListener(OnOwnedToggleChanged);

        currentTab = ShopTab.Character;
    }


    public void SetCoin(int coin0, int coin1)
    {
        coin0Text.text = $"coin0: {coin0:N0}";
        coin1Text.text = $"coin1: {coin1:N0}";
    }

    private void ResetSelection()
    {
        selectedIndex = -1;
        selectedOwned = false;

        buyButton.interactable = false;
    }

    private void ShowCharacterTab()
    {
        currentTab = ShopTab.Character;
        ResetSelection();

        ClearItems();

        foreach (var data in characterItems)
        {
            if (ownedToggle.isOn && data.owned)
            {
                continue; 
            }
            GameObject go = Instantiate(itemPrefab, itemParent);
            ShopItemUI ui = go.GetComponent<ShopItemUI>();

            ui.Set(
                characterImages[data.index],
                data,
                OnCharacterItemClicked
            );
        }
    }

    private void ShowBalloonTab()
    {
        currentTab = ShopTab.Balloon;
        ResetSelection();
        ShowItems(balloonItems, balloonImages, OnNonCharacterItemClicked);
    }

    private void ShowEmoTab()
    {
        currentTab = ShopTab.Emo;
        ResetSelection();
        ShowItems(emoItems, emoImages, OnNonCharacterItemClicked);
    }

    private void ShowIconTab()
    {
        currentTab = ShopTab.Icon;
        ResetSelection();
        ShowItems(iconItems, iconImages, OnNonCharacterItemClicked);
    }

    private void ShowItems(List<StoreItemData> list, Sprite[] sprites, System.Action<StoreItemData> onClick)
    {
        ClearItems();

        foreach (var data in list)
        {
            if (ownedToggle.isOn && data.owned)
            {
                continue;
            }

            GameObject go = Instantiate(itemPrefab, itemParent);
            ShopItemUI ui = go.GetComponent<ShopItemUI>();

            ui.Set(
                sprites[data.index],
                data,
                onClick
            );
        }
    }

    private void OnCharacterItemClicked(StoreItemData data)
    {
        selectedIndex = data.index;
        selectedOwned = data.owned;
        buyButton.interactable = !data.owned;

        characterImage.sprite = characterImages[data.index];

        background3D.SetActive(true);

        characterPriceText2D.text = data.owned
            ? "보유중"
            : $"{data.price:N0} {data.priceType}";


        characterPriceText3D.text = data.owned
                  ? "보유중"
                  : $"{data.price:N0} {data.priceType}";

        SpawnCharacterModel(data.index);

        buyTabImage.sprite = characterImages[data.index];
    }

    private void OnNonCharacterItemClicked(StoreItemData data)
    {
        selectedIndex = data.index;
        selectedOwned = data.owned;
        buyButton.interactable = !data.owned;

        Sprite preview = null;

        switch (currentTab)
        {
            case ShopTab.Icon:
                preview = iconImages[data.index] ;
                break;
            case ShopTab.Emo:
                preview = emoImages[data.index];
                break;
            case ShopTab.Balloon:
                preview = balloonImages[data.index];
                break;
        }

        buyTabImage.sprite = preview;

        buyTab.SetActive(true);
    }


    private void SpawnCharacterModel(int index)
    {
        if (currentCharacterModel != null)
            Destroy(currentCharacterModel);

        currentCharacterModel = Instantiate(
            characterModelings[index],
            Vector3.zero,
            Quaternion.Euler(0, 180f, 0)
        );
    }

    public void SetCharacterItems(List<StoreItemData> list)
    {
        characterItems = list;
        ShowCharacterTab();
    }
    public void SetBalloonItems(List<StoreItemData> list)
    {
        balloonItems = list;

        if (currentTab == ShopTab.Balloon)
        {
            ShowBalloonTab();
        }
    }

    public void SetEmoItems(List<StoreItemData> list)
    {
        emoItems = list;

        if (currentTab == ShopTab.Emo)
        {
            ShowEmoTab();
        }
    }

    public void SetIconItems(List<StoreItemData> list)
    {
        iconItems = list;

        if (currentTab == ShopTab.Icon)
        {
            ShowIconTab();
        }
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

    private void BuyItem()
    {
        if (selectedIndex < 0) return;
        if (selectedOwned) return;

        string nick = NetworkConnector.Instance.UserNickname;

        switch (currentTab)
        {
            case ShopTab.Character:
                shopSender.SendBuyChar(nick, selectedIndex);
                break;

            case ShopTab.Balloon:
                shopSender.SendBuyBalloon(nick, selectedIndex);
                break;

            case ShopTab.Emo:
                shopSender.SendBuyEmo(nick, selectedIndex);
                break;

            case ShopTab.Icon:
                shopSender.SendBuyIcon(nick, selectedIndex);
                break;
        }

        buyTab.SetActive(false);
        background2D.SetActive(false);
        background3D.SetActive(false);  

        buyButton.interactable = false;
    }

    private void OnOwnedToggleChanged(bool isOn)
    {
        ResetSelection();

        switch (currentTab)
        {
            case ShopTab.Character: ShowCharacterTab(); break;
            case ShopTab.Balloon: ShowBalloonTab(); break;
            case ShopTab.Emo: ShowEmoTab(); break;
            case ShopTab.Icon: ShowIconTab(); break;
        }
    }

}
