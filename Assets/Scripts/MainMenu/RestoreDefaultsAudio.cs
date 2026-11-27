using UnityEngine;
using UnityEngine.UI;

public class RestoreDefaults : MonoBehaviour
{
    [SerializeField] private Slider masterVolume;
    [SerializeField] private Slider soundVolume;
    [SerializeField] private Slider musicVolume;

    public void Restore()
    {
        masterVolume.value = 8f;
        soundVolume.value = 8f;
        musicVolume.value = 8f;
    }
}