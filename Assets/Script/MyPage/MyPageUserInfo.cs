using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MyPageUserInfo : MonoBehaviour
{
    [SerializeField] private MyPageSender myPageSender;

    public TextMeshProUGUI userNameText;
    public TextMeshProUGUI userLevelText;
    public Slider userExpBar;
    public TextMeshProUGUI userExpText;
    private float maxExp = 100;

    private void Start()
    {
        userExpBar.interactable = false;
        myPageSender.SendGetUserInfo(NetworkConnector.Instance.UserNickname);
    }

    public void SetUserInfoUI(string nickname, int level, float exp)
    {
        userNameText.text = nickname;
        userLevelText.text = $"Level {level}";
        maxExp = level * 100;
        float currentExp = Mathf.Clamp(exp, 0f, maxExp);
        float percent = (maxExp > 0f) ? (currentExp / maxExp) * 100f : 0f;
        userExpBar.maxValue = maxExp;
        userExpBar.value = currentExp;

        userExpText.text = $"{percent:0.0}%";
    }
}
