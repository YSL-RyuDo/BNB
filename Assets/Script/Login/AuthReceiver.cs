using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 로그인 및 회원 가입 정보 인증용 메시지 수신 클래스
public class AuthReceiver : IMessageHandler
{
    private readonly LoginUI loginUI;
    private readonly RegisterUI registerUI;

    private readonly string[] commands = {
        "LOGIN_SUCCESS", "WRONG_PASSWORD", "ID_NOT_FOUND", "ALREADY_LOGGED_IN", "AUTH_ERROR",
        "REGISTER_SUCCESS", "EMPTY_PASSWORD", "DUPLICATE_ID",
        "DUPLICATE_NICK", "REGISTER_ERROR", "FILE_WRITE_ERROR"
    };

    public AuthReceiver(LoginUI loginUI, RegisterUI registerUI)
    {
        this.loginUI = loginUI;
        this.registerUI = registerUI;

        foreach (string command in commands)
        {
            NetworkConnector.Instance.RegisterHandler(command, this);
        }
    }
    public void Dispose()
    {
        foreach (string command in commands)
        {
            NetworkConnector.Instance.RemoveHandler(command, this);
        }
    }

    public void HandleMessage(string message)
    {
        string command = AuthProtocol.Command(message);
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
                    if (loginUI != null)
                        loginUI.loginErrorText.text = "유저 정보 파싱 실패";
                }
            }
            else
            {
                Debug.LogError("응답 형식 오류");
                if (loginUI != null)
                    loginUI.loginErrorText.text = "응답 형식 오류";
            }
        }
        else if (command == "ALREADY_LOGGED_IN" || command == "AUTH_ERROR")
        {
            if (loginUI != null)
                loginUI.loginErrorText.text = command == "ALREADY_LOGGED_IN"
                    ? "Already logged in." : "Authentication unavailable. Please retry.";
        }
        else if (command == "WRONG_PASSWORD")
        {
            Debug.Log("비밀번호 오류");
            if (loginUI != null)
                loginUI.loginErrorText.text = "잘못된 비밀번호";
        }
        else if (command == "ID_NOT_FOUND")
        {
            Debug.Log("계정이 없음");
            if (loginUI != null)
                loginUI.loginErrorText.text = "존재하지 않는 사용자";
        }

        if (command == "AUTH_ERROR" && registerUI != null)
            registerUI.registerErrorText.text = "Authentication unavailable. Please retry.";

        if (command == "REGISTER_SUCCESS")
        {
            Debug.Log("회원가입 성공");
            if (registerUI != null)
                registerUI.registerErrorText.text = "회원가입 성공";
        }
        else if (command == "EMPTY_PASSWORD")
        {
            if (registerUI != null)
                registerUI.registerErrorText.text = "빈 비밀번호";
        }
        else if (command == "DUPLICATE_ID")
        {
            if (registerUI != null)
                registerUI.registerErrorText.text = "ID 중복";
        }
        else if (command == "DUPLICATE_NICK")
        {
            if (registerUI != null)
                registerUI.registerErrorText.text = "닉네임 중복";
        }
        else if (command == "REGISTER_ERROR")
        {
            if (registerUI != null)
                registerUI.registerErrorText.text = "알 수 없는 에러";
        }
        else if (command == "FILE_WRITE_ERROR")
        {
            if (registerUI != null)
                registerUI.registerErrorText.text = "파일 에러";
        }
    }
    
}
