using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//로그인 씬 UI 관리 스크립트
public class LoginUI : MonoBehaviour
{
    public TMP_InputField inputID;
    public TMP_InputField inputPassword;
    public TextMeshProUGUI loginErrorText;
    public Button loginButton;
    public Button exitButton;
    public Button changeResigeterButton;

    public GameObject loginImage;
    public GameObject registerImage;

    // Start is called before the first frame update
    void Start()
    {
        loginButton.onClick.AddListener(OnLoginClicked);
        loginErrorText.text = "";
        exitButton.onClick.AddListener(OnExitClicked);
        changeResigeterButton.onClick.AddListener(OnRegisterClicked);

        var receiver = new AuthReceiver(this, null);
        NetworkConnector.Instance.RegisterHandler("LOGIN_SUCCESS", receiver);
        NetworkConnector.Instance.RegisterHandler("WRONG_PASSWORD", receiver);
        NetworkConnector.Instance.RegisterHandler("ID_NOT_FOUND", receiver);
    }

    private async void OnLoginClicked()
    {
        ButtonSoundManager.Instance?.PlayClick();
        string id = inputID.text.Trim();
        string pw = inputPassword.text.Trim();
        await AuthSender.SendLoginRequest(id, pw);
    }

    private void OnRegisterClicked()
    {
        ButtonSoundManager.Instance?.PlayClick();
        loginImage.SetActive(false);
        registerImage.SetActive(true);
    }

    private void OnExitClicked()
    {
        ButtonSoundManager.Instance?.PlayClick();
        Application.Quit();
    }
}
