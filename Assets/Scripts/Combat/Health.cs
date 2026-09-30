using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Health : MonoBehaviour
{
    [SerializeField, Min(1f)]
    private float maxHealth = 100f;

    public float CurrentHealth { get; private set; }

    public float MaxHealth => maxHealth;

    public bool IsDead => CurrentHealth <= 0f;

    public event Action<float, float> HealthChanged;
    public event Action Died;
    
    void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f || IsDead)
            return;

        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0f);

        HealthChanged?.Invoke(CurrentHealth, maxHealth);

        Debug.Log(
            $"{name} took {amount} damage. " +
            $"Health: {CurrentHealth}/{maxHealth}",
            this
        );

        if (IsDead)
        {
            Died?.Invoke();
            Debug.Log(
                $"{name} died.",
                this
            );
        }
    }
}
