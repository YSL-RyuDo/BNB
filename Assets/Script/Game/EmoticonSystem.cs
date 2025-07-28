using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class EmoticonSystem : MonoBehaviour
{
    private string nickname;
    public static EmoticonSystem Instance;
    public Sprite[] emoticonPrefabs;           // �ε����� ������ �̸�Ƽ�� �̹��� �迭
    public Button[] emoticonButtons;           // �̸�Ƽ�� ��ư 4��
    private bool isCooldown = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        nickname = NetworkConnector.Instance.UserNickname;
        // �� ��ư�� Ŭ�� �̺�Ʈ ����
        for (int i = 0; i < emoticonButtons.Length; i++)
        {
            int index = i; // Ŭ���� ĸó ����
            emoticonButtons[i].onClick.AddListener(() => OnEmoticonButtonClicked(index));
        }
    }

    public void HandleEmoticonMessage(string message)
    {
        if (!message.StartsWith("EMO_LIST|")) return;

        string[] parts = message.Substring("EMO_LIST|".Length).Split(',');
        if (parts.Length != 4)
        {
            Debug.LogWarning("[EmoticonSystem] �̸�Ƽ�� ���� ����");
            return;
        }

        for (int i = 0; i < 4; i++)
        {
            if (int.TryParse(parts[i], out int emoIndex))
            {
                if (emoIndex >= 0 && emoIndex < emoticonPrefabs.Length)
                {
                    emoticonButtons[i].image.sprite = emoticonPrefabs[emoIndex];

                    // ��ư �̸��� emoIndex�� ����
                    emoticonButtons[i].name = emoIndex.ToString();
                }
                else
                {
                    Debug.LogWarning($"[EmoticonSystem] �߸��� �̸�Ƽ�� �ε���: {emoIndex}");
                }
            }
            else
            {
                Debug.LogWarning($"[EmoticonSystem] �̸�Ƽ�� �Ľ� ����: {parts[i]}");
            }
        }
    }

    private async void OnEmoticonButtonClicked(int slotIndex)
    {
        if (isCooldown) return;

        if (string.IsNullOrEmpty(nickname))
        {
            Debug.LogWarning("[EmoticonSystem] �г����� �������� ����");
            return;
        }

        string name = emoticonButtons[slotIndex].name;

        // ��ư �̸����� ���� ���� (��: "Emoticon_3" �� 3)
        string digits = System.Text.RegularExpressions.Regex.Match(name, @"\d+").Value;

        if (string.IsNullOrEmpty(digits))
        {
            Debug.LogWarning("[EmoticonSystem] ��ư �̸����� ���� ���� ����");
            return;
        }

        // ���� �޽��� ����: EMO_CLICK|�г���|�̸�Ƽ���ε���
        string sendMsg = $"EMO_CLICK|{nickname}|{digits}\n";
        byte[] bytes = Encoding.UTF8.GetBytes(sendMsg);

        if (NetworkConnector.Instance != null && NetworkConnector.Instance.Stream != null)
        {
            await NetworkConnector.Instance.Stream.WriteAsync(bytes, 0, bytes.Length);
            Debug.Log($"[EmoticonSystem] ������ �̸�Ƽ�� Ŭ�� ����: {sendMsg.Trim()}");
        }
        StartCoroutine(StartCooldown(3f));
    }

    public void ShowUserEmoticon(string targetNickname, int emoIndex)
    {
        if (emoIndex < 0 || emoIndex >= emoticonPrefabs.Length)
        {
            Debug.LogWarning($"[EmoticonSystem] �߸��� �̸�Ƽ�� �ε���: {emoIndex}");
            return;
        }

        // UserInfo_{nickname} GameObject ã��
        GameObject userInfoObj = GameObject.Find($"UserInfo_{targetNickname}");
        if (userInfoObj == null)
        {
            Debug.LogWarning($"[EmoticonSystem] UserInfo_{targetNickname} ������Ʈ�� ã�� �� ����");
            return;
        }

        // �ڽ� ������Ʈ �߿��� UseremoticonImage ã��
        Image emoImage = userInfoObj.transform.Find("UseremoticonImage")?.GetComponent<Image>();
        if (emoImage == null)
        {
            Debug.LogWarning($"[EmoticonSystem] {targetNickname}�� UseremoticonImage�� ã�� �� ����");
            return;
        }

        // �̸�Ƽ�� ǥ��
        emoImage.sprite = emoticonPrefabs[emoIndex];
        emoImage.enabled = true;
        emoImage.gameObject.SetActive(true);

        // 3�� �� ��Ȱ��ȭ �� �̹��� ����
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
