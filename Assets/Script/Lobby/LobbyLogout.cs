using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class LobbyLogout : MonoBehaviour
{
    public Button logoutButton;
    // Start is called before the first frame update
    void Start()
    {
        logoutButton.onClick.AddListener(OnLogoutClicked);
    }

    private async void OnLogoutClicked()
    {
        try
        {
            var stream = NetworkConnector.Instance.Stream;

            string nickname = NetworkConnector.Instance.UserNickname;
            if (string.IsNullOrEmpty(nickname))
            {
                Debug.LogError("�г����� �������� �ʾҽ��ϴ�. �α׾ƿ� �޽����� ���� �� �����ϴ�.");
                return;
            }

            string logoutMsg = $"LOGOUT|{nickname}\n";
            byte[] quitMsg = Encoding.UTF8.GetBytes(logoutMsg);
            await stream.WriteAsync(quitMsg, 0, quitMsg.Length);
            await stream.FlushAsync();

            if (NetworkConnector.Instance.CurrentUserList.Contains(nickname))
            {
                NetworkConnector.Instance.CurrentUserList.Remove(nickname);
                Debug.Log($"[�α׾ƿ�] ���� ���ŵ�: {nickname}");
            }

            NetworkConnector.Instance.UserNickname = null;

            UnityEngine.SceneManagement.SceneManager.LoadScene("LoginScene");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("�α׾ƿ� �� ���� �߻�: " + ex.Message);
        }
    }
}
