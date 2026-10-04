using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputReader))]
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
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 input = _input.Move;

        Vector3 moveDirection = new Vector3(input.x, 0f, input.y);

        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

        UpdateSprintLock();

        float currentMoveSpeed = ResolveMoveSpeed(moveDirection);

        UpdateVerticalVelocity();

        Vector3 velocity = moveDirection * currentMoveSpeed + Vector3.up * _verticalVelocity;

        _controller.Move(velocity * Time.deltaTime);
    }

    private float ResolveMoveSpeed(Vector3 moveDirection)
    {
        IsSprinting = false;

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
}
