using UnityEngine;

public readonly struct DamageInfo
{
    public float Damage { get; }
    public float GuardDamage { get; }

    public DamageType DamageType { get; }

    public GameObject Source { get; }
    
    public Vector3 Origin { get; }

    public bool CanBeBlocked { get; }

    public DamageInfo(
        float damage,
        float guardDamage,
        DamageType damageType,
        GameObject source,
        Vector3 origin,
        bool canBeBlocked = true)
    {
        Damage = damage;
        GuardDamage = guardDamage;
        DamageType = damageType;
        Source = source;
        Origin = origin;
        CanBeBlocked = canBeBlocked;
    }
}
