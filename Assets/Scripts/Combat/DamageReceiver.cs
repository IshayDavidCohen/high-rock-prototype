using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public sealed class DamageReceiver : MonoBehaviour
{

    private Health _health;
    private BlockController _block;

    public bool IsDead => _health.IsDead;

    private void Awake()
    {
        _health = GetComponent<Health>();
        TryGetComponent(out _block);
    }

    public void ReceiveHit(in DamageInfo hit)
    {
        if (_health.IsDead || hit.Damage <= 0f)
            return;

        float damageMultiplier = 1f;

        if (_block != null)
        {
            damageMultiplier = _block.ResolveDamageMultiplier(hit);
        }

        float resolvedDamage = hit.Damage * damageMultiplier;

        _health.TakeDamage(resolvedDamage, hit.DamageType, hit.Source);
    }
}
