using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Health : MonoBehaviour
{

    [Header("Health")]
    [SerializeField, Min(1f)]
    private float maxHealth = 100f;

    [SerializeField, Min(0f)]
    private float healthRegenPerSecond = 0f;

    public float CurrentHealth { get; private set; }

    public float MaxHealth => maxHealth;

    public bool IsDead => CurrentHealth <= 0f;

    public bool IsFullHealth => CurrentHealth >= maxHealth;

    public event Action<float, float> HealthChanged;
    public event Action Died;

    private Defense _defense;
    
    void Awake()
    {
        CurrentHealth = maxHealth;

        TryGetComponent(out _defense);
    }

    private void Update()
    {
        RegenerateHealth();
    }

    public void TakeDamage(float amount, DamageType damageType)
    {
        if (amount <= 0f || IsDead)
            return;

        float resolvedDamage = _defense != null ? _defense.ResolveDamage(amount, damageType) : amount;
        if (resolvedDamage <= 0f)
            return;

        CurrentHealth = Mathf.Max(CurrentHealth - resolvedDamage, 0f);

        HealthChanged?.Invoke(CurrentHealth, maxHealth);

        Debug.Log(
            $"{name} took {resolvedDamage:0.##} {damageType} damage " +
            $"(incoming: {amount:0.##}). " +
            $"Health: {CurrentHealth:0.##}/{maxHealth:0.##}",
            this
        );

        if (!IsDead)
            return;

        Died?.Invoke();
        Debug.Log(
            $"{name} died.",
            this
        );
    }

    public void Heal(float amount) 
    {
        if (amount <= 0f || IsDead || IsFullHealth)
            return;

        CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    private void RegenerateHealth()
    {
        if (healthRegenPerSecond <= 0f || IsDead || IsFullHealth)
            return;

        Heal(healthRegenPerSecond * Time.deltaTime);
    }
}
