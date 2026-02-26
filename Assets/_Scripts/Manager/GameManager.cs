using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// GameManager Singleton persistente tra le scene.
/// Gestisce la progressione dei livelli e le vite del player.
/// </summary>
public class GameManager : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // SINGLETON
    // -----------------------------------------------------------------------
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        // Se esiste già un'istanza, distruggi questo duplicato
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // persiste tra le scene
    }

    // -----------------------------------------------------------------------
    // IMPOSTAZIONI
    // -----------------------------------------------------------------------
    [Header("Vite")]
    [SerializeField] private int startingLives = 3;

    [Header("Scene")]
    [SerializeField] private string gameOverSceneName = "GameOver";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    // -----------------------------------------------------------------------
    // STATO
    // -----------------------------------------------------------------------
    public int CurrentLives { get; private set; }
    public int CurrentLevelIndex => SceneManager.GetActiveScene().buildIndex;

    // -----------------------------------------------------------------------
    // EVENTI — altri script possono iscriversi per reagire ai cambiamenti
    // -----------------------------------------------------------------------
    public event System.Action<int> OnLivesChanged;  // param: vite rimanenti
    public event System.Action OnGameOver;

    // -----------------------------------------------------------------------
    // START
    // -----------------------------------------------------------------------
    private void Start()
    {
        CurrentLives = startingLives;
        Debug.Log($"[GameManager] Inizializzato. Vite: {CurrentLives}, Livello: {CurrentLevelIndex}");
    }

    // -----------------------------------------------------------------------
    // PROGRESSIONE LIVELLI
    // -----------------------------------------------------------------------

    /// <summary>Chiamato dal LevelPortal quando il player raggiunge il portale.</summary>
    public void LoadNextLevel()
    {
        int nextIndex = CurrentLevelIndex + 1;

        if (nextIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.Log("[GameManager] Ultimo livello completato! Fine del gioco.");
            LoadMainMenu(); // per ora torna al menu, in futuro puoi fare una scena Credits
            return;
        }

        Debug.Log($"[GameManager] Caricamento livello {nextIndex}...");
        Time.timeScale = 1f; // sicurezza: resetta la pausa
        SceneManager.LoadScene(nextIndex);
    }

    // -----------------------------------------------------------------------
    // GESTIONE VITE
    // -----------------------------------------------------------------------

    /// <summary>Chiamato da Health.cs quando il player muore.</summary>
    public void PlayerDied()
    {
        CurrentLives--;
        Debug.Log($"[GameManager] Player morto. Vite rimanenti: {CurrentLives}");

        OnLivesChanged?.Invoke(CurrentLives); // notifica la UI

        if (CurrentLives <= 0)
        {
            Debug.Log("[GameManager] Game Over!");
            OnGameOver?.Invoke();
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameOverSceneName);
        }
        else
        {
            // Ricarica il livello corrente
            SceneManager.LoadScene(CurrentLevelIndex);
        }
    }

    // -----------------------------------------------------------------------
    // UTILITY
    // -----------------------------------------------------------------------
    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void RestartGame()
    {
        CurrentLives = startingLives;
        Time.timeScale = 1f;
        SceneManager.LoadScene(0); // torna al Level0
    }
}