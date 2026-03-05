using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class GamePause : MonoBehaviour
{
    private Player_Input _playerInput; // Assicurati che il nome corrisponda al tuo asset InputActions

    [Header("UI Elements")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;

    private bool isPaused = false;

    private void Awake()
    {
        // NON usiamo Singleton né DontDestroyOnLoad qui.
        // Ogni Canvas Prefab avrà la sua istanza locale.

        _playerInput = new Player_Input();
        _playerInput.Player.Pause.performed += ctx => TogglePause();

        // Se il bottone non è assegnato, prova a trovarlo nei figli
        if (resumeButton == null && pausePanel != null)
            resumeButton = pausePanel.GetComponentInChildren<Button>();

        if (resumeButton != null)
            resumeButton.onClick.AddListener(TogglePause);

        SetPaused(false);
    }

    private void OnEnable() => _playerInput?.Enable();
    private void OnDisable() => _playerInput?.Disable();

    public void TogglePause()
    {
        isPaused = !isPaused;
        SetPaused(isPaused);
    }

    private void SetPaused(bool value)
    {
        isPaused = value;
        if (pausePanel != null) pausePanel.SetActive(isPaused);

        // Ferma o riattiva il tempo
        Time.timeScale = isPaused ? 0f : 1f;

        // Gestione mouse
        Cursor.visible = isPaused;
        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void OnDestroy()
    {
        if (_playerInput != null)
        {
            _playerInput.Player.Pause.performed -= ctx => TogglePause();
            _playerInput.Dispose();
        }

        // Sicurezza: se il canvas viene distrutto, il gioco non deve restare freezato
        Time.timeScale = 1f;
    }
}