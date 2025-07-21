using UnityEngine;
using System.Text;
using System.Threading.Tasks;

public class LocalPlayerController : MonoBehaviour
{
    private GameObject localCharacter;
    private Rigidbody rb;

    private Vector3 lastSentPosition;
    private float moveSpeed = 3.0f;

    private string myNick;
    private bool hasSentWaterHit = false;

    void Start()
    {
        myNick = NetworkConnector.Instance.UserNickname;
        string characterObjectName = $"Character_{myNick}";

        localCharacter = GameObject.Find(characterObjectName);

        if (localCharacter == null)
        {
            Debug.LogError($"내 캐릭터를 찾을 수 없음: {characterObjectName}");
            return;
        }

        rb = localCharacter.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError($"Rigidbody 컴포넌트가 없음: {characterObjectName}");
            return;
        }

        lastSentPosition = localCharacter.transform.position;
    }

    Vector3 inputDirection = Vector3.zero;

    void Update()
    {
        if (localCharacter == null)
            return;

        // 입력만 받고, 이동은 FixedUpdate에서 처리
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        inputDirection = new Vector3(h, 0, v).normalized;

        // 물풍선 설치 처리
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!BalloonSystem.Instance.CanPlaceBalloon())
            {
                Debug.Log("[LocalPlayerController] 풍선 쿨타임 중이라 설치 불가");
                return;
            }

            Vector3 pos = localCharacter.transform.position;
            float cellSize = 1.0f;
            float snappedX = Mathf.Round(pos.x / cellSize) * cellSize;
            float snappedZ = Mathf.Round(pos.z / cellSize) * cellSize;
            Vector3 snappedPos = new Vector3(snappedX, 0, snappedZ);

            int balloonType = BalloonSystem.Instance != null ? BalloonSystem.Instance.GetCurrentBalloonType() : 0;

            string balloonMsg = $"PLACE_BALLOON|{myNick}|{snappedX:F2},{snappedZ:F2}|{balloonType}\n";
            byte[] bytes = Encoding.UTF8.GetBytes(balloonMsg);

            try
            {
                NetworkConnector.Instance.Stream.WriteAsync(bytes, 0, bytes.Length);
                Debug.Log($"[PlayerInput] 물풍선 설치 요청 전송: {balloonMsg.Trim()}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PlayerInput] 물풍선 설치 요청 실패: {ex.Message}");
            }
        }
    }

    void FixedUpdate()
    {
        if (localCharacter == null || rb == null)
            return;

        if (inputDirection.magnitude > 0.1f)
        {
            Vector3 targetPos = rb.position + inputDirection * moveSpeed * Time.fixedDeltaTime;

            rb.MovePosition(targetPos);

            Quaternion targetRot = Quaternion.LookRotation(inputDirection);
            rb.MoveRotation(targetRot);

            TrySendPosition();
        }
    }

    private async void TrySendPosition()
    {
        Vector3 currentPos = localCharacter.transform.position;

        lastSentPosition = currentPos;

        string posStr = $"{currentPos.x:F2},{currentPos.z:F2}";
        string msg = $"MOVE|{myNick}|{posStr}\n";

        byte[] bytes = Encoding.UTF8.GetBytes(msg);

        try
        {
            var stream = NetworkConnector.Instance.Stream;
            if (stream != null && stream.CanWrite)
            {
                await stream.WriteAsync(bytes, 0, bytes.Length);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"위치 전송 실패: {e.Message}");
        }
    }

    private async void OnTriggerEnter(Collider other)
    {
        if (localCharacter == null || hasSentWaterHit)
            return;

        if (other.gameObject.CompareTag("Water"))
        {
            if (this.gameObject == localCharacter)
            {
                hasSentWaterHit = true;
                Debug.Log("[WaterHitDetector] 내 캐릭터가 물에 맞음! 패킷 전송");

                string msg = $"WATER_HIT|{myNick}|1000\n";
                byte[] bytes = Encoding.UTF8.GetBytes(msg);
                await NetworkConnector.Instance.Stream.WriteAsync(bytes, 0, bytes.Length);

                Debug.Log("[WaterHitDetector] WATER_HIT 패킷 전송 완료");

                ResetHitFlagAfterDelay(2f);
            }
        }
    }

    private async void ResetHitFlagAfterDelay(float delaySeconds)
    {
        await Task.Delay((int)(delaySeconds * 1000));
        hasSentWaterHit = false;
        Debug.Log("[WaterHitDetector] hasSentWaterHit 플래그 초기화됨");
    }
}
