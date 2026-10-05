using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyPerception))]
[RequireComponent(typeof(EnemyCombat))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(StaggerController))]
[RequireComponent(typeof(BlockController))]
public sealed class EnemyBrain : MonoBehaviour
{
    private enum EnemyState
    {
        Idle,
        Chase,
        Attack,
        Block,
        Search,
        Return
    }

    [Header("Target")]
    [SerializeField]
    private Transform target;

    [Header("Behaviour")]
    [SerializeField, Min(0f)]
    private float lostSightGracePeriod = 2f;

    [SerializeField, Min(0f)]
    private float searchDuration = 2f;

    [SerializeField, Min(0f)]
    private float leashDistance = 25f;

    [SerializeField, Min(0f)]
    private float destinationTolerance = 0.15f;

    [SerializeField, Min(0f)]
    private float repathInterval = 0.15f;

    [SerializeField, Min(0f)]
    private float attackTurnSpeed = 540f;

    [SerializeField, Min(0f)]
    private float searchTurnSpeed = 90f;

    [Header("Blocking")]
    [SerializeField, Range(0f, 1f)]
    private float blockChance = 0.25f;

    [SerializeField, Min(0.1f)]
    private float blockDecisionInterval = 1.25f;

    [SerializeField, Min(0.1f)]
    private float minBlockDuration = 1.5f;

    [SerializeField, Min(0.1f)]
    private float maxBlockDuration = 3f;

    [SerializeField, Range(0f, 1f)]
    private float retreatWhileBlockingChance = 0.8f;

    [SerializeField, Min(0f)]
    private float blockRetreatDistance = 3f;

    [SerializeField, Range(0.1f, 1f)]
    private float blockMoveSpeedMultiplier = 0.5f;
    private BlockController _block;
    private float _nextBlockDecisionTime;
    private float _blockTimer;
    private bool _blockRetreating;

    [Header("Stagger")]
    private StaggerController _stagger;

    [SerializeField, Min(0f)]
    private float staggerRecoilDistance = 2f;

    [SerializeField, Min(0.01f)]
    private float staggerRecoilDuration = 0.18f;

    private Vector3 _staggerRecoilDirection;
    private float _staggerRecoilTimeRemaining;



    // Used not just in blocking, but for speed of enemy.
    private float _defaultAgentSpeed;


    [Header("Debug")]
    [SerializeField]
    private bool logStateChanges = true;

    private NavMeshAgent _agent;
    private EnemyPerception _perception;
    private EnemyCombat _combat;
    private Health _health;

    private Health _targetHealth;

    private EnemyState _state;

    private Vector3 _homePosition;

    private float _nextRepathTime;
    private float _searchTimer;

    private bool _searchPointReached;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _perception = GetComponent<EnemyPerception>();
        _combat = GetComponent<EnemyCombat>();
        _health = GetComponent<Health>();
        _stagger = GetComponent<StaggerController>();
        _block = GetComponent<BlockController>();

        _defaultAgentSpeed = _agent.speed;

