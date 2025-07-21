using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Text;
using System.Collections.Generic;

public class BalloonSystem : MonoBehaviour
{
    public static BalloonSystem Instance;

    public Button balloonButton;
    public Sprite[] BalloonImageArray;
    private int currentBalloonType = 0;
    public GameObject[] balloonPrefabs; // 0: �⺻, 1: Ư��1, 2: Ư��2
    public GameObject[] waterPrefabs;
    private bool isCooldown = false;
    HashSet<string> hitPlayers = new HashSet<string>(); // �ߺ� ������
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public int GetCurrentBalloonType()
    {
        return currentBalloonType;
    }

    public bool CanPlaceBalloon()
    {
        return !isCooldown;
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

    public void HandleBalloonResult(string message)
    {
        // ��: PLACE_BALLOON_RESULT|�г���|x,z|Ÿ��
        string[] parts = message.Split('|');
        if (parts.Length < 4)
        {
            Debug.LogWarning("[BalloonSystem] PLACE_BALLOON_RESULT ���� ����");
            return;
        }

        string username = parts[1].Trim();
        string[] coordParts = parts[2].Split(',');

        if (coordParts.Length != 2 || !float.TryParse(coordParts[0], out float x) || !float.TryParse(coordParts[1], out float z))
        {
            Debug.LogWarning("[BalloonSystem] ��ǥ �Ľ� ����");
            return;
        }

        if (!int.TryParse(parts[3], out int type))
        {
            Debug.LogWarning("[BalloonSystem] ǳ�� Ÿ�� �Ľ� ����");
            return;
        }

        Vector3 spawnPos = new Vector3(Mathf.Round(x), 1f, Mathf.Round(z));
        if (type < 0 || type >= balloonPrefabs.Length || balloonPrefabs[type] == null)
        {
            Debug.LogWarning($"[BalloonSystem] ǳ�� Ÿ�� {type} �߸���");
            return;
        }

        GameObject balloon = Instantiate(balloonPrefabs[type], spawnPos, Quaternion.identity);
        balloon.tag = "Balloon";
        Debug.Log($"[BalloonSystem] {username} �� ǳ�� ��ġ��: Ÿ�� {type} at {spawnPos}");

        if (username == NetworkConnector.Instance.UserNickname)
        {
            StartCoroutine(StartCooldown(3f));

            GameObject localPlayer = GameObject.Find($"Character_{username}");
            if (localPlayer != null)
            {
                Collider playerCol = localPlayer.GetComponent<Collider>();
                Collider balloonCol = balloon.GetComponent<Collider>();
                if (playerCol != null && balloonCol != null)
                {
                    // �ϴ� ������ �浹 ���� ���·� ����
                    Physics.IgnoreCollision(playerCol, balloonCol, true);

                    // �浹 ���¸� ���������� �����ϴ� �ڷ�ƾ ����
                    StartCoroutine(MonitorCollision(playerCol, balloonCol));
                }
            }
        }

        StartCoroutine(RemoveBalloonAfterDelay(balloon, spawnPos, username, 3f, type));
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

    private IEnumerator MonitorCollision(Collider playerCol, Collider balloonCol)
    {
        while (playerCol != null && balloonCol != null)
        {
            bool overlapping = IsOverlapping(playerCol, balloonCol);

            if (!overlapping)
            {
                // �������� ������ �浹 Ȱ��ȭ�ϰ� �ڷ�ƾ ����
                Physics.IgnoreCollision(playerCol, balloonCol, false);
                yield break;
            }
            else
            {
                // ���������� ��� ����
                Physics.IgnoreCollision(playerCol, balloonCol, true);
            }

            yield return new WaitForSeconds(0.1f);
        }

        // Ȥ�� �ݶ��̴��� null�� �Ǹ� �浹 ���� ����
        if (playerCol != null && balloonCol != null)
            Physics.IgnoreCollision(playerCol, balloonCol, false);
    }


    private bool IsOverlapping(Collider colA, Collider colB)
    {
        return Physics.ComputePenetration(
            colA, colA.transform.position, colA.transform.rotation,
            colB, colB.transform.position, colB.transform.rotation,
            out Vector3 direction, out float distance);
    }


    private IEnumerator EnableCollisionAfterDelay(Collider col1, Collider col2, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (col1 != null && col2 != null)
        {
            Physics.IgnoreCollision(col1, col2, false);
        }
    }

    private IEnumerator RemoveBalloonAfterDelay(GameObject balloon, Vector3 pos, string username, float delay, int type)
    {
        yield return new WaitForSeconds(delay);
        // ���� ǳ���� ������ ���� ��û ����
        if (username == NetworkConnector.Instance.UserNickname)
        {
            string msg = $"REMOVE_BALLOON|{username}|{pos.x:F2},{pos.z:F2}|{type}\n";
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(msg);
            try
            {
                NetworkConnector.Instance.Stream.WriteAsync(bytes, 0, bytes.Length);
                Debug.Log($"[BalloonSystem] ǳ�� ���� ��Ŷ ���۵�: {msg.Trim()}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BalloonSystem] ǳ�� ���� ��Ŷ ���� ����: {e.Message}");
            }
        }
    }


    public void HandleRemoveBalloon(string message)
    {
        // ���� �޽���: REMOVE_BALLOON|�г���|x,z
        string[] parts = message.Split('|');
        if (parts.Length < 3)
        {
            Debug.LogWarning("[BalloonSystem] REMOVE_BALLOON �޽��� ���� ����");
            return;
        }

        string nickname = parts[1].Trim();
        string[] coords = parts[2].Split(',');

        if (coords.Length != 2 || !float.TryParse(coords[0], out float x) || !float.TryParse(coords[1], out float z))
        {
            Debug.LogWarning("[BalloonSystem] REMOVE_BALLOON ��ǥ �Ľ� ����");
            return;
        }

        Vector3 pos = new Vector3(Mathf.Round(x), 1f, Mathf.Round(z));

        // �� ���� �����ϴ� ǳ���� ã�� ����
        foreach (GameObject balloon in GameObject.FindGameObjectsWithTag("Balloon"))
        {
            Vector3 balloonPos = balloon.transform.position;
            if (Mathf.Approximately(balloonPos.x, pos.x) && Mathf.Approximately(balloonPos.z, pos.z))
            {
                Destroy(balloon);
                Debug.Log($"[BalloonSystem] {nickname}�� ǳ�� ���ŵ� at {pos}");
                return;
            }
        }

        Debug.LogWarning($"[BalloonSystem] ���� ��� ǳ���� ã�� ����: {pos}");
    }

    public void HandleWaterSpread(string message)
    {
        // �޽��� ���� üũ
        string[] parts = message.Split('|');
        if (parts.Length < 5)
        {
            Debug.LogWarning("[BalloonSystem] WATER_SPREAD �޽��� ���� ����");
            return;
        }

        string nickname = parts[1];
        string typeStr = parts[3];
        string positionsStr = parts[4];

        if (!int.TryParse(typeStr, out int waterType))
        {
            Debug.LogWarning("[BalloonSystem] �� Ÿ�� �Ľ� ����");
            return;
        }

        List<Vector3> waterPositions = new List<Vector3>();

        string[] positions = positionsStr.Split(';');
        foreach (var posStr in positions)
        {
            string[] coord = posStr.Split(',');
            if (coord.Length != 2) continue;

            if (float.TryParse(coord[0], out float x) && float.TryParse(coord[1], out float z))
            {
                Vector3 spawnPos = new Vector3(Mathf.Round(x), 1f, Mathf.Round(z));
                waterPositions.Add(spawnPos);

                // ����Ʈ ����
                if (waterPrefabs != null && waterType >= 0 && waterType < waterPrefabs.Length && waterPrefabs[waterType] != null)
                {
                    GameObject waterEffect = Instantiate(waterPrefabs[waterType], spawnPos, Quaternion.identity);
                    Destroy(waterEffect, 2f);
                }
            }
        }
    }

}
