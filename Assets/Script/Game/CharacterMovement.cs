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

            // 바라보는 방향 변경
            localCharacter.transform.forward = dir;
        }

        TrySendPosition();
    }

    private async void TrySendPosition()
    {
        Vector3 currentPos = localCharacter.transform.position;

        // 위치 변화가 거의 없으면 전송하지 않음 (0.01 이상 이동했을 때만)
        if (Vector3.Distance(currentPos, lastSentPosition) > 0.01f)
        {
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
}
