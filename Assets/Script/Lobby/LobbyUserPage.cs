using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUserPage : MonoBehaviour
{
    public GameObject userPageImage;

    public TextMeshProUGUI userNameText;
    public TextMeshProUGUI userLevelText;

    public TextMeshProUGUI userRecordText;
    public TextMeshProUGUI userWinnigRateText;

    public Transform characterRateParent;
    public GameObject characterRatePrefab;
    public Sprite[] CharacterList;


    public Image[] myEmoticonImages;
    public Sprite[] emoticonList;

    public Image myBalloon;
    public Sprite[] balloonList;

    public void SetUserInfoUI(string nickname, int level, int[] equippedEmoIndices, int balloonIndex)
    {
        if (userNameText) userNameText.text = nickname;
        if (userLevelText) userLevelText.text = $"LV. {level}";

        ApplyEquippedEmotes(equippedEmoIndices);
        ApplyBalloon(balloonIndex);
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

        // 扁粮 亲格 瘤快绊 货肺 积己
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
    }

    private void BuildCharacterRateItem(int charIndex, int win, int lose)
    {
        var go = Instantiate(characterRatePrefab, characterRateParent);

        var texts = go.GetComponentsInChildren<TextMeshProUGUI>(true);
        TextMeshProUGUI recordT = null, rateT = null;
        foreach (var t in texts)
        {
            string n = t.name.ToLowerInvariant();
            if (recordT == null && n.Contains("record")) recordT = t;
            else if (rateT == null && (n.Contains("rate") || n.Contains("win"))) rateT = t;
        }

        float rate = (win + lose) > 0 ? (win * 100f / (win + lose)) : 0f;

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
                img.sprite = null;  // 后 沫 贸府
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
}
