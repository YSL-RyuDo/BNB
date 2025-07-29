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
                Debug.LogError("닉네임이 설정되지 않았습니다. 로그아웃 메시지를 보낼 수 없습니다.");
                return;
            }

            string logoutMsg = $"LOGOUT|{nickname}\n";
            byte[] quitMsg = Encoding.UTF8.GetBytes(logoutMsg);
            await stream.WriteAsync(quitMsg, 0, quitMsg.Length);
            await stream.FlushAsync();

            if (NetworkConnector.Instance.CurrentUserList.Contains(nickname))
            {
                NetworkConnector.Instance.CurrentUserList.Remove(nickname);
                Debug.Log($"[로그아웃] 유저 제거됨: {nickname}");
            }

            NetworkConnector.Instance.UserNickname = null;

            UnityEngine.SceneManagement.SceneManager.LoadScene("LoginScene");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("로그아웃 중 오류 발생: " + ex.Message);
        }
    }
}
