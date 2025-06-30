using System.Text;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

public class GameTest : MonoBehaviour
{
    public Button button1;

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

        string requestMessage = $"1|{userNickname}\n";
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
}
