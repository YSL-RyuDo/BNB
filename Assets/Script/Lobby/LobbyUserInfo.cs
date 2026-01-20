using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

public class LobbyUserInfo : MonoBehaviour
{
    [SerializeField]private LobbySender lobbySender;

    public TextMeshProUGUI userNameText;
    public TextMeshProUGUI userLevelText;
    public Slider userExpBar;
    public TextMeshProUGUI userExpText;
    private float maxExp = 100;

    private async void Start()
    {
        if (NetworkConnector.Instance != null)
        {
            var stream = NetworkConnector.Instance.Stream;

            userExpBar.interactable = false;
            string sendUserInfoStr = $"GET_USER_INFO|{NetworkConnector.Instance.UserNickname}\n";
            byte[] sendUserInfoBytes = Encoding.UTF8.GetBytes(sendUserInfoStr);
            await stream.WriteAsync(sendUserInfoBytes, 0, sendUserInfoBytes.Length);
        }
    }
    
    public void SetUserInfoUI(string nickname, int level, float exp)
    {
        userNameText.text = nickname;
        userLevelText.text = $"LV.{level}";
        maxExp = level * 100;
        float currentExp = Mathf.Clamp(exp, 0f, maxExp);
        float percent = (maxExp > 0f) ? (currentExp / maxExp) * 100f : 0f;
        userExpBar.maxValue = maxExp;
        userExpBar.value = currentExp;

        userExpText.text = $"{percent:0.0}%";
    }
}
