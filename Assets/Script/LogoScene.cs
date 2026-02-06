using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LogoScene : MonoBehaviour
{
    public Image logo;
    public float fadeIn = 1.5f;
    public float stay = 1.0f;

    void Start()
    {
        StartCoroutine(Sequence());
    }

    IEnumerator Sequence()
    {
        for (float t = 0; t < fadeIn; t += Time.deltaTime)
        {
            logo.color = new Color(1, 1, 1, t / fadeIn);
            yield return null;
        }

        yield return new WaitForSeconds(stay);

        SceneManager.LoadScene("LoginScene");
    }

}
