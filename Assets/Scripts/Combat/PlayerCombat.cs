using System.Collections.Generic;
using UnityEngine;


[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInputReader))]
[RequireComponent(typeof(Stamina))]
public sealed class PlayerCombat : MonoBehaviour
{
    private const int HitBufferSize = 16;

    [Header("Attack")]
    [SerializeField, Min(0f)]
    private float attackDamage = 25f;

    [SerializeField, Min(0f)]
    private float attackRange = 2f;

    [SerializeField, Range(0f, 180f)]
    private float attackAngle = 90f;

    [SerializeField, Min(0f)]
    private float attackCooldown = 0.4f;

    [SerializeField, Min(0f)]
    private float attackStaminaCost = 15f;

    [SerializeField, Range(0f, 1f)]
    private float exhaustedDamageMultiplier = 0.5f;

    [Header("Detection")]
    [SerializeField]
    private LayerMask damageableLayers;

    [SerializeField]
    private LayerMask attackOcclusionLayers;

    private readonly Collider[] _hitBuffer = new Collider[HitBufferSize];
    private readonly HashSet<Health> _damagedThisAttack = new HashSet<Health>();

    private PlayerInputReader _input;

    private float _nextAttackTime;

    private Stamina _stamina;

    void Awake()
    {
        _input = GetComponent<PlayerInputReader>();
        _stamina = GetComponent<Stamina>();
    }

    void Update()
    {
        if (!_input.AttackPressedThisFrame)
            return;

        if (Time.time < _nextAttackTime)
            return;

        bool hasEnoughStamina = _stamina.TrySpend(attackStaminaCost);

        float resolvedDamage = hasEnoughStamina ? attackDamage : attackDamage * exhaustedDamageMultiplier;

        PerformAttack(resolvedDamage);
    }

    private void PerformAttack(float resolvedDamage)
    {
        _nextAttackTime = Time.time + attackCooldown;
        _damagedThisAttack.Clear();

        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            attackRange,
            _hitBuffer,
            damageableLayers,
            QueryTriggerInteraction.Ignore
        );

        if (hitCount == _hitBuffer.Length)
        {
            Debug.LogWarning(
                $"{nameof(PlayerCombat)} hit buffer is full.",
                this
            );
        }

        float halfAngle = attackAngle * 0.5f;

        float minimumDot = Mathf.Cos(halfAngle * Mathf.Deg2Rad);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _hitBuffer[i];

            Health health = hit.GetComponentInParent<Health>();

            if (health == null || health.IsDead)
                continue;

            Vector3 targetPosition = hit.ClosestPoint(transform.position);
            Vector3 directionToTarget = targetPosition - transform.position;

            directionToTarget.y = 0f;

            if (directionToTarget.sqrMagnitude < 0.0001f)
            {
                directionToTarget = health.transform.position - transform.position;
                directionToTarget.y = 0f;
            }

            if (directionToTarget.sqrMagnitude < 0.0001f)
                continue;

            directionToTarget.Normalize();

            float facingDot = Vector3.Dot(transform.forward,  directionToTarget);

            if (facingDot < minimumDot)
                continue;

            Vector3 attackOrigin = transform.position + Vector3.up * 0.7f;
            Vector3 targetPoint = hit.bounds.center;
            Vector3 toTarget = targetPoint - attackOrigin;

            if (toTarget.sqrMagnitude > 0.0001f)
            {
                bool blocked = Physics.Raycast(
                    attackOrigin,
                    toTarget.normalized,
                    toTarget.magnitude,
                    attackOcclusionLayers,
                    QueryTriggerInteraction.Ignore
                );

                if (blocked)
                    continue;
            }

            if (!_damagedThisAttack.Add(health))
                continue;

            health.TakeDamage(resolvedDamage, DamageType.Physical, gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            attackRange
        );

        Vector3 leftBoundry = Quaternion.AngleAxis(-attackAngle * 0.5f, Vector3.up) * transform.forward;
        Vector3 rightBoundry = Quaternion.AngleAxis(attackAngle * 0.5f, Vector3.up) * transform.forward;

        Gizmos.DrawLine(
            transform.position,
            transform.position + leftBoundry * attackRange
        );

        Gizmos.DrawLine(
            transform.position,
            transform.position + rightBoundry * attackRange
        );
    }
}
