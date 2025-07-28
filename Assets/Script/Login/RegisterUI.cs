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
        string id = inputID.text.Trim();
        string pw = inputPassword.text.Trim();
        string nickname = inputNickname.text.Trim();
        
        await AuthSender.SendRegisterRequest(id, pw, nickname);
    }

    private void OnLoginClicked()
    {
        loginImage.SetActive(true);
        registerImage.SetActive(false);
    }
}
