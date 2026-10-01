using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PlayerHealthBar : MonoBehaviour
{
    [SerializeField]
    private Health playerHealth;

    [SerializeField]
    private Image fillImage;

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged += HandleHealthChanged;
        }
    }

    private void Start()
    {
        if (playerHealth == null || fillImage == null)
        {
            Debug.LogError(
                $"{nameof(PlayerHealthBar)} is missing references.",
                this
            );

            enabled = false;
            return;
        }

        UpdateBar(
            playerHealth.CurrentHealth,
            playerHealth.MaxHealth
        );
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged -= HandleHealthChanged;
        }
    }

    private void HandleHealthChanged(
        float currentHealth,
        float maxHealth)
    {
        UpdateBar(currentHealth, maxHealth);
    }

    private void UpdateBar(
        float currentHealth,
        float maxHealth)
    {
        fillImage.fillAmount =
            maxHealth > 0f
                ? Mathf.Clamp01(currentHealth / maxHealth)
                : 0f;
    }
}