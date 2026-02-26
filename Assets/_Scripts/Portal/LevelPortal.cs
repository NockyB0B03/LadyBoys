using UnityEngine;

public class LevelPortal : MonoBehaviour
{
    [Header("Impostazioni Portale")]
    [SerializeField] private float delayBeforeLoad = 0.5f;

    [Header("Layer")]
    [SerializeField] private LayerMask playerLayer;

    private bool _activated = false; // evita doppi trigger

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[LevelPortal] Trigger colpito da: {other.gameObject.name} | Layer: {other.gameObject.layer} | Activated: {_activated}");

        if (_activated) return;

        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            _activated = true;
            Debug.Log("[LevelPortal] Player riconosciuto! Caricamento...");
            Invoke(nameof(LoadNext), delayBeforeLoad);
        }
    }

    private void LoadNext()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.LoadNextLevel();
        else
            Debug.LogWarning("[LevelPortal] GameManager non trovato!");
    }
}