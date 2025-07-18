using UnityEngine;
using System.Text;
using System.Threading.Tasks;

public class LocalPlayerController : MonoBehaviour
{
    private GameObject localCharacter;

    private Vector3 lastSentPosition;
    private float moveSpeed = 3.0f;

    void Start()
    {
        string myNick = NetworkConnector.Instance.UserNickname;
        string characterObjectName = $"Character_{myNick}";

        localCharacter = GameObject.Find(characterObjectName);

        if (localCharacter == null)
        {
            Debug.LogError($"내 캐릭터를 찾을 수 없음: {characterObjectName}");
            return;
        }

        lastSentPosition = localCharacter.transform.position;
    }

    void Update()
    {
        if (localCharacter == null)
            return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 dir = new Vector3(h, 0, v).normalized;

        if (dir.magnitude > 0.1f)
        {
            // 이동
            localCharacter.transform.position += dir * moveSpeed * Time.deltaTime;

            // 방향
            localCharacter.transform.forward = dir;

            // 이동했을 때만 위치 전송
            TrySendPosition();
        }

        // 물풍선 설치
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (BalloonSystem.Instance != null)
            {
                int balloonType = 0;
                BalloonSystem.Instance.PlaceBalloonAt(localCharacter.transform.position, balloonType);
            }
        }
    }


    private async void TrySendPosition()
    {
        Vector3 currentPos = localCharacter.transform.position;

        lastSentPosition = currentPos;

        // 메시지 포맷 예시: MOVE|닉네임|x,z\n (y축 제외 가능)
        string posStr = $"{currentPos.x:F2},{currentPos.z:F2}";
        string msg = $"MOVE|{NetworkConnector.Instance.UserNickname}|{posStr}\n";

        byte[] bytes = Encoding.UTF8.GetBytes(msg);

        try
        {
            var stream = NetworkConnector.Instance.Stream;
            if (stream != null && stream.CanWrite)
            {
                await stream.WriteAsync(bytes, 0, bytes.Length);
                Debug.Log($"위치 전송: {msg.Trim()}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"위치 전송 실패: {e.Message}");
        }
    }
}
