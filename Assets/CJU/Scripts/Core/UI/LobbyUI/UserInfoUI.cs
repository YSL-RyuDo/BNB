using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UserInfoUI : MonoBehaviour
{
    public TextMeshProUGUI nicknameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI expText;
    public Slider expSlider;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetUserInfo(string nickname, int level, float exp)
    {
        nicknameText.text = nickname;
        levelText.text = $"Lv. {level}";
        expText.text = $"{exp}"; 
        expSlider.value = exp;
    }
}
