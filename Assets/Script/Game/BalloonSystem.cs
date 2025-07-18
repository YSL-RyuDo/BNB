using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BalloonSystem : MonoBehaviour
{
    public static BalloonSystem Instance;

    public Button balloonButton;
    public Sprite[] BalloonImageArray;
    private int currentBalloonType = 0;
    public GameObject[] balloonPrefabs; // 0: �⺻, 1: Ư��1, 2: Ư��2
    private bool isCooldown = false;
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void HandleBalloonMessage(string message)
    {
        // ��: BALLOON_LIST|1
        string[] parts = message.Split('|');
        if (parts.Length < 2)
        {
            Debug.LogWarning("[BalloonSystem] �߸��� BALLOON_LIST �޽���");
            return;
        }

        if (int.TryParse(parts[1], out int type))
        {
            if (type < 0 || type >= BalloonImageArray.Length)
            {
                Debug.LogWarning("[BalloonSystem] ǳ�� Ÿ�� �ε��� ���� �ʰ�");
                return;
            }

            currentBalloonType = type;
            Debug.Log($"[BalloonSystem] ���õ� ǳ�� Ÿ��: {currentBalloonType}");

            // ��ư �̹��� ����
            if (balloonButton != null && BalloonImageArray[type] != null)
            {
                var image = balloonButton.GetComponent<UnityEngine.UI.Image>();
                if (image != null)
                {
                    image.sprite = BalloonImageArray[type];
                }
            }
        }
        else
        {
            Debug.LogWarning("[BalloonSystem] ǳ�� Ÿ�� �Ľ� ����");
        }
    }


    public void PlaceBalloonAt(Vector3 worldPosition, int balloonType = 0)
    {
        if (isCooldown)
        {
            Debug.Log("[BalloonSystem] ��Ÿ�� ���̶� ǳ�� ��ġ �Ұ�");
            return;
        }

        int type = currentBalloonType;

        if (type < 0 || type >= balloonPrefabs.Length || balloonPrefabs[type] == null)
        {
            Debug.LogError($"[BalloonSystem] ǳ�� Ÿ�� {type}�� �ش��ϴ� �������� �����ϴ�.");
            return;
        }

        Vector3 spawnPos = new Vector3(
            Mathf.Round(worldPosition.x),
            1f,
            Mathf.Round(worldPosition.z)
        );

        GameObject balloon = Instantiate(balloonPrefabs[type], spawnPos, Quaternion.identity);
        Destroy(balloon, 3f);

        Debug.Log($"[BalloonSystem] ǳ�� Ÿ�� {type} ���� at {spawnPos}");
        StartCoroutine(StartCooldown(3f));
    }

    private IEnumerator StartCooldown(float duration)
    {
        isCooldown = true;

        if (balloonButton != null)
        {
            balloonButton.interactable = false;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float fillAmount = 1f - (elapsed / duration);

            if (balloonButton != null)
            {
                Transform mask = balloonButton.transform.Find("CooldownMask");
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

        if (balloonButton != null)
        {
            balloonButton.interactable = true;

            Transform mask = balloonButton.transform.Find("CooldownMask");
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
