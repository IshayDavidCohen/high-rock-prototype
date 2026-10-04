using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyPerception : MonoBehaviour
{
    [Header("Sight")]
    [SerializeField, Min(0f)]
    private float detectionSightRange = 10f;

    [SerializeField, Min(0f)]
    private float engagedSightRange = 18f;

    [SerializeField, Range(0f, 360f)]
    private float fieldOfView = 120f;

    [SerializeField]
    private float eyeHeight = 0.7f;

    [SerializeField]
    private float targetHeightOffset = 0.5f;

    [SerializeField]
    private LayerMask occlusionLayers;

    private Transform _target;
    private float _lastKnownTime = float.NegativeInfinity;

    private float _activeSightRange;

    public float ActiveSightRange => _activeSightRange;

    public bool CanSeeTarget { get; private set; }
    public bool HasSeenTarget { get; private set; }
    
    public Vector3 LastKnownTargetPosition { get; private set; }

    public float TimeSinceTargetKnown => Time.time - _lastKnownTime;

    public void SetTarget(Transform target)
    {
        _target = target;
    }

    public void RegisterTargetStimulus(Vector3 position)
    {
        LastKnownTargetPosition = position;
        _lastKnownTime = Time.time;
    }

    private void Awake()
    {
        _activeSightRange = detectionSightRange;
    }

    public void Refresh(bool isEngaged)
    {
        CanSeeTarget = false;

        if (_target == null)
            return;

        _activeSightRange = isEngaged ? engagedSightRange : detectionSightRange;

        Vector3 origin = transform.position + Vector3.up * eyeHeight;

        Vector3 targetPoint = _target.position + Vector3.up * targetHeightOffset;

        Vector3 toTarget = targetPoint - origin;

        Vector3 flatDirection = toTarget;
        flatDirection.y = 0f;

        float squaredDistance = flatDirection.sqrMagnitude;

        if (squaredDistance > _activeSightRange * _activeSightRange)
            return;

        if (flatDirection.sqrMagnitude < 0.0001f)
            return;

        Vector3 normalizedDirection = flatDirection.normalized;

        float halfFov = fieldOfView * 0.5f;

        float minimumDot = Mathf.Cos(halfFov * Mathf.Deg2Rad);

        float facingDot = Vector3.Dot(transform.forward, normalizedDirection);

        if (facingDot < minimumDot)
            return;

        float rayDistance = toTarget.magnitude;

        bool blocked = Physics.Raycast(
            origin,
            toTarget.normalized,
            rayDistance,
            occlusionLayers,
            QueryTriggerInteraction.Ignore
        );

        if (blocked)
            return;

        CanSeeTarget = true;
        HasSeenTarget = true;

        RegisterTargetStimulus(_target.position);

    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, detectionSightRange);

        Vector3 leftBoundry = Quaternion.AngleAxis(-fieldOfView * 0.5f, Vector3.up) * transform.forward;
        Vector3 rightBoundry = Quaternion.AngleAxis(fieldOfView * 0.5f, Vector3.up) * transform.forward;

        Gizmos.DrawLine(transform.position, transform.position + leftBoundry * detectionSightRange);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundry * detectionSightRange);

    }
}
