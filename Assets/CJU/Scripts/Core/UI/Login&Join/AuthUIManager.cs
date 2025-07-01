using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AuthUIManager : MonoBehaviour
{

    public GameObject joinPanel;
    public GameObject loginPanel;
    public Button toLoginButton;
    public Button toJoinButton;

    void Start()
    {
        toLoginButton.onClick.AddListener(SwitchToLogin);
        toJoinButton.onClick.AddListener(SwitchToJoin);
    }

    void SwitchToLogin()
    {
        joinPanel.SetActive(false);
        loginPanel.SetActive(true);
    }


    void SwitchToJoin()
    {
        loginPanel.SetActive(false);
        joinPanel.SetActive(true);
    }

}
