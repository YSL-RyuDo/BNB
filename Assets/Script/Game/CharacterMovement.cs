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

            // �ٶ󺸴� ���� ����
            localCharacter.transform.forward = dir;
        }

        TrySendPosition();
    }

    private async void TrySendPosition()
    {
        Vector3 currentPos = localCharacter.transform.position;

        // ��ġ ��ȭ�� ���� ������ �������� ���� (0.01 �̻� �̵����� ����)
        if (Vector3.Distance(currentPos, lastSentPosition) > 0.01f)
        {
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
}
