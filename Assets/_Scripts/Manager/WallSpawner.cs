using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Va assegnato a un cubo in scena.
/// Genera coordinate casuali all'interno del cubo, lancia 6 SphereCast (±X, ±Y, ±Z)
/// e spawna il prefab della wave nel punto di hit del primo muro colpito.
/// Gli oggetti vengono gestiti tramite un pool. La trappola si riattiva se il player
/// rientra dopo che gli oggetti sono stati reinseriti nel pool.
/// </summary>
public class WallSpawner : MonoBehaviour
{
    [Header("Wave")]
    [SerializeField] private WaveData wave;

    [Header("SphereCast Settings")]
    [SerializeField] private float sphereRadius = 0.1f;
    [Tooltip("Distanza massima di cast (deve essere abbastanza grande da raggiungere i muri)")]
    [SerializeField] private float castMaxDistance = 100f;

    [Header("Trigger Settings")]
    [SerializeField] private string playerLayerName = "Player";
    [SerializeField] private float spawnDelay = 1.5f;

    [Header("Trap Settings")]
    [Tooltip("Secondi dopo l'uscita del player prima che gli oggetti vengano reinseriti nel pool e la trappola si possa riattivare")]
    [SerializeField] private float deactivationDelay = 3f;

    // --- Stato trappola ---
    private bool trappolaAttivata = false;   // true = oggetti attivi in scena
    private bool playerInside = false;

    // --- Layer ---
    private LayerMask wallLayerMask;
    private int playerLayer;

    // --- Bounds ---
    private Bounds cubeBounds;

    // --- Pool ---
    private Queue<GameObject> pool = new Queue<GameObject>();
    private List<GameObject> activeObjects = new List<GameObject>();

    // --- Spawn data (posizione e rotazione pre-calcolate) ---
    private List<(Vector3 position, Quaternion rotation)> spawnPoints = new List<(Vector3, Quaternion)>();

    // --- Coroutines ---
    private Coroutine spawnDelayCoroutine;
    private Coroutine deactivationCoroutine;

    // Direzioni dei 6 raggi: ±X, ±Y, ±Z
    private static readonly Vector3[] CastDirections = new Vector3[]
    {
        Vector3.right,
        Vector3.left,
        Vector3.up,
        Vector3.down,
        Vector3.forward,
        Vector3.back
    };

    private void Awake()
    {
        int wallLayer = LayerMask.NameToLayer("muri");
        if (wallLayer == -1)
        {
            Debug.LogError("[WallSpawner] Layer 'muri' non trovato!");
            return;
        }
        wallLayerMask = 1 << wallLayer;

        playerLayer = LayerMask.NameToLayer(playerLayerName);
        if (playerLayer == -1)
            Debug.LogError($"[WallSpawner] Layer '{playerLayerName}' non trovato!");

        Renderer rend = GetComponent<Renderer>();
        cubeBounds = rend != null ? rend.bounds : new Bounds(transform.position, transform.lossyScale);

        if (wave == null || wave.spawnPrefab == null)
        {
            Debug.LogError("[WallSpawner] WaveData o spawnPrefab mancante!");
            return;
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        // Pre-calcola i punti di spawn e costruisce il pool
        BuildSpawnPointsAndPool();
    }

    // -----------------------------------------------------------------------
    // TRIGGER
    // -----------------------------------------------------------------------

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != playerLayer) return;
        playerInside = true;

        // Ferma eventuale coroutine di disattivazione in corso
        if (deactivationCoroutine != null)
        {
            StopCoroutine(deactivationCoroutine);
            deactivationCoroutine = null;
        }

        // Se la trappola è già attiva, non fare nulla
        if (trappolaAttivata) return;

