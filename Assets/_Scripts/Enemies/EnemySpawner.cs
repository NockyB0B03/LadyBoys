using UnityEngine;

/// <summary>
/// Classe concreta di Spawner per i nemici.
/// Estende Spawner aggiungendo logica di inizializzazione specifica per i nemici.
/// Lo spawn parte automaticamente quando il player entra nel BoxCollider (gestito da Spawner).
/// </summary>
public class EnemySpawner : Spawner
{
    /// <summary>
    /// Chiamato ogni volta che un nemico viene spawnato.
    /// Aggiungi qui la logica di inizializzazione (es. assegnare il target al nemico).
    /// </summary>
    protected override void OnObjectSpawned(GameObject obj)
    {
        // Esempio:
        // EnemyAI ai = obj.GetComponent<EnemyAI>();
        // if (ai != null) ai.SetTarget(player);

        Debug.Log($"[EnemySpawner] Nemico spawnato: {obj.name} in {obj.transform.position}");
    }

    protected override void OnWaveCompleted(int waveIndex)
    {
        Debug.Log($"[EnemySpawner] Wave {waveIndex} completata.");
    }

    protected override void OnAllWavesCompleted()
    {
        Debug.Log("[EnemySpawner] Tutte le wave completate. Spawn terminato.");
    }
}