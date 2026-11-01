using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(Slider))]
public class HealthBar : MonoBehaviour
{
    public Slider slider;

    void Reset()
    {
        slider = GetComponent<Slider>();
    }

    void Awake()
    {
        if (slider == null)
        {
            slider = GetComponent<Slider>();
        }
    }

    public void SetMaxHealth(float maxHealth)
    {
        slider.minValue = 0f;
        slider.maxValue = maxHealth;
        slider.value = maxHealth;
    }

    public void SetHealth(float currentHealth)
    {
        slider.value = currentHealth;
    }
}
