using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputReader))]
[RequireComponent(typeof(Stamina))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(BlockController))]
[RequireComponent(typeof(StaggerController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)]
    private float moveSpeed = 6f;

    [Header("Sprint")]
    [SerializeField, Min(1f)]
    private float sprintSpeedMultiplier = 1.6f;

    [SerializeField, Min(0f)]
    private float sprintStaminaCostPerSecond = 8f;

    [SerializeField, Range(0f, 1f)]
    private float sprintRecoveryFraction = 0.2f;

    [Header("Dodge")]
    [SerializeField, Min(0f)]
    private float dodgeDistance = 3f;

    [SerializeField, Min(0.01f)]
    private float dodgeDuration = 0.2f;

    [SerializeField, Min(0f)]
    private float dodgeStaminaCost = 20f;

    [SerializeField, Min(0f)]
    private float dodgeCooldown = 0.45f;

    [SerializeField, Min(0f)]
    private float dodgeInvulnerabilityDuration = 0.2f;

    [Header("Block")]
    private BlockController _block;
    private StaggerController _stagger;

    private Health _health;
    private bool _isDodging;
    private Vector3 _dodgeDirection;
    private float _dodgeTimeRemaining;
    private float _nextDodgeTime;

    public bool IsDodging => _isDodging;

    [Header("Stagger Recoil")]
    [SerializeField, Min(0f)]
    private float staggerRecoilDistance = 1.5f; // For player we will set it uniquely to 1.5f
    
    [SerializeField, Min(0.01f)]
    private float staggerRecoilDuration = 0.18f;

    private Vector3 _staggerRecoilDirection;
    private float _staggerRecoilTimeRemaining;


    [SerializeField]
    private float groundedVerticalVelocity = -2f;

    private CharacterController _controller;
    private PlayerInputReader _input;
    private Stamina _stamina;

    private float _verticalVelocity;
    private bool _sprintLocked;

    public bool IsSprinting { get; private set; }

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<PlayerInputReader>();
        _stamina = GetComponent<Stamina>();
        _health = GetComponent<Health>();
        _block = GetComponent<BlockController>();
        _stagger = GetComponent<StaggerController>();
    }

    private void OnEnable()
    {
        _stagger.StaggerStarted += HandleStaggerStarted;
    }

    private void OnDisable()
    {
        _stagger.StaggerStarted -= HandleStaggerStarted;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 input = _input.Move;

        Vector3 moveDirection = new Vector3(input.x, 0f, input.y);

        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

        UpdateVerticalVelocity();

        if (_stagger.IsStaggered)
        {
            _isDodging = false;
            IsSprinting = false;

            UpdateStaggerRecoil();
            return;
        }


        if (_isDodging)
        {
            UpdateDodge();
            return;
        }

        if (_input.DodgePressedThisFrame && TryStartDodge(moveDirection))
        {
            UpdateDodge();
            return;
        }

        UpdateSprintLock();

        float currentMoveSpeed = ResolveMoveSpeed(moveDirection);

        Vector3 velocity = moveDirection * currentMoveSpeed + Vector3.up * _verticalVelocity;

        _controller.Move(velocity * Time.deltaTime);
    }

    private float ResolveMoveSpeed(Vector3 moveDirection)
    {
        IsSprinting = false;

        if (_block.IsBlocking)
        {
            return moveSpeed * _block.MovementSpeedMultiplier;
        }

        bool isMoving = moveDirection.sqrMagnitude > 0.0001f;
        if (!isMoving || !_input.IsSprintHeld || _sprintLocked)
            return moveSpeed;

        float staminaCost = sprintStaminaCostPerSecond * Time.deltaTime;
        bool hasEnoughStamina = _stamina.TrySpend(staminaCost);

        if (!hasEnoughStamina)
        {
            _sprintLocked = true;
            return moveSpeed;
        }

        IsSprinting = true;
        return moveSpeed * sprintSpeedMultiplier;

    }

    private void UpdateSprintLock()
    {
        if (!_sprintLocked)
            return;

        float requiredStamina = _stamina.MaxStamina * sprintRecoveryFraction;

        if (_stamina.CurrentStamina >= requiredStamina)
            _sprintLocked = false;
    }

    private void UpdateVerticalVelocity()
    {
        if (_controller.isGrounded && _verticalVelocity < 0f)
        {
            _verticalVelocity = groundedVerticalVelocity;
            return;
        }

        _verticalVelocity += Physics.gravity.y * Time.deltaTime;
    }

    private bool TryStartDodge(Vector3 moveDirection)
    {
        if (Time.time < _nextDodgeTime)
            return false;

        if (!_stamina.TrySpend(dodgeStaminaCost))
            return false;


        Debug.Log("Dodge accepted");

        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            _dodgeDirection = moveDirection.normalized;
        }
        else
        {
            _dodgeDirection = transform.forward;

            _dodgeDirection.y = 0f;
            _dodgeDirection.Normalize();
        }

        _isDodging = true;
        IsSprinting = false;

        _dodgeTimeRemaining = dodgeDuration;

        _nextDodgeTime = Time.time + dodgeCooldown;

        _health.GrantInvulnerability(dodgeInvulnerabilityDuration);

        return true;
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
        Vector3 displacement =
            Vector3.up *
            _verticalVelocity *
            Time.deltaTime;

        if (_staggerRecoilTimeRemaining > 0f)
        {
            float stepTime =
                Mathf.Min(
                    Time.deltaTime,
                    _staggerRecoilTimeRemaining
                );

            float recoilSpeed =
                staggerRecoilDistance /
                staggerRecoilDuration;

            displacement +=
                _staggerRecoilDirection *
                recoilSpeed *
                stepTime;

            _staggerRecoilTimeRemaining -=
                stepTime;
        }

        _controller.Move(displacement);
    }

    private void UpdateDodge()
    {
        float dodgeSpeed = dodgeDistance / dodgeDuration;

        float dodgeStepTime = Mathf.Min(Time.deltaTime, _dodgeTimeRemaining);

        Vector3 displacement = _dodgeDirection * dodgeSpeed * dodgeStepTime + Vector3.up * _verticalVelocity * Time.deltaTime;

        _controller.Move(displacement);

        _dodgeTimeRemaining -= Time.deltaTime;

        if (_dodgeTimeRemaining <= 0f)
            _isDodging = false;
    }
}
