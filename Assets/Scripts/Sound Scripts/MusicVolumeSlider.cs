using UnityEngine;
using UnityEngine.UI;

// Controls the MusicManager's volume.
[RequireComponent(typeof(Slider))]
public class MusicVolumeSlider : MonoBehaviour
{
    private Slider slider;

    private void OnEnable()
    {
        if (slider == null) slider = GetComponent<Slider>();

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.onValueChanged.AddListener(OnSliderChanged);

        ShowCurrentVolume();
    }

    private void Start()
    {
        ShowCurrentVolume();
    }

    private void OnDisable()
    {
        slider.onValueChanged.RemoveListener(OnSliderChanged);
    }

    private void ShowCurrentVolume()
    {
        if (MusicManager.Instance != null)
        {
            slider.SetValueWithoutNotify(MusicManager.Instance.Volume);
        }
    }

    private void OnSliderChanged(float value)
    {
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetVolume(value);
        }
    }
}