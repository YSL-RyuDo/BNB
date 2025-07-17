using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class EmoticonSystem : MonoBehaviour
{
    private string nickname;
    public static EmoticonSystem Instance;
    public Sprite[] emoticonPrefabs;           // 인덱스로 접근할 이모티콘 이미지 배열
    public Button[] emoticonButtons;           // 이모티콘 버튼 4개
    private bool isCooldown = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        nickname = NetworkConnector.Instance.UserNickname;
        // 각 버튼에 클릭 이벤트 연결
        for (int i = 0; i < emoticonButtons.Length; i++)
        {
            int index = i; // 클로저 캡처 방지
            emoticonButtons[i].onClick.AddListener(() => OnEmoticonButtonClicked(index));
        }
    }

    public void HandleEmoticonMessage(string message)
    {
        if (!message.StartsWith("EMO_LIST|")) return;

        string[] parts = message.Substring("EMO_LIST|".Length).Split(',');
        if (parts.Length != 4)
        {
            Debug.LogWarning("[EmoticonSystem] 이모티콘 개수 오류");
            return;
        }

        for (int i = 0; i < 4; i++)
        {
            if (int.TryParse(parts[i], out int emoIndex))
            {
                if (emoIndex >= 0 && emoIndex < emoticonPrefabs.Length)
                {
                    emoticonButtons[i].image.sprite = emoticonPrefabs[emoIndex];

                    // 버튼 이름을 emoIndex로 설정
                    emoticonButtons[i].name = emoIndex.ToString();
                }
                else
                {
                    Debug.LogWarning($"[EmoticonSystem] 잘못된 이모티콘 인덱스: {emoIndex}");
                }
            }
            else
            {
                Debug.LogWarning($"[EmoticonSystem] 이모티콘 파싱 실패: {parts[i]}");
            }
        }
    }

    private async void OnEmoticonButtonClicked(int slotIndex)
    {
        if (isCooldown) return;

        if (string.IsNullOrEmpty(nickname))
        {
            Debug.LogWarning("[EmoticonSystem] 닉네임이 설정되지 않음");
            return;
        }

        string name = emoticonButtons[slotIndex].name;

        // 버튼 이름에서 숫자 추출 (예: "Emoticon_3" → 3)
        string digits = System.Text.RegularExpressions.Regex.Match(name, @"\d+").Value;

        if (string.IsNullOrEmpty(digits))
        {
            Debug.LogWarning("[EmoticonSystem] 버튼 이름에서 숫자 추출 실패");
            return;
        }

        // 전송 메시지 포맷: EMO_CLICK|닉네임|이모티콘인덱스
        string sendMsg = $"EMO_CLICK|{nickname}|{digits}\n";
        byte[] bytes = Encoding.UTF8.GetBytes(sendMsg);

        if (NetworkConnector.Instance != null && NetworkConnector.Instance.Stream != null)
        {
            await NetworkConnector.Instance.Stream.WriteAsync(bytes, 0, bytes.Length);
            Debug.Log($"[EmoticonSystem] 서버로 이모티콘 클릭 전송: {sendMsg.Trim()}");
        }
        StartCoroutine(StartCooldown(3f));
    }

    public void ShowUserEmoticon(string targetNickname, int emoIndex)
    {
        if (emoIndex < 0 || emoIndex >= emoticonPrefabs.Length)
        {
            Debug.LogWarning($"[EmoticonSystem] 잘못된 이모티콘 인덱스: {emoIndex}");
            return;
        }

        // UserInfo_{nickname} GameObject 찾기
        GameObject userInfoObj = GameObject.Find($"UserInfo_{targetNickname}");
        if (userInfoObj == null)
        {
            Debug.LogWarning($"[EmoticonSystem] UserInfo_{targetNickname} 오브젝트를 찾을 수 없음");
            return;
        }

        // 자식 오브젝트 중에서 UseremoticonImage 찾기
        Image emoImage = userInfoObj.transform.Find("UseremoticonImage")?.GetComponent<Image>();
        if (emoImage == null)
        {
            Debug.LogWarning($"[EmoticonSystem] {targetNickname}의 UseremoticonImage를 찾을 수 없음");
            return;
        }

        // 이모티콘 표시
        emoImage.sprite = emoticonPrefabs[emoIndex];
        emoImage.enabled = true;
        emoImage.gameObject.SetActive(true);

        // 3초 뒤 비활성화 및 이미지 제거
        StartCoroutine(HideEmoticonAfterDelay(emoImage, 3f));
    }

    private IEnumerator HideEmoticonAfterDelay(Image image, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (image != null)
        {
            image.sprite = null;
            image.enabled = false;
            image.gameObject.SetActive(false);
        }
    }

    private IEnumerator StartCooldown(float duration)
    {
        isCooldown = true;

        foreach (var btn in emoticonButtons)
        {
            btn.interactable = false;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float fillAmount = 1f - (elapsed / duration);
            foreach (var btn in emoticonButtons)
            {
                Transform mask = btn.transform.Find("CooldownMask");
                if (mask != null)
                {
                    Image maskImg = mask.GetComponent<Image>();
                    if (maskImg != null)
                    {
                        maskImg.fillAmount = fillAmount;
                        maskImg.enabled = true;
                    }
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        foreach (var btn in emoticonButtons)
        {
            btn.interactable = true;
            Transform mask = btn.transform.Find("CooldownMask");
            if (mask != null)
            {
                Image maskImg = mask.GetComponent<Image>();
                if (maskImg != null)
                {
                    maskImg.fillAmount = 0f;
                    maskImg.enabled = false;
                }
            }
        }

        isCooldown = false;
    }
}
