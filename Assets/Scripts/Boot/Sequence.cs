using UnityEngine;
using UnityEngine.SceneManagement;

public class Sequence : MonoBehaviour
{
    [SerializeField] private CanvasGroup teamLogo;
    [SerializeField] private CanvasGroup autoSave;
    private float timeElapsed = 0f;
    private float teamLogoFadeInStart = 1f;
    private float teamLogoFadeOutStart = 4f;
    private float autoSaveFadeInStart = 6f;
    private float autoSaveFadeOutStart = 11f;
    private float nextSceneStart = 12f;
    private string nextScene = "MainMenu";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        teamLogo.alpha = 0;
        autoSave.alpha = 0;
    }

    // Update is called once per frame
    void Update()
    {
        timeElapsed += Time.deltaTime;

        if (timeElapsed >= teamLogoFadeInStart && timeElapsed < teamLogoFadeOutStart)
        {
            teamLogo.alpha += 0.5f * Time.deltaTime;
        }
        else if (timeElapsed >= teamLogoFadeOutStart && timeElapsed < autoSaveFadeInStart)
        {
            teamLogo.alpha -= 1f * Time.deltaTime;
        }
        else if (timeElapsed >= autoSaveFadeInStart && timeElapsed < autoSaveFadeOutStart)
        {
            teamLogo.alpha = 0;
            autoSave.alpha += 0.5f * Time.deltaTime;
        }
        else if (timeElapsed >= autoSaveFadeOutStart && timeElapsed < nextSceneStart)
        {
            autoSave.alpha -= 1f * Time.deltaTime;
        }
        else if (timeElapsed >= nextSceneStart)
        {
            SceneManager.LoadScene(nextScene);
        }
    }
}
