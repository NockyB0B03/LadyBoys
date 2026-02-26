using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class GamePause : MonoBehaviour
{
    private Player_Input _playerInput; // Classe generata dall'Input System

    [Header("UI Elements")]
    [SerializeField] private GameObject pausePanel; // Il pannello che contiene il menu
    [SerializeField] private Button resumeButton;   // Il tasto "Riprendi"

    private bool isPaused = false;

    private void Awake()
    {
        // Inizializza l'input
        _playerInput = new Player_Input();

        // Collega l'azione di pausa (es. tasto ESC o P)
        _playerInput.Player.Pause.performed += ctx => TogglePause();

        // Collega il bottone UI se presente
        if (resumeButton != null)
            resumeButton.onClick.AddListener(TogglePause);

        // Assicurati che il gioco parta non in pausa
        SetPaused(false);
    }

    private void OnEnable() => _playerInput.Enable();
    private void OnDisable() => _playerInput.Disable();

    public void TogglePause()
    {
        isPaused = !isPaused;
        SetPaused(isPaused);
    }

    private void SetPaused(bool value)
    {
        isPaused = value;

        // Mostra/Nasconde il pannello
        if (pausePanel != null)
            pausePanel.SetActive(isPaused);

        // Blocca/Sblocca il tempo di gioco (0 = fermo, 1 = normale)
        Time.timeScale = isPaused ? 0f : 1f;

        // Gestione del cursore per poter cliccare i tasti del menu
        Cursor.visible = isPaused;
        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void OnDestroy()
    {
        _playerInput.Dispose();
    }
}