using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Stamina))]
[RequireComponent(typeof(StaggerController))]
public sealed class BlockController : MonoBehaviour
{

    [Header("Block")]
    [SerializeField, Range(0f, 180f)]
    private float blockAngle = 120f;

    [SerializeField, Range(0f, 1f)]
    private float blockedDamageMultiplier = 0.3f;

    [SerializeField, Range(0f, 1f)]
    private float blockingMovementSpeedMultipler = 0.5f;

    [SerializeField, Range(0f, 1f)]
    private float blockingStaminaRegenMultiplier = 0.35f;

    [Header("Guard Break")]
    [SerializeField, Min(0f)]
    private float guardBreakStaggerDuration = 0.6f;

    public bool IsBlocking { get; private set; }

    public float MovementSpeedMultiplier => IsBlocking ? blockingMovementSpeedMultipler : 1f;

    public event Action GuardBroken;

    private Stamina _stamina;

    private StaggerController _stagger;

    private void Awake()
    {
        _stamina = GetComponent<Stamina>();
        _stagger = GetComponent<StaggerController>();
    }

    private void OnEnable()
    {
        _stagger.StaggerStarted += HandleStaggerStarted;
    }

    private void OnDisable()
    {
        _stagger.StaggerStarted -= HandleStaggerStarted;

        StopBlocking();
    }

    public void SetBlocking(bool shouldBlock)
    {
        if (!shouldBlock)
        {
            StopBlocking();
            return;
        }

        if (IsBlocking)
            return;

        if (_stagger.IsStaggered || _stamina.IsEmpty)
            return;

        IsBlocking = true;

        _stamina.SetRegenerationMultiplier(blockingStaminaRegenMultiplier);
    }

    public void ForceStopBlocking()
    {
        StopBlocking();
    }

    public float ResolveDamageMultiplier(in DamageInfo hit)
    {
        if (!IsBlocking)
            return 1f;

        if (!hit.CanBeBlocked)
            return 1f;

        if (!IsWithinBlockArc(hit.Origin))
            return 1f;

        if (_stamina.IsEmpty)
        {
            StopBlocking();
            return 1f;
        }

        bool paidGuardCost = _stamina.TrySpend(hit.GuardDamage);

        if (!paidGuardCost)
        {
            _stamina.Exhaust();
            BreakGuard(hit.Origin);
            return blockedDamageMultiplier;
        }

        // Reaching exactly zero also breaks guard.
        if (_stamina.IsEmpty)
        {
            StopBlocking();
        }

        return blockedDamageMultiplier;
    }

    private bool IsWithinBlockArc(Vector3 attackOrigin)
    {
        Vector3 direction = attackOrigin - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return true;

        direction.Normalize();

        float minimumDot = Mathf.Cos(blockAngle * 0.5f * Mathf.Deg2Rad);

        float facingDot = Vector3.Dot(transform.forward, direction);

        return facingDot >= minimumDot;
    }

    private void BreakGuard(Vector3 attackOrigin)
    {
        StopBlocking();

        _stagger.ApplyStagger(guardBreakStaggerDuration, attackOrigin);

        GuardBroken?.Invoke();
    }

    private void StopBlocking()
    {
        if (!IsBlocking)
            return;

        IsBlocking = false;

        _stamina.SetRegenerationMultiplier(1f);
    }

    private void HandleStaggerStarted(Vector3 sourcePosition, float duration)
    {
        StopBlocking();
    }
}
