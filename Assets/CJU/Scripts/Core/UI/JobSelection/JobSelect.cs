using System.Text;
using TMPro;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;

public class JobSelect : MonoBehaviour
{
    public TextMeshProUGUI jobNameText;
    public Button selectButton;

    public void Initialize(string jobName, int jobIndex, TMP_InputField inputField)
    {
        jobNameText.text = jobName;

        selectButton.onClick.AddListener(() =>
        {
            string nickname = inputField.text.Trim();
            if (!string.IsNullOrEmpty(nickname))
            {
                SendJobPacket(nickname, jobIndex);
            }
        });
    }

    private async void SendJobPacket(string nickname, int jobType)
    {
        string packet = $"0|{nickname},{jobType}\n";
        byte[] bytes = Encoding.UTF8.GetBytes(packet);
        await NetworkConnector.Instance.Stream.WriteAsync(bytes, 0, bytes.Length);
        Debug.Log("직업 선택 후 보낸 정보" + packet);
    }
}