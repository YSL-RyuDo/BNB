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

    public void SetUserInfoUI(string nickname, int level)
    {
        if (userNameText) userNameText.text = nickname;
        if (userLevelText) userLevelText.text = $"LV. {level}";
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

        // 기존 항목 지우고 새로 생성
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
}
