using System;
using System.Net.Sockets;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RegisterSystem: MonoBehaviour
{
    public TMP_InputField inputID;
    public TMP_InputField inputPassword;
    public TMP_InputField inputNickname;
    public TextMeshProUGUI registerErrorText;
    public Button registerButton;
    //public Button exitButton;
    public Button changeLoginButton;

    public GameObject loginImage;
    public GameObject registerImage;
    private void Start()
    {
        registerButton.onClick.AddListener(OnRegisterClicked);
        registerErrorText.text = "";
        //exitButton.onClick.AddListener(OnExitClicked);
        changeLoginButton.onClick.AddListener(OnLoginClicked);
    }

    private async void OnRegisterClicked()
    {
        string id = inputID.text.Trim();
        string pw = inputPassword.text.Trim();
        string nickname = inputNickname.text.Trim();

        try
        {
            string message = $"REGISTER|{id},{pw},{nickname}\n";
            byte[] sendBytes = Encoding.UTF8.GetBytes(message);
            await NetworkConnector.Instance.Stream.WriteAsync(sendBytes, 0, sendBytes.Length);

            registerErrorText.text = "회원가입 요청 전송 완료, 응답 대기 중...";
        }
        catch (Exception ex)
        {
            registerErrorText.text = "연결 실패: " + ex.Message;
        }
    }

    public void HandleRegisterMessage(string message)
    {
        RegisterSystem registerManager = GameObject.FindObjectOfType<RegisterSystem>();

        if (message == "REGISTER_SUCCESS")
        {
            Debug.Log("회원가입 성공");
            if (registerManager != null)
                registerManager.registerErrorText.text = "회원가입 성공";
        }
        else if (message == "EMPTY_PASSWORD")
        {
            if (registerManager != null)
                registerManager.registerErrorText.text = "빈 비밀번호";
        }
        else if (message == "DUPLICATE_ID")
        {
            if (registerManager != null)
                registerManager.registerErrorText.text = "ID 중복";
        }
        else if (message == "DUPLICATE_NICK")
        {
            if (registerManager != null)
                registerManager.registerErrorText.text = "닉네임 중복";
        }
        else if (message == "REGISTER_ERROR")
        {
            if (registerManager != null)
                registerManager.registerErrorText.text = "알 수 없는 에러";
        }
        else if (message == "FILE_WRITE_ERROR")
        {
            if (registerManager != null)
                registerManager.registerErrorText.text = "파일 에러";
        }
    }

    private void OnLoginClicked()
    {
        loginImage.SetActive(true);
        registerImage.SetActive(false);
    }
}