        _homePosition = transform.position;
    }

    private void OnEnable()
    {
        _health.Damaged += HandleDamaged;
        _stagger.StaggerStarted += HandleStaggerStarted;
    }

    private void OnDisable()
    {
        _health.Damaged -= HandleDamaged;
        _stagger.StaggerStarted -= HandleStaggerStarted;
    }

    private void Start()
    {
        if (!_agent.isOnNavMesh)
        {
            Debug.LogError(
                $"{name} is not standing on a NavMesh.",
                this
            );

            enabled = false;
            return;
        }

        if (!BindTarget())
        {
            enabled = false;
            return;
        }

        _agent.autoBraking = true;

        ChangeState(EnemyState.Idle);
    }

    private void Update()
    {
        if (_stagger.IsStaggered)
        {
            StopAgent();
            UpdateStaggerRecoil();
            return;
        }

        if (_targetHealth == null ||
            _targetHealth.IsDead)
        {
            if (_state != EnemyState.Idle &&
                _state != EnemyState.Return)
            {
                ChangeState(EnemyState.Return);
            }

            if (_state == EnemyState.Return)
            {
                UpdateReturn();
            }

            return;
        }

        bool isEngaged = _state == EnemyState.Chase || _state == EnemyState.Attack || _state == EnemyState.Search || _state == EnemyState.Block;
        _perception.Refresh(isEngaged);

        switch (_state)
        {
            case EnemyState.Idle:
                UpdateIdle();
                break;

            case EnemyState.Chase:
                UpdateChase();
                break;

            case EnemyState.Attack:
                UpdateAttack();
                break;

            case EnemyState.Block:
                UpdateBlock();
                break;

            case EnemyState.Search:
                UpdateSearch();
                break;

            case EnemyState.Return:
                UpdateReturn();
                break;
        }
    }

    public void Initialize(Transform newTarget)
    {
        target = newTarget;

        if (_perception != null &&
            _combat != null)
        {
            BindTarget();
        }
    }

    private bool BindTarget()
    {
        if (target == null)
        {
            Debug.LogError(
                $"{nameof(EnemyBrain)} requires a target.",
                this
            );

            return false;
        }

        if (!target.TryGetComponent(
                out _targetHealth))
        {
            Debug.LogError(
                $"{target.name} needs a Health component.",
                target
            );

            return false;
        }

        _perception.SetTarget(target);
        _combat.SetTarget(_targetHealth);

        return true;
    }

    private void UpdateIdle()
    {
        if (!_perception.CanSeeTarget)
        {
            return;
        }

        if (!IsWithinLeash(target.position))
        {
            return;
        }

        ChangeState(EnemyState.Chase);
    }

    private void UpdateChase()
    {
        _agent.isStopped = false;

        if (!IsWithinLeash(transform.position))
        {
            ChangeState(EnemyState.Return);
            return;
        }

        Vector3 pursuitPosition = _perception.CanSeeTarget ? target.position : _perception.LastKnownTargetPosition;
        UpdateChaseDestination(pursuitPosition);
        UpdateChaseFacing(pursuitPosition);


        if (_perception.CanSeeTarget && _combat.IsTargetInRange(target.position))
        {
            ChangeState(EnemyState.Attack);
            return;
        }

        if (!_perception.CanSeeTarget && _perception.TimeSinceTargetKnown >= lostSightGracePeriod)
        {
            if (IsWithinLeash(_perception.LastKnownTargetPosition))
            {
                ChangeState(EnemyState.Search);
            }
            else
            {
                ChangeState(EnemyState.Return);
            }
        }
    }

    private void UpdateAttack()
    {
        if (!IsWithinLeash(transform.position))
        {
            ChangeState(EnemyState.Return);
            return;
        }

        if (!_perception.CanSeeTarget)
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        if (!_combat.IsTargetInRange(
                target.position))
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        if (Time.time >= _nextBlockDecisionTime)
        {
            _nextBlockDecisionTime = Time.time + blockDecisionInterval;

            if (Random.value < blockChance)
            {
                ChangeState(EnemyState.Block);
                return;
            }
        }

        FaceTarget();

        _combat.TryAttack();
    }

    private void UpdateBlock()
    {
        if (!_perception.CanSeeTarget)
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        if (!_block.IsBlocking)
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        FaceTarget();

        _blockTimer -= Time.deltaTime;

        if (_blockTimer <= 0f)
        {
            ChangeState(
                _combat.IsTargetInRange(target.position)
                    ? EnemyState.Attack
                    : EnemyState.Chase
            );
        }
    }

    private void UpdateSearch()
    {
        if (_perception.CanSeeTarget &&
            IsWithinLeash(target.position))
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        if (!_searchPointReached)
        {
            if (!HasReachedDestination())
            {
                return;
            }

            _searchPointReached = true;

            _agent.isStopped = true;
            _agent.updateRotation = false;
        }

        _searchTimer += Time.deltaTime;

        transform.Rotate(
            Vector3.up,
            searchTurnSpeed * Time.deltaTime,
            Space.World
        );

        if (_searchTimer >= searchDuration)
        {
            ChangeState(EnemyState.Return);
        }
    }

    private void UpdateReturn()
    {
        // Hysteresis: don't immediately re-engage
        // at the outer leash boundary.
        float reengageDistance =
            leashDistance * 0.75f;

        if (_perception.CanSeeTarget &&
            HorizontalDistance(
                _homePosition,
                target.position
            ) <= reengageDistance)
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        if (!HasReachedDestination())
        {
            return;
        }

        ChangeState(EnemyState.Idle);
    }

    private void ChangeState(EnemyState newState)
    {
        if (_state == newState)
        {
            return;
        }

        if (_state == EnemyState.Block)
        {
            _block.SetBlocking(false);
            _agent.speed = _defaultAgentSpeed;
        }

        if (logStateChanges)
        {
            Debug.Log(
                $"{name}: {_state} → {newState}",
                this
            );
        }

        _state = newState;

        _agent.updateRotation = true;

        switch (_state)
        {
            case EnemyState.Idle:
                StopAgent();
                break;

            case EnemyState.Chase:
                _agent.isStopped = false;
                _agent.updateRotation = false;

                _agent.stoppingDistance =
                    _combat.AttackRange * 0.9f;

                _nextRepathTime = 0f;
                break;

            case EnemyState.Attack:
                StopAgent();
                _agent.updateRotation = false;
                break;

            case EnemyState.Block:
                _blockTimer = Random.Range(minBlockDuration, maxBlockDuration);
                _blockRetreating = Random.value < retreatWhileBlockingChance;
                _block.SetBlocking(true);

                _agent.updateRotation = false;

                if (_blockRetreating)
                {
                    _agent.isStopped = false;
                    _agent.speed = _defaultAgentSpeed * blockMoveSpeedMultiplier;

                    Vector3 awayFromPlayer = transform.position - target.position;

                    awayFromPlayer.y = 0f;
                    awayFromPlayer.Normalize();

                    Vector3 retreatPosition = transform.position + awayFromPlayer * blockRetreatDistance;

                    _agent.SetDestination(retreatPosition);
                }
                else
                {
                    StopAgent();
                    _agent.updateRotation = false;
                }

                break;

            case EnemyState.Search:
                _searchTimer = 0f;
                _searchPointReached = false;

                _agent.updateRotation = true;
                _agent.isStopped = false;

                _agent.stoppingDistance =
                    destinationTolerance;

                _agent.SetDestination(
                    _perception
                        .LastKnownTargetPosition
                );

                break;

            case EnemyState.Return:
                _agent.updateRotation = true;
                _agent.isStopped = false;

                _agent.stoppingDistance =
                    destinationTolerance;

                _agent.SetDestination(
                    _homePosition
                );

                break;
        }
    }

    private void UpdateChaseFacing(Vector3 pursuitPosition)
    {
        Vector3 desiredVelocity = _agent.desiredVelocity;
        desiredVelocity.y = 0f;

        if (desiredVelocity.sqrMagnitude > 0.01f)
        {
            FaceDirection(desiredVelocity);
            return;
        }

        FacePosition(pursuitPosition);
    }

    private void UpdateChaseDestination(
        Vector3 destination)
    {
        if (Time.time < _nextRepathTime)
            return;

        _nextRepathTime = Time.time + repathInterval;
        _agent.SetDestination(destination);
    }

    private void FaceTarget()
    {
        FacePosition(target.position);
    }

    private void FacePosition(Vector3 position)
    {
        Vector3 direction = position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion desiredRotation =
            Quaternion.LookRotation(
                direction,
                Vector3.up
            );

        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                desiredRotation,
                attackTurnSpeed * Time.deltaTime
            );
    }

    private void FaceDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion desiredRotation =
            Quaternion.LookRotation(
                direction.normalized,
                Vector3.up
            );

        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                desiredRotation,
                attackTurnSpeed *
                Time.deltaTime
            );
    }

    private bool HasReachedDestination()
    {
        if (_agent.pathPending)
            return false;

        if (float.IsInfinity(_agent.remainingDistance))
            return false;

        return _agent.remainingDistance <= _agent.stoppingDistance + destinationTolerance;
    }

    private bool IsWithinLeash(Vector3 position)
    {
        return HorizontalDistance(_homePosition, position) <= leashDistance;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;

        return Vector3.Distance(a, b);
    }

    private void StopAgent()
    {
        if (!_agent.isOnNavMesh)
            return;

        _agent.isStopped = true;

        if (_agent.hasPath)
            _agent.ResetPath();
    }

    private void HandleDeath()
    {
        StopAgent();
        enabled = false;
    }

    private void HandleDamaged(GameObject source, float damage, DamageType damageType)
    {
        if (source == null || target == null || _health.IsDead)
            return;

        if (source.transform.root != target.root)
            return;

        _perception.RegisterTargetStimulus(source.transform.position);

        if (_state != EnemyState.Block)
            ChangeState(EnemyState.Chase);
    }

    private void HandleStaggerStarted(Vector3 sourcePosition, float duration)
    {
        Vector3 direction = transform.position - sourcePosition;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            direction = -transform.forward;

        _staggerRecoilDirection = direction.normalized;

        _staggerRecoilTimeRemaining = staggerRecoilDuration;
    }

    private void UpdateStaggerRecoil()
    {
        if (_staggerRecoilTimeRemaining <= 0f)
            return;

        if (!_agent.isOnNavMesh)
            return;

        float stepTime = Mathf.Min(Time.deltaTime, _staggerRecoilTimeRemaining);

        float recoilSpeed = staggerRecoilDistance / staggerRecoilDuration;

        Vector3 displacement = _staggerRecoilDirection * recoilSpeed * stepTime;
        _agent.Move(displacement);

        _staggerRecoilTimeRemaining -= stepTime;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center =
            Application.isPlaying
                ? _homePosition
                : transform.position;

        Gizmos.DrawWireSphere(
            center,
            leashDistance
        );
    }
}