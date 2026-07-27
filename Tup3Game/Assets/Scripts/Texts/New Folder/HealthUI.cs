using UnityEngine;
using TMPro;

/*public class HealthUI : MonoBehaviour
{
    public PlayerHealth playerHealth;
    public TMP_Text healthText;

    void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged += UpdateHealthUI;
    }
    void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= UpdateHealthUI;
    }
    void Start()
    {
        UpdateHealthUI(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }
    void UpdateHealthUI(float current, float max)
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthUI;
            healthText.text = $"{current:F0} / {max:F0}";
        }
    }
}*/