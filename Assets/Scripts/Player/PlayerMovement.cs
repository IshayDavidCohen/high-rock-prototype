using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputReader))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)]
    private float moveSpeed = 6f;

    [SerializeField]
    private float groundedVerticalVelocity = -2f;

    private CharacterController _controller;
    private PlayerInputReader _input;

    private float _verticalVelocity;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<PlayerInputReader>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 input = _input.Move;

        Vector3 moveDirection = new Vector3(
            input.x,
            0f,
            input.y
        );

        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

        UpdateVerticalVelocity();

        Vector3 velocity = moveDirection * moveSpeed + Vector3.up * _verticalVelocity;

        _controller.Move(velocity * Time.deltaTime);
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
