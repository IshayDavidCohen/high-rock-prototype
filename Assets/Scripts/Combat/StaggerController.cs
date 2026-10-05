using System;
using UnityEngine;


[DisallowMultipleComponent]
public sealed class StaggerController : MonoBehaviour
{
    public bool IsStaggered => Time.time < _staggerUntil;

    public Vector3 LastSourcePosition { get; private set; }

    public event Action<Vector3, float> StaggerStarted;
    public event Action StaggerEnded;

    private float _staggerUntil;
    private bool _wasStaggered;

    public void ApplyStagger(float duration, Vector3 sourcePosition)
    {
        if (duration <= 0f)
            return;

        LastSourcePosition = sourcePosition;

        float newEndTime = Time.time + duration;

        _staggerUntil = Mathf.Max(_staggerUntil, newEndTime);
        if (_wasStaggered)
            return;

        _wasStaggered = true;

        StaggerStarted?.Invoke(sourcePosition, duration);
    }
    
    void Update()
    {
        if (!_wasStaggered)
            return;

        if (IsStaggered)
            return;

        _wasStaggered = false;

        StaggerEnded?.Invoke();
    }
}
