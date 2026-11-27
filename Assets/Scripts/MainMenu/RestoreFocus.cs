using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RestoreFocus : MonoBehaviour
{
    [SerializeField] private Selectable defaultSelectable;

    private void Update()
    {
        if (EventSystem.current.currentSelectedGameObject == null || !EventSystem.current.currentSelectedGameObject.activeInHierarchy)
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
                {
                    EventSystem.current.SetSelectedGameObject(defaultSelectable.gameObject);
                }
            }
            
            if (Gamepad.current != null)
            {
                if (Gamepad.current.dpad.up.wasPressedThisFrame || Gamepad.current.dpad.down.wasPressedThisFrame)
                {
                    EventSystem.current.SetSelectedGameObject(defaultSelectable.gameObject);
                }
            }
        }
    }
}