        // Avvia spawn con delay
        if (spawnDelayCoroutine != null) StopCoroutine(spawnDelayCoroutine);
        spawnDelayCoroutine = StartCoroutine(SpawnWithDelay());
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer != playerLayer) return;
        playerInside = false;

        if (trappolaAttivata)
        {
            // Avvia il timer di disattivazione
            if (deactivationCoroutine != null) StopCoroutine(deactivationCoroutine);
            deactivationCoroutine = StartCoroutine(DeactivationTimer());
        }
        else
        {
            // Il player è uscito prima che finisse il delay di spawn: annulla
            if (spawnDelayCoroutine != null)
            {
                StopCoroutine(spawnDelayCoroutine);
                spawnDelayCoroutine = null;
            }
        }
    }

    // -----------------------------------------------------------------------
    // COROUTINES
    // -----------------------------------------------------------------------

    private IEnumerator SpawnWithDelay()
    {
        yield return new WaitForSeconds(spawnDelay);

        // Doppio check: il player potrebbe essere uscito nel frattempo
        if (!playerInside)
        {
            spawnDelayCoroutine = null;
            yield break;
        }

        ActivateTrap();
        spawnDelayCoroutine = null;
    }

    private IEnumerator DeactivationTimer()
    {
        yield return new WaitForSeconds(deactivationDelay);
        ReturnAllToPool();
        trappolaAttivata = false;
        deactivationCoroutine = null;
    }

    // -----------------------------------------------------------------------
    // TRAP LOGIC
    // -----------------------------------------------------------------------

    private void ActivateTrap()
    {
        trappolaAttivata = true;

        foreach (var (position, rotation) in spawnPoints)
        {
            GameObject obj = TakeFromPool();
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            activeObjects.Add(obj);
        }

        Debug.Log($"[WallSpawner] Trappola attivata: {activeObjects.Count} oggetti spawnati.");
    }

    private void ReturnAllToPool()
    {
        foreach (GameObject obj in activeObjects)
        {
            if (obj == null) continue;
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
        activeObjects.Clear();
        Debug.Log("[WallSpawner] Oggetti reinseriti nel pool. Trappola pronta.");
    }

    // -----------------------------------------------------------------------
    // SPAWN POINTS PRE-CALC + POOL BUILD
    // -----------------------------------------------------------------------

    private void BuildSpawnPointsAndPool()
    {
        int totalToSpawn = wave.spawnCount;

        // Pre-calcola tutti i punti di spawn via SphereCast
        int spawned = 0;
        while (spawned < totalToSpawn)
        {
            Vector3 randomPoint = GetRandomPointInsideBounds(cubeBounds);

            foreach (Vector3 direction in CastDirections)
            {
                if (Physics.SphereCast(randomPoint, sphereRadius, direction, out RaycastHit hit, castMaxDistance, wallLayerMask))
                {
                    Vector3 upAxis = -direction;
                    Vector3 forwardAxis = hit.normal;

                    if (Vector3.Cross(upAxis, forwardAxis).sqrMagnitude < 0.001f)
                        forwardAxis = Mathf.Abs(upAxis.y) < 0.9f ? Vector3.up : Vector3.forward;

                    Quaternion spawnRotation = Quaternion.LookRotation(forwardAxis, upAxis);
                    float heightOffset = GetPrefabHalfHeight(wave.spawnPrefab, upAxis, spawnRotation);
                    Vector3 spawnPosition = hit.point + upAxis * heightOffset;

                    spawnPoints.Add((spawnPosition, spawnRotation));
                    spawned++;
                    break;
                }
            }
        }

        // Costruisce il pool con esattamente spawnCount oggetti, tutti disattivi
        for (int i = 0; i < totalToSpawn; i++)
        {
            GameObject obj = Instantiate(wave.spawnPrefab);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }

        Debug.Log($"[WallSpawner] Pool creato con {totalToSpawn} oggetti, {spawnPoints.Count} punti di spawn calcolati.");
    }

    // -----------------------------------------------------------------------
    // POOL
    // -----------------------------------------------------------------------

    private GameObject TakeFromPool()
    {
        if (pool.Count == 0)
        {
            Debug.LogWarning("[WallSpawner] Pool esaurito, creo un nuovo oggetto.");
            GameObject extra = Instantiate(wave.spawnPrefab);
            extra.SetActive(false);
            return extra;
        }
        return pool.Dequeue();
    }

    // -----------------------------------------------------------------------
    // HELPERS
    // -----------------------------------------------------------------------

    private float GetPrefabHalfHeight(GameObject prefab, Vector3 upAxis, Quaternion rotation)
    {
        GameObject temp = Instantiate(prefab, Vector3.zero, rotation);
        temp.SetActive(false);

        Bounds bounds;
        bool found = false;

        Collider col = temp.GetComponentInChildren<Collider>();
        if (col != null)
        {
            bounds = col.bounds;
            found = true;
        }
        else
        {
            Renderer rend = temp.GetComponentInChildren<Renderer>();
            if (rend != null) { bounds = rend.bounds; found = true; }
            else bounds = new Bounds();
        }

        Destroy(temp);

        if (!found)
        {
            Debug.LogWarning("[WallSpawner] Nessun Collider né Renderer trovato sul prefab, offset = 0.");
            return 0f;
        }

        Vector3 absUp = new Vector3(
            Mathf.Abs(upAxis.normalized.x),
            Mathf.Abs(upAxis.normalized.y),
            Mathf.Abs(upAxis.normalized.z));
        return Vector3.Dot(bounds.extents, absUp);
    }

    private Vector3 GetRandomPointInsideBounds(Bounds bounds)
    {
        return new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            Random.Range(bounds.min.z, bounds.max.z)
        );
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Renderer rend = GetComponent<Renderer>();
        Bounds b = rend != null ? rend.bounds : new Bounds(transform.position, transform.lossyScale);
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        Gizmos.DrawCube(b.center, b.size);
        Gizmos.color = new Color(0f, 1f, 0.5f, 1f);
        Gizmos.DrawWireCube(b.center, b.size);
    }
#endif
}