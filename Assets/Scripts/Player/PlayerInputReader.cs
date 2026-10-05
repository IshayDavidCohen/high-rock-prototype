using UnityEngine;

public sealed class PlayerInputReader : MonoBehaviour
{
    private PlayerControls _controls;

    public Vector2 Move => _controls.Player.Move.ReadValue<Vector2>();
    public Vector2 PointerPosition => _controls.Player.PointerPosition.ReadValue<Vector2>();
    public bool IsLookAheadHold => _controls.Player.LookAhead.IsPressed();

    public bool AttackPressedThisFrame => _controls.Player.Attack.WasPressedThisFrame();

    public bool IsSprintHeld => _controls.Player.Sprint.IsPressed();

    public bool DodgePressedThisFrame => _controls.Player.Dodge.WasPressedThisFrame();

    public bool IsBlockHeld => _controls.Player.Block.IsPressed();

    private void Awake()
    {
        _controls = new PlayerControls();
    }

    private void OnEnable()
    {
        _controls.Player.Enable();
    }

    private void OnDisable()
    {
        _controls.Player.Disable();
    }

    private void OnDestroy()
    {
        _controls.Dispose();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
