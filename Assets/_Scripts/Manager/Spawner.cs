using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Classe base astratta per il sistema di spawn a ondate.
///
/// SETUP IN UNITY:
/// 1. Crea un empty GameObject e aggiungici questo script (o una sua classe figlia)
/// 2. Aggiungici un BoxCollider con Is Trigger = true: definisce l'area di spawn
/// 3. Assegna le WaveData nell'Inspector
/// 4. Il layer "Muri" deve essere configurato nel campo wallLayerMask
/// 5. Il player deve avere il tag "Player"
///
/// LOGICA:
/// - Il player entra nel BoxCollider → parte la prima wave
/// - Ogni wave spawna oggetti uno alla volta a intervalli random, su superfici trovate via SphereCast
/// - waveCompleted diventa true quando tutti gli oggetti della wave sono stati disabilitati
/// - Se la wave non è completed e il player esce, tutti gli oggetti vengono disabilitati e la wave è marcata completed
/// - Quando tutte le wave sono completed lo spawn si ferma
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public abstract class Spawner : MonoBehaviour
{
    [Header("Waves")]
    [Tooltip("Lista ordinata delle wave da eseguire")]
    [SerializeField] private WaveData[] waves;

    [Header("Pool")]
    [Tooltip("Dimensione del pool condiviso tra tutte le wave")]
    [SerializeField] private int poolSize = 20;

    [Header("Spawn Detection")]
    [Tooltip("LayerMask dei muri su cui avviene lo spawn")]
    [SerializeField] private LayerMask wallLayerMask;

    [Tooltip("Numero massimo di tentativi per trovare un punto di spawn valido per ogni oggetto")]
    [SerializeField] private int maxSpawnAttempts = 30;

    [Header("Player")]
    [SerializeField] private string playerTag = "Player";

    // Riferimenti interni
    private BoxCollider _box;
    private bool _playerInside;
    private bool _allWavesDone;

    // Indice della wave corrente
    private int _currentWaveIndex = 0;

    // Stato wave corrente
    private bool _waveCompleted = false;
    private bool _waveRunning = false;

    // Oggetti attualmente attivi spawnati dalla wave corrente
    private readonly List<GameObject> _activeObjects = new();

    // Pool condiviso (FIFO circolare)
    private Queue<GameObject> _pool;
    private GameObject _currentPrefab; // prefab corrente nel pool (si reinizializza se cambia)

    // RNG per posizioni casuali
    private System.Random _rng;

    // Direzioni dei 6 lati del cubo (spazio locale)
    private static readonly Vector3[] _sixDirections = new Vector3[]
    {
        Vector3.up, Vector3.down,
        Vector3.left, Vector3.right,
        Vector3.forward, Vector3.back
    };

    // -----------------------------------------------------------------------
    // AWAKE
    // -----------------------------------------------------------------------
    protected virtual void Awake()
    {
        _box = GetComponent<BoxCollider>();
        _box.isTrigger = true;
    }

    // -----------------------------------------------------------------------
    // TRIGGER — entrata e uscita del player
    // -----------------------------------------------------------------------
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (_allWavesDone) return;

        _playerInside = true;

        // Se non c'è una wave in corso, parte la prossima
        if (!_waveRunning)
            StartCoroutine(RunNextWave());
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        _playerInside = false;

        // Se la wave è ancora in corso forzo il completamento
        if (_waveRunning && !_waveCompleted)
            StartCoroutine(ForceCompleteWave());
    }

    // -----------------------------------------------------------------------
    // WAVE RUNNER
    // -----------------------------------------------------------------------
    private IEnumerator RunNextWave()
    {
        if (_currentWaveIndex >= waves.Length)
        {
            _allWavesDone = true;
            OnAllWavesCompleted();
            yield break;
        }

        WaveData wave = waves[_currentWaveIndex];
        if (wave == null || wave.prefab == null)
        {
            Debug.LogWarning($"[Spawner] Wave {_currentWaveIndex} ha WaveData o prefab nullo, la salto.");
            _currentWaveIndex++;
            yield break;
        }

        _waveCompleted = false;
        _waveRunning = true;
        _activeObjects.Clear();
        _rng = new System.Random(wave.seed);

        EnsurePool(wave.prefab, wave.spawnCount);

        List<Vector3> occupiedPositions = new();

        // Spawna gli oggetti uno alla volta a intervalli random
        for (int i = 0; i < wave.spawnCount; i++)
        {
            if (!_playerInside) break; // se il player è uscito durante la wave, interrompo

            float delay = (float)(_rng.NextDouble() *
                          (wave.maxSpawnInterval - wave.minSpawnInterval) +
                          wave.minSpawnInterval);

            yield return new WaitForSeconds(delay);

            if (!_playerInside) break;

            Vector3? spawnPos = FindSpawnPosition(wave, occupiedPositions);
            if (spawnPos == null) continue;

            occupiedPositions.Add(spawnPos.Value);

            GameObject obj = TakeFromPool();
            obj.transform.position = spawnPos.Value;
            obj.transform.rotation = transform.rotation;
            obj.SetActive(true);

            _activeObjects.Add(obj);
            OnObjectSpawned(obj);
        }

        // Aspetto che tutti gli oggetti attivi vengano disabilitati
        yield return StartCoroutine(WaitForWaveCompletion(wave));

        _waveCompleted = true;
        _waveRunning = false;
        _currentWaveIndex++;

        OnWaveCompleted(_currentWaveIndex - 1);

        // Se il player è ancora dentro parto subito con la prossima wave
        if (_playerInside && !_allWavesDone)
            StartCoroutine(RunNextWave());
    }

    /// <summary>
    /// Attende che tutti gli oggetti attivi della wave vengano disabilitati.
    /// Rimuove dalla lista quelli già disattivati ad ogni check.
    /// </summary>
    private IEnumerator WaitForWaveCompletion(WaveData wave)
    {
        while (true)
        {
            // Rimuovo gli oggetti già disabilitati
            _activeObjects.RemoveAll(obj => obj == null || !obj.activeSelf);

            if (_activeObjects.Count == 0)
                yield break;

            yield return new WaitForSeconds(0.2f); // check ogni 200ms
        }
    }

    /// <summary>
    /// Il player è uscito mentre la wave era in corso:
    /// disabilita tutti gli oggetti e marca la wave come completed.
    /// </summary>
    private IEnumerator ForceCompleteWave()
    {
        StopCoroutine(nameof(RunNextWave));

        foreach (var obj in _activeObjects)
            if (obj != null && obj.activeSelf)
                obj.SetActive(false);

        _activeObjects.Clear();
        _waveCompleted = true;
        _waveRunning = false;

        yield return null;
    }

    // -----------------------------------------------------------------------
    // SPHERECAST — trova un punto di spawn sui muri
    // -----------------------------------------------------------------------
    private Vector3? FindSpawnPosition(WaveData wave, List<Vector3> occupied)
    {
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            // Punto random all'interno del BoxCollider in spazio locale
            Vector3 localPoint = new Vector3(
                (float)(_rng.NextDouble() * 2 - 1) * (_box.size.x * 0.5f) + _box.center.x,
                (float)(_rng.NextDouble() * 2 - 1) * (_box.size.y * 0.5f) + _box.center.y,
                (float)(_rng.NextDouble() * 2 - 1) * (_box.size.z * 0.5f) + _box.center.z
            );

            Vector3 worldPoint = transform.TransformPoint(localPoint);

            // SphereCast verso tutte e 6 le direzioni: prendo la prima che colpisce un muro
            Vector3? hitPoint = null;
            float closestDist = Mathf.Infinity;

            foreach (Vector3 localDir in _sixDirections)
            {
                Vector3 worldDir = transform.TransformDirection(localDir);

                if (Physics.SphereCast(worldPoint, wave.sphereCastRadius, worldDir,
                    out RaycastHit hit, Mathf.Infinity, wallLayerMask))
                {
                    if (hit.distance < closestDist)
                    {
                        closestDist = hit.distance;
                        hitPoint = hit.point;
                    }
                }
            }

            if (hitPoint == null) continue;

            // Controlla distanza minima dagli altri spawn già piazzati
            bool tooClose = false;
            foreach (var pos in occupied)
            {
                if (Vector3.Distance(hitPoint.Value, pos) < wave.minDistanceBetweenSpawns)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose) return hitPoint;
        }

        Debug.LogWarning($"[Spawner] Nessuna posizione valida trovata dopo {maxSpawnAttempts} tentativi.");
        return null;
    }

    // -----------------------------------------------------------------------
    // POOL CONDIVISO FIFO CIRCOLARE
    // -----------------------------------------------------------------------
    private void EnsurePool(GameObject prefab, int waveSpawnCount)
    {
        // Se il prefab è cambiato rispetto al pool corrente, ricreo il pool
        if (_pool != null && prefab == _currentPrefab) return;

        // Distruggo il vecchio pool se il prefab è cambiato
        if (_pool != null && prefab != _currentPrefab)
        {
            foreach (var go in _pool)
                if (go != null) Destroy(go);
            _pool.Clear();
        }

        int size = Mathf.Max(poolSize, waveSpawnCount);
        _pool = new Queue<GameObject>(size);
        _currentPrefab = prefab;

        for (int i = 0; i < size; i++)
        {
            var go = Instantiate(prefab);
            go.SetActive(false);
            _pool.Enqueue(go);
        }
    }

    private GameObject TakeFromPool()
    {
        var go = _pool.Dequeue();

        if (go.activeSelf)
        {
            Debug.LogWarning($"[Spawner] Pool esaurito: riciclo forzato di {go.name}. Aumenta poolSize.");
            go.SetActive(false);
        }

        _pool.Enqueue(go);
        return go;
    }

    // -----------------------------------------------------------------------
    // HOOKS — override nelle classi figlie
    // -----------------------------------------------------------------------

    /// <summary>Chiamato ogni volta che un oggetto viene spawnato.</summary>
    protected virtual void OnObjectSpawned(GameObject obj) { }

    /// <summary>Chiamato al termine di ogni wave. waveIndex è l'indice della wave appena completata.</summary>
    protected virtual void OnWaveCompleted(int waveIndex) { }

    /// <summary>Chiamato quando tutte le wave sono completate.</summary>
    protected virtual void OnAllWavesCompleted() { }

    // -----------------------------------------------------------------------
    // GIZMOS — visualizza il BoxCollider nell'Editor
    // -----------------------------------------------------------------------
    private void OnDrawGizmosSelected()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        if (col == null) return;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.15f);
        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(
            transform.TransformPoint(col.center),
            transform.rotation,
            transform.lossyScale);
        Gizmos.DrawCube(Vector3.zero, col.size);
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 1f);
        Gizmos.DrawWireCube(Vector3.zero, col.size);
        Gizmos.matrix = old;
    }
}