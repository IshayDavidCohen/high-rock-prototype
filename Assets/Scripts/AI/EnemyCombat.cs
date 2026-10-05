using UnityEngine;


[DisallowMultipleComponent]
[RequireComponent(typeof(Stamina))]
[RequireComponent(typeof(BlockController))]
[RequireComponent(typeof(StaggerController))]

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

    [SerializeField, Min(0f)]
    private float attackStaminaCost = 15f;

    [Header("Block")]
    [SerializeField, Min(0f)]
    private float attackGuardDamage = 15f;
    private DamageReceiver _targetReceiver;

    private StaggerController _stagger;
    private BlockController _block;


    [Header("Exhausted Attack")]
    [SerializeField, Range(0f, 1f)]
    private float exhaustedDamageMultiplier = 0.5f;

    [SerializeField, Min(1f)]
    private float exhaustedCooldownMultiplier = 1.4f;

    [SerializeField]
    private DamageType damageType = DamageType.Physical;

    private Health _targetHealth;
    private float _nextAttackTime;

    public float AttackRange => attackRange;

    private Stamina _stamina;

    private void Awake()
    {
        _stamina = GetComponent<Stamina>();
        _block = GetComponent<BlockController>();
        _stagger = GetComponent<StaggerController>();

    }

    public void SetTarget(Health targetHealth)
    {
        _targetHealth = targetHealth;

        if (_targetHealth == null)
        {
            _targetReceiver = null;
            return;
        }

        _targetReceiver = targetHealth != null ? targetHealth.GetComponent<DamageReceiver>() : null;

        // Debug sake
        if (_targetReceiver == null)
        {
            Debug.LogError(
                $"{_targetHealth.name} has Health but no DamageReceiver.",
                _targetHealth
            );
        }
    }

    public bool IsTargetInRange(Vector3 targetPosition)
    {
        Vector3 difference = targetPosition - transform.position;

        difference.y = 0f;

        return difference.sqrMagnitude <= attackRange * attackRange;
    }

    public bool TryAttack()
    {
        if (_targetHealth == null || _targetReceiver == null || _targetHealth.IsDead || Time.time < _nextAttackTime)
            return false;

        if (_block.IsBlocking || _stagger.IsStaggered)
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

        bool hasEnoughStamina = _stamina.TrySpend(attackStaminaCost);

        float resolvedDamage =
            hasEnoughStamina
                ? attackDamage
                : attackDamage *
                  exhaustedDamageMultiplier;

        float resolvedCooldown =
            hasEnoughStamina
                ? attackCooldown
                : attackCooldown *
                  exhaustedCooldownMultiplier;

        _nextAttackTime = Time.time + resolvedCooldown;

        DamageInfo hitInfo = new DamageInfo(resolvedDamage, attackGuardDamage, damageType, gameObject, transform.position, true);

        _targetReceiver.ReceiveHit(hitInfo);

        //_targetHealth.TakeDamage(resolvedDamage, damageType, gameObject);

        return true;
    }
}
