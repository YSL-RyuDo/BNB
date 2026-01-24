using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainSoundManager : MonoBehaviour
{
    public static MainSoundManager Instance;


    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private string[] stopOnScenes;


    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (bgmSource == null)
            bgmSource = GetComponent<AudioSource>();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (bgmSource == null) return;

        foreach (var s in stopOnScenes)
        {
            if (scene.name == s)
            {
                bgmSource.Stop();   
                return;
            }
        }

        if (!bgmSource.isPlaying)
        {
            bgmSource.Play();
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this)
            Instance = null;
    }
}
