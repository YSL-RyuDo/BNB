using System.Text;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

public class GameTest : MonoBehaviour
{
    public GameObject[] modelPrefabs; // 0: 전사, 1: 마법사 등
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
            Debug.LogError("NetworkConnector 인스턴스가 없습니다.");
            return;
        }

        var stream = NetworkConnector.Instance.Stream;
        if (stream == null || !stream.CanWrite)
        {
            Debug.LogWarning("서버 스트림이 유효하지 않거나 쓸 수 없습니다.");
            return;
        }

        string requestMessage = $"REQUEST_MODEL|{userNickname}\n";
        Debug.Log($"[Test] 보내는 메시지: {requestMessage.Trim()}");

        byte[] sendBytes = Encoding.UTF8.GetBytes(requestMessage);

        try
        {
            await stream.WriteAsync(sendBytes, 0, sendBytes.Length);
            Debug.Log("[Test] 서버에 메시지 전송 완료");
        }
        catch (System.Exception e)
        {
            Debug.LogError("서버로 메시지 전송 중 오류 발생: " + e.Message);
        }
    }

    public void ApplyModel(int modelType)
    {
        if (modelType < 0 || modelType >= modelPrefabs.Length)
        {
            Debug.LogWarning("잘못된 모델 타입: " + modelType);
            return;
        }

        // 이전 모델 제거
        if (currentModel != null)
        {
            Destroy(currentModel);
        }

        // 새 모델 생성
        currentModel = Instantiate(modelPrefabs[modelType], transform.position, Quaternion.identity, transform);
        Debug.Log("모델 적용 완료: " + modelType);
    }
}
