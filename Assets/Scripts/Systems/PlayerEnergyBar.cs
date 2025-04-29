using UnityEngine;
using UnityEngine.UI;

/// <summary>Simple helper that controls the Energy slider in the HUD.</summary>
public class PlayerEnergyBar : MonoBehaviour
{
    public Slider _slider;

    public void SetMaxEnergy(float max)
    {
        _slider.maxValue = max;
        _slider.value    = max;
    }

    public void SetEnergy(float value)
    {
        _slider.value = value;
    }
}