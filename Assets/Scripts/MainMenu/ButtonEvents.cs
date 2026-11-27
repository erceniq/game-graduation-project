using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonEvents : MonoBehaviour
{
    [SerializeField] private RectTransform visual;
    [SerializeField] private CanvasGroup defaultFrame;
    [SerializeField] private CanvasGroup selectedFrame;
    [SerializeField] private CanvasGroup arrows;
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_Text number;
    [SerializeField] private Image fill;
    [SerializeField] private Image handle;

    private bool isSelected;
    private bool isPressed;
    private float speed = 0.2f;
    private float value;
    private float slideDistance = 9f;
    private Color defaultColor = new Color32(170, 170, 170, 150);
    private Color selectedColor = new Color32(255, 255, 255, 255);
    private float pressValue;
    private float pressedScale = 0.98f;
    private float pressSpeed = 0.05f;


    private void Update()
    {
        if (isSelected)
        {
            value = Mathf.MoveTowards(value, 1f, Time.unscaledDeltaTime / speed);
        }
        else if (!isSelected)
        {
            value = Mathf.MoveTowards(value, 0f, Time.unscaledDeltaTime / speed);
        }

        float easedValue = 1f - (1f - value) * (1f - value); // ease-out

        selectedFrame.alpha = easedValue;
        defaultFrame.alpha = 1f - easedValue;
        arrows.alpha = easedValue;
        visual.anchoredPosition = new Vector2(slideDistance * easedValue, 0f); // Pos X
        label.color = Color.Lerp(defaultColor, selectedColor, easedValue);

        if (number != null)
        {
            number.color = Color.Lerp(defaultColor, selectedColor, easedValue);
        }

        if (fill != null)
        {
            fill.color = Color.Lerp(defaultColor, selectedColor, easedValue);
        }

        if (handle != null)
        {
            handle.color = Color.Lerp(defaultColor, selectedColor, easedValue);
        }

        if (isPressed)
        {
            pressValue = Mathf.MoveTowards(pressValue, 1f, Time.unscaledDeltaTime / pressSpeed);
        }
        else if (!isPressed)
        {
            pressValue = Mathf.MoveTowards(pressValue, 0f, Time.unscaledDeltaTime / pressSpeed);
        }

        float easedPressValue = 1f - (1f - pressValue) * (1f - pressValue); // ease-out

        float scale = Mathf.Lerp(1f, pressedScale, easedPressValue);
        visual.localScale = new Vector3(scale, scale, 1f);
    }

    public void OnSelected()
    {
        isSelected = true;
    }

    public void OnDeselected()
    {
        isSelected = false;
    }

    public void OnPressed()
    {
        isPressed = true;
    }

    public void OnReleased()
    {
        isPressed = false;
    }

    public void OnHover()
    {
        EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnHoverExit()
    {
        if (EventSystem.current.currentSelectedGameObject == gameObject)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void OnEnable()
    {
        isPressed = false;
        pressValue = 0f;
        value = 0f;
    }

    public void UpdateNumber(float sliderValue)
    {
        number.text = Mathf.RoundToInt(sliderValue).ToString();
    }
}