using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RegisterUI : MonoBehaviour
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

    // Start is called before the first frame update
    void Start()
    {
        registerButton.onClick.AddListener(OnRegisterClicked);
        registerErrorText.text = "";
        changeLoginButton.onClick.AddListener(OnLoginClicked);

        var receiver = new AuthReceiver(null, this);
        NetworkConnector.Instance.RegisterHandler("REGISTER_SUCCESS", receiver);
        NetworkConnector.Instance.RegisterHandler("DUPLICATE_ID", receiver);
        NetworkConnector.Instance.RegisterHandler("DUPLICATE_NICK", receiver);
        NetworkConnector.Instance.RegisterHandler("REGISTER_ERROR", receiver);
    }

    private async void OnRegisterClicked()
    {
        ButtonSoundManager.Instance?.PlayClick();
        string id = inputID.text.Trim();
        string pw = inputPassword.text.Trim();
        string nickname = inputNickname.text.Trim();
        
        await AuthSender.SendRegisterRequest(id, pw, nickname);

        registerErrorText.text = "회원가입 요청 전송 완료, 응답 대기 중...";
    }

    private void OnLoginClicked()
    {
        ButtonSoundManager.Instance?.PlayClick();
        loginImage.SetActive(true);
        registerImage.SetActive(false);
    }
}
