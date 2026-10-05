using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInputReader))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(BlockController))]
public sealed class PlayerBlockInput : MonoBehaviour
{
    private PlayerInputReader _input;
    private PlayerMovement _movement;
    private BlockController _block;

    private bool _requiresBlockRelease;

    private void Awake()
    {
        _input = GetComponent<PlayerInputReader>();

        _movement = GetComponent<PlayerMovement>();

        _block = GetComponent<BlockController>();
    }

    private void OnEnable()
    {
        _block.GuardBroken += HandleGuardBroken;
    }

    private void OnDisable()
    {
        _block.GuardBroken -= HandleGuardBroken;
    }

    private void Update()
    {
        bool blockHeld = _input.IsBlockHeld;

        if (!blockHeld)
        {
            _requiresBlockRelease = false;
            _block.SetBlocking(false);
            return;
        }

        if (_requiresBlockRelease || _movement.IsDodging)
        {
            _block.SetBlocking(false);
            return;
        }

        _block.SetBlocking(true);
    }

    private void HandleGuardBroken()
    {
        _requiresBlockRelease = true;
    }
}