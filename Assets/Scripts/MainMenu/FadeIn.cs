using UnityEngine;

public class FadeIn : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvas;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        canvas.alpha = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        if (canvas.alpha < 1)
        {
            canvas.alpha += 0.5f * Time.deltaTime;
        }
        else
        {
            canvas.alpha = 1f;
        }
    }
}
