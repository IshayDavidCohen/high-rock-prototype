using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(EnemyBrain))]
[RequireComponent(typeof(EnemyCombat))]
[RequireComponent(typeof(EnemyPerception))]
[RequireComponent(typeof(NavMeshAgent))]
public sealed class EnemyDeathHandler : MonoBehaviour
{
    private Health _health;
    private EnemyBrain _brain;
    private EnemyCombat _combat;
    private EnemyPerception _perception;
    private NavMeshAgent _agent;

    private Collider[] _colliders;

    private void Awake()
    {
        _health = GetComponent<Health>();
        _brain = GetComponent<EnemyBrain>();
        _combat = GetComponent<EnemyCombat>();
        _perception = GetComponent<EnemyPerception>();
        _agent = GetComponent<NavMeshAgent>();

        _colliders = GetComponentsInChildren<Collider>(true);
    }

    private void OnEnable()
    {
        _health.Died += HandleDeath;
    }

    private void OnDisable()
    {
        _health.Died -= HandleDeath;
    }

    private void HandleDeath()
    {
        StopNavigation();

        _brain.enabled = false;
        _combat.enabled = false;
        _perception.enabled = false;
        _agent.enabled = false;

        DisableColliders();
    }

    private void StopNavigation()
    {
        if (!_agent.isOnNavMesh)
            return;

        _agent.isStopped = true;

        if (_agent.hasPath)
            _agent.ResetPath();
    }

    private void DisableColliders()
    {
        foreach (Collider collider in _colliders)
        {
            collider.enabled = false;
        }
    }
}