using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Health;

public class GamePause : MonoBehaviour
{
    private Player_Input _playerInput;

    [Header("UI Elements")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;

    private bool isPaused = false;

    private void Awake()
    {
        _playerInput = new Player_Input();
        _playerInput.Player.Pause.performed += ctx => TogglePause();

        // Trova il bottone automaticamente nel pausePanel
        if (resumeButton == null && pausePanel != null)
            resumeButton = pausePanel.GetComponentInChildren<Button>();

        if (resumeButton != null)
            resumeButton.onClick.AddListener(TogglePause);

        SetPaused(false);
    }

    private void OnEnable()
    {
        _playerInput.Enable();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        _playerInput.Disable();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetPaused(false);
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        SetPaused(isPaused);
    }

    private void SetPaused(bool value)
    {
        isPaused = value;

        if (pausePanel != null)
            pausePanel.SetActive(isPaused);

        Time.timeScale = isPaused ? 0f : 1f;

        Cursor.visible = isPaused;
        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void OnDestroy()
    {
        _playerInput.Dispose();
    }
}
