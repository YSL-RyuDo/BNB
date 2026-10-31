using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

// Executes the real scene handlers with minimal UI doubles. This is not a Unity PlayMode test.
class AuthTests
{
    static void Check(bool value) { if (!value) throw new Exception("Authentication check failed"); }
    static void Reset()
    {
        SceneManager.Loads = 0;
        NetworkConnector.Instance.UserNickname = null;
        NetworkConnector.Instance.CurrentUserList.Clear();
        PlayerPrefs.Values.Clear();
    }
    static void Success(Action<string> receive)
    {
        Reset();
        receive("LOGIN_SUCCESS|synthetic-user,테스트닉네임\n");
        Check(SceneManager.Loads == 1 && SceneManager.LastScene == "LobbyScene");
        Check(NetworkConnector.Instance.UserNickname == "테스트닉네임");
        Check(PlayerPrefs.Values.Count == 1 && PlayerPrefs.Values["nickname"] == "테스트닉네임");
        foreach (var message in new[] {
            "LOGIN_SUCCESS|id,synthetic-secret,nick", "LOGIN_SUCCESS|id,", "LOGIN_SUCCESS|,nick",
            "LOGIN_SUCCESS|id,nick|extra", "LOGIN_SUCCESS|id,ni\0ck", "LOGIN_SUCCESS_EXTRA|id,nick" })
        {
            Reset(); receive(message);
            Check(SceneManager.Loads == 0 && PlayerPrefs.Values.Count == 0);
        }
    }
    static void Main()
    {
        var login = new LoginUI { loginErrorText = new TMPro.TextMeshProUGUI() };
        var register = new RegisterUI { registerErrorText = new TMPro.TextMeshProUGUI() };
        var receiver = new AuthReceiver(login, register);
        Success(receiver.HandleMessage);
        foreach (var command in new[] { "WRONG_PASSWORD", "ID_NOT_FOUND", "ALREADY_LOGGED_IN", "AUTH_ERROR" })
        {
            login.loginErrorText.text = ""; receiver.HandleMessage(command + "|\n");
            Check(login.loginErrorText.text.Length > 0);
        }
        foreach (var command in new[] { "REGISTER_SUCCESS", "EMPTY_PASSWORD", "DUPLICATE_ID", "DUPLICATE_NICK", "REGISTER_ERROR", "FILE_WRITE_ERROR", "AUTH_ERROR" })
        {
            register.registerErrorText.text = ""; receiver.HandleMessage(command + "|\n");
            Check(register.registerErrorText.text.Length > 0);
        }
        receiver.Dispose();
        Console.WriteLine("PASS AuthReceiver success, malformed/old responses, error routing and nickname-only preferences");
        var legacy = new LoginSystem { loginErrorText = new TMPro.TextMeshProUGUI() };
        GameObject.Instances[typeof(LoginSystem)] = legacy;
        Success(legacy.HandleLoginMessage);
        foreach (var command in new[] { "WRONG_PASSWORD", "ID_NOT_FOUND", "ALREADY_LOGGED_IN", "AUTH_ERROR" })
        {
            legacy.loginErrorText.text = ""; legacy.HandleLoginMessage(command + "|\n");
            Check(legacy.loginErrorText.text.Length > 0);
        }
        Console.WriteLine("PASS LoginSystem success, malformed/old responses and error routing");
        var oldRegister = new RegisterSystem { registerErrorText = new TMPro.TextMeshProUGUI() };
        GameObject.Instances[typeof(RegisterSystem)] = oldRegister;
        foreach (var command in new[] { "REGISTER_SUCCESS", "EMPTY_PASSWORD", "DUPLICATE_ID", "DUPLICATE_NICK", "REGISTER_ERROR", "FILE_WRITE_ERROR", "AUTH_ERROR" })
        {
            oldRegister.registerErrorText.text = ""; oldRegister.HandleRegisterMessage(command + "|\n");
            Check(oldRegister.registerErrorText.text.Length > 0);
        }
        Console.WriteLine("PASS RegisterSystem delimited response handling");
        Check(!AuthProtocol.TryParseLoginSuccess(null, out _, out _));
        var framing = new LineMessageBuffer(); int calls=0;
        byte[] bytes=System.Text.Encoding.UTF8.GetBytes("LOGIN_SUCCESS|id,닉네임\n");
        foreach(byte b in bytes) framing.Append(new[]{b},1,message => {
            Check(AuthProtocol.TryParseLoginSuccess(message,out var id,out var nick));
            Check(id=="id" && nick=="닉네임"); calls++;
        });
        Check(calls==1);
        Console.WriteLine("PASS split UTF-8 authentication response");
        typeof(LineMessageBufferTests).GetMethod("Main", BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
        Check(Debug.Messages.TrueForAll(m => !m.Contains("synthetic-secret") && !m.Contains("LOGIN_SUCCESS|")));
        Console.WriteLine("PASS authentication logs omit response bodies");
    }
}
