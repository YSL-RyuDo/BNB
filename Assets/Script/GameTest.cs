using System.Text;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

public class GameTest : MonoBehaviour
{
    public GameObject[] modelPrefabs; // 0: ����, 1: ������ ��
    private GameObject currentModel;

    void Start()
    {
        string userNickname = NetworkConnector.Instance?.UserNickname;
        _ = SendModelRequestAsync(userNickname);
    }

    async Task SendModelRequestAsync(string userNickname)
    {
        if (NetworkConnector.Instance == null)
        {
            Debug.LogError("NetworkConnector �ν��Ͻ��� �����ϴ�.");
            return;
        }

        var stream = NetworkConnector.Instance.Stream;
        if (stream == null || !stream.CanWrite)
        {
            Debug.LogWarning("���� ��Ʈ���� ��ȿ���� �ʰų� �� �� �����ϴ�.");
            return;
        }

        string requestMessage = $"REQUEST_MODEL|{userNickname}\n";
        Debug.Log($"[Test] ������ �޽���: {requestMessage.Trim()}");

        byte[] sendBytes = Encoding.UTF8.GetBytes(requestMessage);

        try
        {
            await stream.WriteAsync(sendBytes, 0, sendBytes.Length);
            Debug.Log("[Test] ������ �޽��� ���� �Ϸ�");
        }
        catch (System.Exception e)
        {
            Debug.LogError("������ �޽��� ���� �� ���� �߻�: " + e.Message);
        }
    }

    public void ApplyModel(int modelType)
    {
        if (modelType < 0 || modelType >= modelPrefabs.Length)
        {
            Debug.LogWarning("�߸��� �� Ÿ��: " + modelType);
            return;
        }

        // ���� �� ����
        if (currentModel != null)
        {
            Destroy(currentModel);
        }

        // �� �� ����
        currentModel = Instantiate(modelPrefabs[modelType], transform.position, Quaternion.identity, transform);
        Debug.Log("�� ���� �Ϸ�: " + modelType);
    }
}
