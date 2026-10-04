using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class WorldHealthBar : MonoBehaviour
{
    [SerializeField]
    private Image fillImage;

    [SerializeField]
    private Camera gameplayCamera;

    private Health _health;

    private void Awake()
    {
        _health = GetComponentInParent<Health>();

        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }

        if (_health == null)
        {
            Debug.LogError(
                $"{nameof(WorldHealthBar)} could not find Health in its parent.",
                this
            );

            enabled = false;
            return;
        }

        if (fillImage == null)
        {
            Debug.LogError(
                $"{nameof(WorldHealthBar)} requires a Fill Image.",
                this
            );

            enabled = false;
            return;
        }

        if (gameplayCamera == null)
        {
            Debug.LogError(
                $"{nameof(WorldHealthBar)} requires a gameplay camera.",
                this
            );

            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (_health != null)
        {
            _health.HealthChanged += HandleHealthChanged;
        }
    }

    private void Start()
    {
        UpdateBar(
            _health.CurrentHealth,
            _health.MaxHealth
        );
    }

    private void OnDisable()
    {
        if (_health != null)
        {
            _health.HealthChanged -= HandleHealthChanged;
        }
    }

    private void LateUpdate()
    {
        transform.rotation =
            gameplayCamera.transform.rotation;
    }

    private void HandleHealthChanged(
        float currentHealth,
        float maxHealth)
    {
        UpdateBar(
            currentHealth,
            maxHealth
        );
    }

    private void UpdateBar(
        float currentHealth,
        float maxHealth)
    {
        if (maxHealth <= 0f)
        {
            fillImage.fillAmount = 0f;
            return;
        }

        fillImage.fillAmount =
            Mathf.Clamp01(
                currentHealth / maxHealth
            );
    }
}