using UnityEngine;


[DisallowMultipleComponent]
public sealed class EnemyCombat : MonoBehaviour
{

    [Header("Attack")]
    [SerializeField, Min(0f)]
    private float attackDamage = 10f;

    [SerializeField, Min(0f)]
    private float attackRange = 1.5f;

    [SerializeField, Range(0f, 180f)]
    private float attackAngle = 100f;

    [SerializeField, Min(0f)]
    private float attackCooldown = 1f;

    [SerializeField]
    private DamageType damageType = DamageType.Physical;

    private Health _targetHealth;
    private float _nextAttackTime;

    public float AttackRange => attackRange;

    public void SetTarget(Health targetHealth)
    {
        _targetHealth = targetHealth;
    }

    public bool IsTargetInRange(Vector3 targetPosition)
    {
        Vector3 difference = targetPosition - transform.position;

        difference.y = 0f;

        return difference.sqrMagnitude <= attackRange * attackRange;
    }

    public bool TryAttack()
    {
        if (_targetHealth == null || _targetHealth.IsDead || Time.time < _nextAttackTime)
            return false;

        Vector3 direction = _targetHealth.transform.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > attackRange * attackRange)
            return false;

        direction.Normalize();

        float minimumDot = Mathf.Cos(attackAngle * 0.5f * Mathf.Deg2Rad);

        float facingDot = Vector3.Dot(transform.forward, direction);

        if (facingDot < minimumDot)
            return false;

        _nextAttackTime = Time.time + attackCooldown;

        _targetHealth.TakeDamage(attackDamage, damageType, gameObject);

        return true;
    }
}
