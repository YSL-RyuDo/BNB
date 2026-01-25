using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonSoundManager : MonoBehaviour
{
    public static ButtonSoundManager Instance;

    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip clickClip;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void PlayClick()
    {
        if (uiAudioSource == null || clickClip == null) return;
        uiAudioSource.PlayOneShot(clickClip);
    }
}
