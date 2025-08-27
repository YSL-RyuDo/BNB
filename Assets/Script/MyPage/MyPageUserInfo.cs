using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class MyPageUserInfo : MonoBehaviour
{
    [SerializeField] private MyPageSender myPageSender;

    public TextMeshProUGUI userNameText;
    public TextMeshProUGUI userLevelText;
    public Slider userExpBar;
    public TextMeshProUGUI userExpText;

    public Image[] myEmoticonImages;
    public Sprite[] emoticonList;
    public Transform emoticonSettingParent;
    public GameObject emoticonSettingButtonPrefab;

    public Image myBalloon;
    public Sprite[] balloonList;

    public TextMeshProUGUI userRecordText;
    public TextMeshProUGUI userWinnigRateText;

    public Transform characterRateParent;
    public GameObject characterRatePrefab;

    public GameObject[] characterModelings;
    public Sprite[] CharacterList;
    public RawImage characterModelingIamge;

    private GameObject spawnedModel;
    
    public Button exitButton;
    private float maxExp = 100;

    private void Start()
    {
        userExpBar.interactable = false;
        myPageSender.SendGetInfo(NetworkConnector.Instance.UserNickname);
        exitButton.onClick.AddListener(() => LoadLobbyScene());
    }

    public void SetUserInfoUI(string nickname, int level, float exp, int[] equippedEmoIndices, int balloonIndex)
    {
        ApplyBasicInfo(nickname, level, exp);
        ApplyEquippedEmotes(equippedEmoIndices);
        ApplyBalloon(balloonIndex);
    }

    private void ApplyBasicInfo(string nickname, int level, float exp)
    {
        if (userNameText) userNameText.text = nickname;
        if (userLevelText) userLevelText.text = $"LV. {level}";

        maxExp = Mathf.Max(1f, level * 100f);
        float currentExp = Mathf.Clamp(exp, 0f, maxExp);
        float percent = (maxExp > 0f) ? (currentExp / maxExp) * 100f : 0f;

        if (userExpBar)
        {
            userExpBar.maxValue = maxExp;
            userExpBar.value = currentExp;
        }
        if (userExpText) userExpText.text = $"{percent:0.0}%";
    }

    private void ApplyEquippedEmotes(int[] indices)
    {
        if (myEmoticonImages == null) return;

        for (int i = 0; i < myEmoticonImages.Length; i++)
        {
            var img = myEmoticonImages[i];
            if (img == null) continue;

            bool valid = indices != null
                      && i < indices.Length
                      && indices[i] >= 0
                      && emoticonList != null
                      && indices[i] < emoticonList.Length;

            if (valid)
            {
                img.sprite = emoticonList[indices[i]];
                img.enabled = true;
            }
            else
            {
                img.sprite = null;  // 빈 칸 처리
                img.enabled = false;
            }
        }
    }

    private void ApplyBalloon(int idx)
    {
        if (myBalloon == null) return;

        bool valid = idx >= 0 && balloonList != null && idx < balloonList.Length;
        if (valid)
        {
            myBalloon.sprite = balloonList[idx];
            myBalloon.enabled = true;
        }
        else
        {
            myBalloon.sprite = null;
            myBalloon.enabled = false;
        }
    }

    public void SetWinRateUI(string nickname, int totalWin, int totalLose, List<int[]> top3Triples)
    {
        if (userRecordText) userRecordText.text = $"{totalWin}W {totalLose}L";
        if (userWinnigRateText)
        {
            int tot = totalWin + totalLose;
            float rate = tot > 0 ? (totalWin * 100f / tot) : 0f;
            userWinnigRateText.text = rate.ToString("0.0", CultureInfo.InvariantCulture) + "%";
        }

        if (characterRateParent && characterRatePrefab)
        {
            for (int i = characterRateParent.childCount - 1; i >= 0; i--)
                Destroy(characterRateParent.GetChild(i).gameObject);

            if (top3Triples != null)
            {
                for (int i = 0; i < top3Triples.Count && i < 3; i++)
                {
                    var triple = top3Triples[i];
                    if (triple == null || triple.Length < 3) continue;
                    BuildCharacterRateItem(triple[0], triple[1], triple[2]);
                }
            }
        }

        if (top3Triples != null && top3Triples.Count > 0)
        {
            int topCharIndex = top3Triples[0][0];
            SpawnModelAtOrigin(topCharIndex);
        }
    }

    private void BuildCharacterRateItem(int charIndex, int win, int lose)
    {
        var go = Instantiate(characterRatePrefab, characterRateParent);

        var texts = go.GetComponentsInChildren<TextMeshProUGUI>(true);
        TextMeshProUGUI nameT = null, recordT = null, rateT = null;
        foreach (var t in texts)
        {
            string n = t.name.ToLowerInvariant();
            if (nameT == null && (n.Contains("name") || n.Contains("char") || n.Contains("index"))) nameT = t;
            else if (recordT == null && n.Contains("record")) recordT = t;
            else if (rateT == null && (n.Contains("rate") || n.Contains("win"))) rateT = t;
        }
        if (nameT == null && texts.Length > 0) nameT = texts[0];
        if (recordT == null && texts.Length > 1) recordT = texts[1];
        if (rateT == null && texts.Length > 2) rateT = texts[2];

        float rate = (win + lose) > 0 ? (win * 100f / (win + lose)) : 0f;

        if (nameT) nameT.text = $"Char {charIndex}";
        if (recordT) recordT.text = $"{win}W {lose}L";
        if (rateT) rateT.text = rate.ToString("0.0", CultureInfo.InvariantCulture) + "%";

        TrySetCharacterIcon(go, charIndex);
    }

    private void TrySetCharacterIcon(GameObject item, int charIndex)
    {
        if (CharacterList == null || charIndex < 0 || charIndex >= CharacterList.Length) return;

        Image target = null;
        var images = item.GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            string n = img.name.ToLowerInvariant();
            if (n.Contains("icon") || n.Contains("character"))
            {
                target = img;
                break;
            }
        }
        if (target == null && images.Length > 0) target = images[0];

        if (target != null)
        {
            target.sprite = CharacterList[charIndex];
            target.enabled = (target.sprite != null);
        }
    }
    private void SpawnModelAtOrigin(int charIndex)
    {
        if (spawnedModel != null) Destroy(spawnedModel);

        if (characterModelings == null ||
            charIndex < 0 || charIndex >= characterModelings.Length ||
            characterModelings[charIndex] == null)
        {
            return;
        }

        spawnedModel = Instantiate(characterModelings[charIndex],
                                       Vector3.zero, Quaternion.Euler(0f, 180f, 0f));
    }

    public void BuildOwnedEmoticonButtons(List<int> ownedEmoteIndexes)
    {
        if (emoticonSettingParent == null || emoticonSettingButtonPrefab == null)
        {
            Debug.LogWarning("[EmoteSetting] Parent 또는 Prefab이 연결되지 않았습니다.");
            return;
        }

        for (int i = emoticonSettingParent.childCount - 1; i >= 0; i--)
            Destroy(emoticonSettingParent.GetChild(i).gameObject);

        if (ownedEmoteIndexes == null || ownedEmoteIndexes.Count == 0)
            return;

        ownedEmoteIndexes.Sort();

        foreach (int idx in ownedEmoteIndexes)
        {
            if (idx < 0 || emoticonList == null || idx >= emoticonList.Length)
                continue;

            var sprite = emoticonList[idx];
            if (sprite == null) continue;

            var go = Instantiate(emoticonSettingButtonPrefab, emoticonSettingParent);
            go.name = $"EmoteBtn_{idx}";

            Image icon = null;
            var images = go.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                string n = img.name.ToLowerInvariant();
                if (n.Contains("icon"))
                {
                    icon = img;
                    break;
                }
            }
            if (icon == null && images.Length > 0) icon = images[0];

            if (icon != null)
            {
                icon.sprite = sprite;
                icon.enabled = true;
                icon.preserveAspect = true;
            }
        }
    }

    private void LoadLobbyScene()
    {
        SceneManager.LoadScene("LobbyScene");
    }
}
