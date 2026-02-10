using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreditsUI : MonoBehaviour
{
    public GameObject creditsRoot;      
    public RectTransform viewport;      
    public RectTransform creditsText;   

    public float speed = 80f;           
    public float startPadding = 50f;   
    public bool stopAtBottomAlign = true; 

    bool playing;

    void Awake()
    {
        if (creditsRoot) creditsRoot.SetActive(false);
    }

    void Update()
    {
        if (!playing) return;

        creditsText.anchoredPosition += Vector2.up * speed * Time.deltaTime;

        if (stopAtBottomAlign)
        {
            
            float viewportH = viewport.rect.height;
            float textH = creditsText.rect.height;

            float textBottomY = creditsText.anchoredPosition.y - textH * 0.5f;

            float viewportBottomY = -viewportH * 0.5f;

            if (textBottomY >= viewportBottomY)
            {
                playing = false;
            }
        }
    }

    public void OpenCredits()
    {
        creditsRoot.SetActive(true);

        float viewportH = viewport.rect.height;
        float textH = creditsText.rect.height;

        float viewportBottomY = -viewportH * 0.5f;
        float startY = viewportBottomY - (textH * 0.5f) - startPadding;

        var p = creditsText.anchoredPosition;
        creditsText.anchoredPosition = new Vector2(p.x, startY);

        playing = true;
    }

    public void CloseCredits()
    {
        playing = false;
        creditsRoot.SetActive(false);
    }
}
