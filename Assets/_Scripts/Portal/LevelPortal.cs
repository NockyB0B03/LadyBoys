using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelPortal : MonoBehaviour
{
    [Header("Impostazioni Portale")]
    [SerializeField] private string nextSceneName = "Level1";
    [SerializeField] private float delayBeforeLoad = 0.5f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player ha raggiunto il portale! Caricamento del prossimo livello...");
            Invoke("LoadNextLevel", delayBeforeLoad);
        }
    }

    private void LoadNextLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(nextSceneName);
    }
}