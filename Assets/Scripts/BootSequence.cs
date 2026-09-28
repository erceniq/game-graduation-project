using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootSequence : MonoBehaviour
{
    [SerializeField] private CanvasGroup placeholder;
    private float timeElapsed = 0f;
    private float fadeInStart = 1f;
    private float fadeOutStart = 4f;
    private float nextSceneStart = 5f;
    private string nextScene = "MainMenu";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        placeholder.alpha = 0;
    }

    // Update is called once per frame
    void Update()
    {
        timeElapsed += Time.deltaTime;

        if (timeElapsed >= fadeInStart && timeElapsed < fadeOutStart)
        {
            placeholder.alpha += 0.5f * Time.deltaTime;
        }
        else if (timeElapsed >= fadeOutStart && timeElapsed < nextSceneStart)
        {
            placeholder.alpha -= 1f * Time.deltaTime;
        }
        else if (timeElapsed >= nextSceneStart)
        {
            SceneManager.LoadScene(nextScene);
        }
    }
}
