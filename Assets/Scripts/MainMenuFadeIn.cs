using System.Threading;
using UnityEngine;

public class MainMenuFadeIn : MonoBehaviour
{
    [SerializeField] private CanvasGroup placeholder;
    private float timeElapsed = 0f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        placeholder.alpha = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        timeElapsed += Time.deltaTime;
        placeholder.alpha += 0.5f * Time.deltaTime;
    }
}
