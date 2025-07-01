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

            registerErrorText.text = "ȸ������ ��û ���� �Ϸ�, ���� ��� ��...";
        }
        catch (Exception ex)
        {
            registerErrorText.text = "���� ����: " + ex.Message;
        }
    }

    public void HandleRegisterMessage(string message)
    {
        RegisterSystem registerManager = GameObject.FindObjectOfType<RegisterSystem>();

        if (message == "REGISTER_SUCCESS")
        {
            Debug.Log("ȸ������ ����");
            if (registerManager != null)
                registerManager.registerErrorText.text = "ȸ������ ����";
        }
        else if (message == "EMPTY_PASSWORD")
        {
            if (registerManager != null)
                registerManager.registerErrorText.text = "�� ��й�ȣ";
        }
        else if (message == "DUPLICATE_ID")
        {
            if (registerManager != null)
                registerManager.registerErrorText.text = "ID �ߺ�";
        }
        else if (message == "DUPLICATE_NICK")
        {
            if (registerManager != null)
                registerManager.registerErrorText.text = "�г��� �ߺ�";
        }
        else if (message == "REGISTER_ERROR")
        {
            if (registerManager != null)
                registerManager.registerErrorText.text = "�� �� ���� ����";
        }
        else if (message == "FILE_WRITE_ERROR")
        {
            if (registerManager != null)
                registerManager.registerErrorText.text = "���� ����";
        }
    }

    private void OnLoginClicked()
    {
        loginImage.SetActive(true);
        registerImage.SetActive(false);
    }
}
