using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Transform target;

    [SerializeField]
    private PlayerInputReader input;

    [Header("Follow")]
    [SerializeField]
    private Vector3 offset = new Vector3(0f, 16f, -13.5f);

    [SerializeField, Min(0f)]
    private float followSmoothTime = 0.12f;

    [Header("Look Ahead")]
    [SerializeField, Min(0f)]
    private float maxLookAheadDistance = 4f;

    [SerializeField, Range(0f, 0.5f)]
    private float cursorDeadZone = 0.08f;

    [SerializeField, Min(0f)]
    private float lookAheadSmoothTime = 0.15f;

    [SerializeField, Min(0f)]
    private float returnSmoothTime = 0.2f;

    private Vector3 _followPosition;
    private Vector3 _followVelocity;

    private Vector3 _lookAheadOffset;
    private Vector3 _lookAheadVelocity;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (target == null)
            return;

        _followPosition = target.position;
        transform.position = _followPosition + offset;
    }

    private void LateUpdate()
    {
        if (target == null || input == null)
            return;

        UpdateFollowPosition();
        UpdateLookAhead();

        transform.position = _followPosition + offset + _lookAheadOffset;

    }

    private void UpdateFollowPosition()
    {
        _followPosition = Vector3.SmoothDamp(
            _followPosition,
            target.position,
            ref _followVelocity,
            followSmoothTime
        );
    }

    private void UpdateLookAhead()
    {
        bool lookAheadActive = input.IsLookAheadHold;

        Vector3 targetOffset = lookAheadActive ? CalculateLookAheadOffset() : Vector3.zero;
        float smoothTime = lookAheadActive ? lookAheadSmoothTime : returnSmoothTime;

        _lookAheadOffset = Vector3.SmoothDamp(
            _lookAheadOffset,
            targetOffset,
            ref _lookAheadVelocity,
            smoothTime
        );
    }

    private Vector3 CalculateLookAheadOffset()
    {
        Vector2 pointerPosition = input.PointerPosition;

        Vector2 screenCenter = new Vector2(
            Screen.width * 0.5f,
            Screen.height * 0.5f
        );

        if (screenCenter.x <= 0f || screenCenter.y <= 0f)
            return Vector3.zero;

        Vector2 cursorFormCenter = pointerPosition - screenCenter;
        Vector2 normalizedCursor = new Vector2(
            cursorFormCenter.x / screenCenter.x,
            cursorFormCenter.y / screenCenter.y
        );

        normalizedCursor = Vector2.ClampMagnitude(normalizedCursor, 1f);

        float cursorDistance = normalizedCursor.magnitude;

        if (cursorDistance <= cursorDeadZone)
            return Vector3.zero;

        float strength = Mathf.InverseLerp(cursorDeadZone, 1f, cursorDistance);
        strength = Mathf.SmoothStep(0f, 1f, strength);


        Vector2 direction = normalizedCursor.normalized;

        Vector3 cameraRight = transform.right;
        cameraRight.y = 0f;
        cameraRight.Normalize();

        Vector3 cameraForward = transform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        Vector3 worldDirection = cameraRight * direction.x + cameraForward * direction.y;

        return worldDirection * (maxLookAheadDistance * strength);
    }
}
