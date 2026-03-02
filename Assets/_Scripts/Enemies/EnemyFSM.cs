using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyFSM : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // STATI
    // -----------------------------------------------------------------------
    private enum State { Patrol, Follow, Hit, FallBack }

    // -----------------------------------------------------------------------
    // RIFERIMENTI
    // -----------------------------------------------------------------------
    [Header("References")]
    [SerializeField] private Transform player;

    // -----------------------------------------------------------------------
    // PATROL
    // -----------------------------------------------------------------------
    [Header("Patrol Settings")]
    [SerializeField] private float patrolRotationSpeed = 40f;   // gradi/secondo
    [SerializeField] private float patrolRotationAngle = 60f;   // ampiezza swing (±60°)

    // -----------------------------------------------------------------------
    // FOLLOW
    // -----------------------------------------------------------------------
    [Header("Follow Settings")]
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private float followSpeed = 4f;
    [SerializeField] private float rotationSpeed = 8f;          // velocità rotazione verso il player

    // -----------------------------------------------------------------------
    // HIT / DANNO
    // -----------------------------------------------------------------------
    [Header("Hit Settings")]
    [SerializeField] private float damageAmount = 10f;

    // -----------------------------------------------------------------------
    // FALLBACK
    // -----------------------------------------------------------------------
    [Header("FallBack Settings")]
    [SerializeField] private float fallBackDuration = 2f;

    // -----------------------------------------------------------------------
    // PRIVATI
    // -----------------------------------------------------------------------
    private State _currentState = State.Patrol;
    private Rigidbody _rb;

    // Patrol
    private float _patrolBaseYaw;           // rotazione Y di partenza
    private float _patrolTimer;

    // FallBack
    private bool _fallBackRunning;

    // -----------------------------------------------------------------------
    // UNITY
    // -----------------------------------------------------------------------
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.freezeRotation = true;          // gestiamo la rotazione a mano
        _patrolBaseYaw = transform.eulerAngles.y;
    }

    private void Update()
    {
        switch (_currentState)
        {
            case State.Patrol: UpdatePatrol(); break;
            case State.Follow: UpdateFollow(); break;
            case State.Hit: break; // gestito da OnCollisionEnter
            case State.FallBack: break; // gestito dalla coroutine
        }
    }

    // -----------------------------------------------------------------------
    // PATROL
    // -----------------------------------------------------------------------
    private void UpdatePatrol()
    {
        // Oscillazione sinistra-destra usando un seno nel tempo
        _patrolTimer += Time.deltaTime * patrolRotationSpeed;
        float yaw = _patrolBaseYaw + Mathf.Sin(_patrolTimer * Mathf.Deg2Rad) * patrolRotationAngle;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        // Transizione → Follow
        if (player != null && DistanceToPlayer() <= detectionRadius)
            ChangeState(State.Follow);
    }

    // -----------------------------------------------------------------------
    // FOLLOW
    // -----------------------------------------------------------------------
    private void UpdateFollow()
    {
        if (player == null) return;

        // Transizione → Patrol (player uscito dal raggio)
        if (DistanceToPlayer() > detectionRadius)
        {
            _rb.velocity = Vector3.zero;
            ChangeState(State.Patrol);
            return;
        }

        // Rotazione verso il player
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        dirToPlayer.y = 0f;

        if (dirToPlayer != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(dirToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        // Movimento in avanti (usa il forward del nemico per non scivolare lateralmente)
        _rb.velocity = transform.forward * followSpeed;
    }

    // -----------------------------------------------------------------------
    // HIT — rilevato via collisione fisica
    // -----------------------------------------------------------------------
    private void OnCollisionEnter(Collision collision)
    {
        // Reagisce solo se siamo in Follow e colpiamo il Player
        if (_currentState != State.Follow) return;
        if (!collision.gameObject.CompareTag("Player")) return;

        // Applica danno al componente Health del Player
        Health playerHealth = collision.gameObject.GetComponent<Health>();
        if (playerHealth != null)
            playerHealth.TakeDamage(damageAmount);

        // Ferma il movimento e passa a FallBack
        _rb.velocity = Vector3.zero;
        ChangeState(State.Hit);
        StartCoroutine(FallBackRoutine());
    }

    // -----------------------------------------------------------------------
    // FALLBACK — coroutine
    // -----------------------------------------------------------------------
    private IEnumerator FallBackRoutine()
    {
        ChangeState(State.FallBack);
        yield return new WaitForSeconds(fallBackDuration);
        ChangeState(State.Follow);
    }

    // -----------------------------------------------------------------------
    // UTILITY
    // -----------------------------------------------------------------------
    private void ChangeState(State newState)
    {
        _currentState = newState;

        // Quando si torna in Patrol aggiorna la base yaw così non scatta
        if (newState == State.Patrol)
        {
            _patrolBaseYaw = transform.eulerAngles.y;
            _patrolTimer = 0f;
        }
    }

    private float DistanceToPlayer()
    {
        return Vector3.Distance(transform.position, player.position);
    }

    // -----------------------------------------------------------------------
    // GIZMO — visualizza il raggio di detection nell'editor
    // -----------------------------------------------------------------------
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}