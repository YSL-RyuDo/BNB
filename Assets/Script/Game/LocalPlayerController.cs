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
            Debug.LogError($"�� ĳ���͸� ã�� �� ����: {characterObjectName}");
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
            // �̵�
            localCharacter.transform.position += dir * moveSpeed * Time.deltaTime;

            // ����
            localCharacter.transform.forward = dir;

            // �̵����� ���� ��ġ ����
            TrySendPosition();
        }

        // ��ǳ�� ��ġ
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

        // �޽��� ���� ����: MOVE|�г���|x,z\n (y�� ���� ����)
        string posStr = $"{currentPos.x:F2},{currentPos.z:F2}";
        string msg = $"MOVE|{NetworkConnector.Instance.UserNickname}|{posStr}\n";

        byte[] bytes = Encoding.UTF8.GetBytes(msg);

        try
        {
            var stream = NetworkConnector.Instance.Stream;
            if (stream != null && stream.CanWrite)
            {
                await stream.WriteAsync(bytes, 0, bytes.Length);
                Debug.Log($"��ġ ����: {msg.Trim()}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"��ġ ���� ����: {e.Message}");
        }
    }
}
