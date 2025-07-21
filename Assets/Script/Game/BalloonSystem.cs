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
    public GameObject[] balloonPrefabs; // 0: 기본, 1: 특수1, 2: 특수2
    public GameObject[] waterPrefabs;
    private bool isCooldown = false;
    HashSet<string> hitPlayers = new HashSet<string>(); // 중복 방지용
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
        // 예: BALLOON_LIST|1
        string[] parts = message.Split('|');
        if (parts.Length < 2)
        {
            Debug.LogWarning("[BalloonSystem] 잘못된 BALLOON_LIST 메시지");
            return;
        }

        if (int.TryParse(parts[1], out int type))
        {
            if (type < 0 || type >= BalloonImageArray.Length)
            {
                Debug.LogWarning("[BalloonSystem] 풍선 타입 인덱스 범위 초과");
                return;
            }

            currentBalloonType = type;
            Debug.Log($"[BalloonSystem] 선택된 풍선 타입: {currentBalloonType}");

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
            Debug.LogWarning("[BalloonSystem] 풍선 타입 파싱 실패");
        }
    }

    public void HandleBalloonResult(string message)
    {
        // 예: PLACE_BALLOON_RESULT|닉네임|x,z|타입
        string[] parts = message.Split('|');
        if (parts.Length < 4)
        {
            Debug.LogWarning("[BalloonSystem] PLACE_BALLOON_RESULT 형식 오류");
            return;
        }

        string username = parts[1].Trim();
        string[] coordParts = parts[2].Split(',');

        if (coordParts.Length != 2 || !float.TryParse(coordParts[0], out float x) || !float.TryParse(coordParts[1], out float z))
        {
            Debug.LogWarning("[BalloonSystem] 좌표 파싱 실패");
            return;
        }

        if (!int.TryParse(parts[3], out int type))
        {
            Debug.LogWarning("[BalloonSystem] 풍선 타입 파싱 실패");
            return;
        }

        Vector3 spawnPos = new Vector3(Mathf.Round(x), 1f, Mathf.Round(z));
        if (type < 0 || type >= balloonPrefabs.Length || balloonPrefabs[type] == null)
        {
            Debug.LogWarning($"[BalloonSystem] 풍선 타입 {type} 잘못됨");
            return;
        }

        GameObject balloon = Instantiate(balloonPrefabs[type], spawnPos, Quaternion.identity);
        balloon.tag = "Balloon";
        Debug.Log($"[BalloonSystem] {username} 님 풍선 설치됨: 타입 {type} at {spawnPos}");

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
                    // 일단 무조건 충돌 무시 상태로 시작
                    Physics.IgnoreCollision(playerCol, balloonCol, true);

                    // 충돌 상태를 지속적으로 감시하는 코루틴 실행
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
                // 겹쳐있지 않으면 충돌 활성화하고 코루틴 종료
                Physics.IgnoreCollision(playerCol, balloonCol, false);
                yield break;
            }
            else
            {
                // 겹쳐있으면 계속 무시
                Physics.IgnoreCollision(playerCol, balloonCol, true);
            }

            yield return new WaitForSeconds(0.1f);
        }

        // 혹시 콜라이더가 null이 되면 충돌 복구 보장
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
        // 본인 풍선만 서버에 제거 요청 전송
        if (username == NetworkConnector.Instance.UserNickname)
        {
            string msg = $"REMOVE_BALLOON|{username}|{pos.x:F2},{pos.z:F2}|{type}\n";
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(msg);
            try
            {
                NetworkConnector.Instance.Stream.WriteAsync(bytes, 0, bytes.Length);
                Debug.Log($"[BalloonSystem] 풍선 제거 패킷 전송됨: {msg.Trim()}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BalloonSystem] 풍선 제거 패킷 전송 실패: {e.Message}");
            }
        }
    }


    public void HandleRemoveBalloon(string message)
    {
        // 예시 메시지: REMOVE_BALLOON|닉네임|x,z
        string[] parts = message.Split('|');
        if (parts.Length < 3)
        {
            Debug.LogWarning("[BalloonSystem] REMOVE_BALLOON 메시지 형식 오류");
            return;
        }

        string nickname = parts[1].Trim();
        string[] coords = parts[2].Split(',');

        if (coords.Length != 2 || !float.TryParse(coords[0], out float x) || !float.TryParse(coords[1], out float z))
        {
            Debug.LogWarning("[BalloonSystem] REMOVE_BALLOON 좌표 파싱 실패");
            return;
        }

        Vector3 pos = new Vector3(Mathf.Round(x), 1f, Mathf.Round(z));

        // 씬 내에 존재하는 풍선을 찾아 제거
        foreach (GameObject balloon in GameObject.FindGameObjectsWithTag("Balloon"))
        {
            Vector3 balloonPos = balloon.transform.position;
            if (Mathf.Approximately(balloonPos.x, pos.x) && Mathf.Approximately(balloonPos.z, pos.z))
            {
                Destroy(balloon);
                Debug.Log($"[BalloonSystem] {nickname}의 풍선 제거됨 at {pos}");
                return;
            }
        }

        Debug.LogWarning($"[BalloonSystem] 제거 대상 풍선을 찾지 못함: {pos}");
    }

    public void HandleWaterSpread(string message)
    {
        // 메시지 포맷 체크
        string[] parts = message.Split('|');
        if (parts.Length < 5)
        {
            Debug.LogWarning("[BalloonSystem] WATER_SPREAD 메시지 형식 오류");
            return;
        }

        string nickname = parts[1];
        string typeStr = parts[3];
        string positionsStr = parts[4];

        if (!int.TryParse(typeStr, out int waterType))
        {
            Debug.LogWarning("[BalloonSystem] 물 타입 파싱 실패");
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

                // 이펙트 생성
                if (waterPrefabs != null && waterType >= 0 && waterType < waterPrefabs.Length && waterPrefabs[waterType] != null)
                {
                    GameObject waterEffect = Instantiate(waterPrefabs[waterType], spawnPos, Quaternion.identity);
                    Destroy(waterEffect, 2f);
                }
            }
        }
    }

}
