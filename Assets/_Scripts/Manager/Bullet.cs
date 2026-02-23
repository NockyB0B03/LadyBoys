using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    [Tooltip("Secondi prima che il proiettile si disattivi automaticamente se non colpisce nulla")]
    [SerializeField] private float lifetime = 5f;

    private BulletData _data;
    private Rigidbody _rb;
    private Coroutine _lifetimeCoroutine;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Inizializza il proiettile. Va chiamato subito dopo averlo prelevato dal pool.
    /// </summary>
    public void Initialize(BulletData data, Vector3 direction)
    {
        _data = data;

        // Reset fisico completo
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.useGravity = false;

        // Velocità iniziale
        _rb.velocity = direction.normalized * _data.speed;

        // Avvio lifetime timer
        if (_lifetimeCoroutine != null)
            StopCoroutine(_lifetimeCoroutine);
        _lifetimeCoroutine = StartCoroutine(LifetimeCoroutine());
    }

    private void FixedUpdate()
    {
        if (_data == null) return;

        // Gravità personalizzata
        _rb.AddForce(Physics.gravity * _data.gravityScale, ForceMode.Acceleration);

        // Ruoto il proiettile nella direzione del volo
        if (_rb.velocity.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(_rb.velocity.normalized);
    }

    // Il collider del prefab NON deve essere Trigger:
    // così colpisce qualsiasi superficie fisica.
    // Il layer mask nel BulletData esclude i layer che non devono ricevere danno.
    private void OnCollisionEnter(Collision collision)
    {
        if (_data == null) return;

        // Ignoro se il layer non è nella hitLayers mask
        if ((_data.hitLayers.value & (1 << collision.gameObject.layer)) == 0) return;

        // Infliggo danno se il bersaglio ha Health
        Health health = collision.gameObject.GetComponent<Health>();
        if (health != null)
            health.TakeDamage(_data.damage);

        Deactivate();
    }

    private IEnumerator LifetimeCoroutine()
    {
        yield return new WaitForSeconds(lifetime);
        Deactivate();
    }

    private void Deactivate()
    {
        if (_lifetimeCoroutine != null)
        {
            StopCoroutine(_lifetimeCoroutine);
            _lifetimeCoroutine = null;
        }

        // Reset fisico
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _data = null;

        gameObject.SetActive(false);
    }
}