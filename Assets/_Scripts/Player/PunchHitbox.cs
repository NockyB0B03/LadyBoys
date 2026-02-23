using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gestisce la hitbox del pugno del player.
/// Aggiungi questo script su un GameObject figlio del player con un Collider impostato come Trigger.
/// La hitbox si attiva solo durante la finestra di danno del pugno.
/// </summary>
public class PunchHitbox : MonoBehaviour
{
    [Header("Punch Settings")]
    [Tooltip("Danno inflitto dal pugno")]
    [SerializeField] private float punchDamage = 15f;

    [Tooltip("Durata della finestra di danno attiva (in secondi)")]
    [SerializeField] private float hitboxActiveDuration = 0.2f;

    [Tooltip("Cooldown tra un pugno e il successivo (in secondi)")]
    [SerializeField] private float punchCooldown = 0.5f;

    [Tooltip("Layer mask dei bersagli colpibili dal pugno")]
    [SerializeField] private LayerMask hitLayers;

    private Collider _hitboxCollider;
    private InputAction _punchAction;
    private bool _isOnCooldown;

    private void Awake()
    {
        _hitboxCollider = GetComponent<Collider>();

        if (_hitboxCollider == null)
        {
            Debug.LogError("[PunchHitbox] Nessun Collider trovato su questo GameObject. Aggiungine uno e impostalo come Trigger.");
            return;
        }

        // La hitbox parte disattivata: si attiva solo durante il pugno
        _hitboxCollider.enabled = false;
    }

    private void Start()
    {
        PlayerInput playerInput = GetComponentInParent<PlayerInput>();

        if (playerInput == null)
        {
            Debug.LogError("[PunchHitbox] Nessun PlayerInput trovato nei parent.");
            return;
        }

        var map = playerInput.actions.FindActionMap("Player", throwIfNotFound: true);
        _punchAction = map.FindAction("Punch", throwIfNotFound: true);
        _punchAction.Enable();
        _punchAction.performed += OnPunchPerformed;
    }

    private void OnDisable()
    {
        if (_punchAction != null)
        {
            _punchAction.performed -= OnPunchPerformed;
            _punchAction.Disable();
        }
    }

    private void OnPunchPerformed(InputAction.CallbackContext context)
    {
        if (_isOnCooldown) return;
        StartCoroutine(PunchCoroutine());
    }

    private IEnumerator PunchCoroutine()
    {
        _isOnCooldown = true;

        // Attiva la hitbox per la finestra di danno
        _hitboxCollider.enabled = true;
        yield return new WaitForSeconds(hitboxActiveDuration);

        // Disattiva la hitbox
        _hitboxCollider.enabled = false;

        // Attende il cooldown prima di permettere un nuovo pugno
        yield return new WaitForSeconds(punchCooldown - hitboxActiveDuration);
        _isOnCooldown = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Controlla il layer mask
        if ((hitLayers.value & (1 << other.gameObject.layer)) == 0) return;

        Health health = other.GetComponent<Health>();
        if (health != null)
            health.TakeDamage(punchDamage);
    }
}