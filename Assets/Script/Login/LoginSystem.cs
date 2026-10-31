using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;


public class LoginSystem : MonoBehaviour
{
    public TMP_InputField inputID;
    public TMP_InputField inputPassword;
    public TextMeshProUGUI loginErrorText;
    public Button loginButton;
    public Button exitButton;
    public Button changeResigeterButton;

    public GameObject loginImage;
    public GameObject registerImage;
    private void Start()
    {
        loginButton.onClick.AddListener(OnLoginClicked);
        loginErrorText.text = "";
        exitButton.onClick.AddListener(OnExitClicked);
        changeResigeterButton.onClick.AddListener(OnRegisterClicked);
    }

    private async void OnLoginClicked()
    {
        string id = inputID.text.Trim();
        string pw = inputPassword.text.Trim();

        try
        {
            var stream = NetworkConnector.Instance.Stream;
            string sendStr = $"LOGIN|{id},{pw}\n";
            byte[] sendBytes = Encoding.UTF8.GetBytes(sendStr);
            await Task.Delay(100);
            await stream.WriteAsync(sendBytes, 0, sendBytes.Length);
        }
        catch (System.Exception ex)
        {
            loginErrorText.text = "Connection failed: " + ex.Message;
        }
    }

    private async void OnExitClicked()
    {
        try
        {
            var stream = NetworkConnector.Instance.Stream;
            byte[] quitMsg = Encoding.UTF8.GetBytes("QUIT|\n");
            await stream.WriteAsync(quitMsg, 0, quitMsg.Length);
            await stream.FlushAsync();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("종료 메시지 전송 실패: " + ex.Message);
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    public void HandleLoginMessage(string message)
    {
        string command = AuthProtocol.Command(message);
        LoginSystem loginManager = GameObject.FindObjectOfType<LoginSystem>();

        if (command == "LOGIN_SUCCESS")
        {
            string[] parts = message.Split('|');
            if (parts.Length == 2)
            {
                string userId, userNick;

                if (AuthProtocol.TryParseLoginSuccess(message, out userId, out userNick))
                {

                    PlayerPrefs.SetString("nickname", userNick);
                    PlayerPrefs.Save();
                    NetworkConnector.Instance.UserNickname = userNick;
                    if (!NetworkConnector.Instance.CurrentUserList.Contains(userNick))
                    {
                        NetworkConnector.Instance.CurrentUserList.Add(userNick);
                    }
                    SceneManager.LoadScene("LobbyScene");
                }
                else
                {
                    Debug.LogError("유저 정보 파싱 실패");
                    if (loginManager != null)
                        loginManager.loginErrorText.text = "유저 정보 파싱 실패";
                }
            }
            else
            {
                Debug.LogError("응답 형식 오류");
                if (loginManager != null)
                    loginManager.loginErrorText.text = "응답 형식 오류";
            }
        }
        else if (command == "ALREADY_LOGGED_IN" || command == "AUTH_ERROR")
        {
            if (loginManager != null)
                loginManager.loginErrorText.text = command == "ALREADY_LOGGED_IN"
                    ? "Already logged in." : "Authentication unavailable. Please retry.";
        }
        else if (command == "WRONG_PASSWORD")
        {
            Debug.Log("비밀번호 오류");
            if (loginManager != null)
                loginManager.loginErrorText.text = "잘못된 비밀번호";
        }
        else if (command == "ID_NOT_FOUND")
        {
            Debug.Log("계정이 없음");
            if (loginManager != null)
                loginManager.loginErrorText.text = "존재하지 않는 사용자";
        }
    }

    private void OnRegisterClicked()
    {
        loginImage.SetActive(false);
        registerImage.SetActive(true);
    }
}
