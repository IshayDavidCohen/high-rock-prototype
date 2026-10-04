using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(EnemyPerception))]
public sealed class EnemyPerceptionDebug : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField]
    private bool showSightRadius = true;

    [SerializeField, Min(8)]
    private int circleSegments = 64;

    [SerializeField, Min(0.001f)]
    private float lineWidth = 0.05f;

    [SerializeField]
    private float heightOffset = 0.05f;

    private EnemyPerception _perception;
    private LineRenderer _lineRenderer;

    private float _lastRadius = -1f;

    private void Awake()
    {
        _perception =
            GetComponent<EnemyPerception>();

        _lineRenderer =
            GetComponent<LineRenderer>();

        ConfigureLineRenderer();
    }

    private void Update()
    {
        _lineRenderer.enabled =
            showSightRadius;

        if (!showSightRadius)
            return;

        float radius =
            _perception.ActiveSightRange;

        if (!Mathf.Approximately(
                radius,
                _lastRadius))
        {
            DrawCircle(radius);
            _lastRadius = radius;
        }
    }

    private void ConfigureLineRenderer()
    {
        _lineRenderer.loop = true;
        _lineRenderer.useWorldSpace = false;

        _lineRenderer.widthMultiplier =
            lineWidth;

        _lineRenderer.positionCount =
            circleSegments;
    }

    private void DrawCircle(float radius)
    {
        _lineRenderer.positionCount =
            circleSegments;

        for (int i = 0;
             i < circleSegments;
             i++)
        {
            float angle =
                i / (float)circleSegments *
                Mathf.PI * 2f;

            Vector3 point = new Vector3(
                Mathf.Cos(angle) * radius,
                heightOffset,
                Mathf.Sin(angle) * radius
            );

            _lineRenderer.SetPosition(
                i,
                point
            );
        }
    }
}