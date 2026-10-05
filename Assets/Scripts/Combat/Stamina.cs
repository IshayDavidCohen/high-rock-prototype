using System;
using UnityEngine;


[DisallowMultipleComponent]
public sealed class Stamina : MonoBehaviour
{
    [Header("Stamina")]
    [SerializeField, Min(1f)]
    private float maxStamina = 100f;

    [SerializeField, Min(0f)]
    private float regenerationPerSecond = 20f;

    [SerializeField, Min(0f)]
    private float regenerationDelay = 1f;

    public float CurrentStamina { get; private set; }

    public float MaxStamina => maxStamina;

    public bool IsEmpty => CurrentStamina <= 0f;

    public bool IsFull => CurrentStamina >= maxStamina;

    public event Action<float, float> StaminaChanged;

    private float _lastSpendTime;

    private float _regenerationMultiplier = 1f;

    public void SetRegenerationMultiplier(float multiplier)
    {
        _regenerationMultiplier = Mathf.Max(0f, multiplier);
    }

    private void Awake()
    {
        CurrentStamina = maxStamina;
    }

    private void Update()
    {
        Regenerate();
    }

    public bool TrySpend(float amount)
    {
        if (amount <= 0f)
            return true;

        if (CurrentStamina < amount)
            return false;

        CurrentStamina -= amount;
        _lastSpendTime = Time.time;

        StaminaChanged?.Invoke(CurrentStamina, maxStamina);

        return true;
    }

    public void Restore(float amount)
    {
        if (amount <= 0f || IsFull)
            return;

        CurrentStamina = Mathf.Min(CurrentStamina + amount, maxStamina);

        StaminaChanged?.Invoke(CurrentStamina, maxStamina);
    }

    private void Regenerate()
    {
        if (IsFull)
            return;

        if (Time.time < _lastSpendTime + regenerationDelay)
            return;

        Restore(regenerationPerSecond * _regenerationMultiplier * Time.deltaTime);
    }

    public void Exhaust()
    {
        if (IsEmpty)
            return;

        CurrentStamina = 0f;
        _lastSpendTime = Time.time;

        StaminaChanged?.Invoke(CurrentStamina, maxStamina);
    }
}
