using UnityEngine;
using UnityEngine.InputSystem;

public class Health : MonoBehaviour
{
    public enum Team { Player, Enemy, Neutral }

    [Header("Team")]
    [SerializeField] private Team team = Team.Neutral;

    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float healAmount = 25f;

    // Proprietà pubblica leggibile dagli altri script (es. UI)
    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public Team CurrentTeam => team;

    // Input (solo per il Player)
    private InputAction _healAction;

    private void Start()
    {
        CurrentHealth = maxHealth;

        if (team == Team.Player)
            SetupHealInput();
    }

    private void SetupHealInput()
    {
        // Cerca il PlayerInput sul GameObject o nei parent
        PlayerInput playerInput = GetComponentInParent<PlayerInput>();

        if (playerInput == null)
        {
            Debug.LogWarning("[Health] Team è Player ma non è stato trovato nessun PlayerInput sul GameObject o nei suoi parent.");
            return;
        }

        var map = playerInput.actions.FindActionMap("Player", throwIfNotFound: true);
        _healAction = map.FindAction("Heal", throwIfNotFound: true);
        _healAction.Enable();
        _healAction.performed += OnHealPerformed;
    }

    private void OnDisable()
    {
        if (_healAction != null)
        {
            _healAction.performed -= OnHealPerformed;
            _healAction.Disable();
        }
    }

    // -----------------------------------------------------------------------
    // HEAL — chiamato dall'input solo se team == Player
    // -----------------------------------------------------------------------
    private void OnHealPerformed(InputAction.CallbackContext context)
    {
        CurrentHealth = Mathf.Min(CurrentHealth + healAmount, maxHealth);
        Debug.Log($"[Health] Curato! Salute attuale: {CurrentHealth}/{maxHealth}");
    }

    // -----------------------------------------------------------------------
    // TAKE DAMAGE — chiamato dagli altri script (proiettili, armi, ecc.)
    // -----------------------------------------------------------------------
    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;

        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0f);
        Debug.Log($"[Health] {gameObject.name} ha ricevuto {amount} danni. Salute attuale: {CurrentHealth}/{maxHealth}");

        if (CurrentHealth <= 0f)
            Die();
    }

    // -----------------------------------------------------------------------
    // DIE
    // -----------------------------------------------------------------------
    private void Die()
    {
        Debug.Log($"[Health] {gameObject.name} è morto.");

        if (team == Team.Player)
        {
            // Notifica il GameManager invece di disabilitare direttamente
            if (GameManager.Instance != null)
                GameManager.Instance.PlayerDied();
            else
                Debug.LogWarning("[Health] GameManager non trovato! Assicurati che esista in scena.");
        }
        else
        {
            // Nemici e Neutral: comportamento invariato
            gameObject.SetActive(false);
        }
    }
}