using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Classe base astratta per il sistema di sparo.
/// Gestisce l'object pooling FIFO circolare dei proiettili e il fire rate.
/// Il pool è auto-sufficiente: nessun callback esterno, nessuna dipendenza da OnDisable.
/// </summary>
public abstract class Shooter : MonoBehaviour
{
    [Header("Shooter Base")]
    [Tooltip("BulletData attualmente equipaggiato")]
    [SerializeField] protected BulletData currentBulletData;

    [Tooltip("Dimensione del pool per ogni tipo di proiettile")]
    [SerializeField] protected int bulletPoolSize = 30;

    // Pool FIFO circolare: Dictionary prefab -> Queue di istanze
    protected Dictionary<GameObject, Queue<GameObject>> bulletPools = new();

    // Tempo dell'ultimo sparo, usato per calcolare il fire rate
    private float _lastFireTime = -Mathf.Infinity;

    // -----------------------------------------------------------------------
    // POOL — inizializza il pool per un prefab se non esiste ancora
    // -----------------------------------------------------------------------
    protected void EnsurePool(GameObject prefab)
    {
        if (prefab == null || bulletPools.ContainsKey(prefab)) return;

        Queue<GameObject> q = new Queue<GameObject>(Mathf.Max(1, bulletPoolSize));
        for (int i = 0; i < Mathf.Max(1, bulletPoolSize); i++)
        {
            var go = Instantiate(prefab);
            go.SetActive(false);
            q.Enqueue(go);
        }
        bulletPools[prefab] = q;
    }

    /// <summary>
    /// Preleva un oggetto dal pool FIFO circolare.
    /// Se l'oggetto estratto è ancora attivo (pool esaurito), lo disattiva forzatamente e lo riusa.
    /// L'oggetto viene rimesso in fondo alla queue immediatamente dopo essere stato estratto.
    /// </summary>
    protected GameObject TakeFromPool(GameObject prefab)
    {
        EnsurePool(prefab);

        var q = bulletPools[prefab];
        var go = q.Dequeue();

        // Se è ancora attivo il pool è esaurito: riciclo forzato
        if (go.activeSelf)
        {
            Debug.LogWarning($"[Shooter] Pool esaurito per {prefab.name}: riciclo forzato. Considera di aumentare bulletPoolSize.");
            go.SetActive(false);
        }

        // Rimetto subito in fondo alla queue (FIFO circolare)
        q.Enqueue(go);

        return go;
    }

    // -----------------------------------------------------------------------
    // PREWARM — chiama EnsurePool al momento giusto per evitare lag al primo sparo
    // -----------------------------------------------------------------------
    protected void PrewarmPool()
    {
        if (currentBulletData == null || currentBulletData.bulletPrefab == null) return;
        EnsurePool(currentBulletData.bulletPrefab);
    }

    // -----------------------------------------------------------------------
    // TRY SHOOT — chiamato dalle classi figlie, rispetta il fire rate
    // -----------------------------------------------------------------------
    protected void TryShoot(Vector3 spawnPosition, Vector3 direction, Quaternion rotation)
    {
        if (currentBulletData == null)
        {
            Debug.LogWarning($"[Shooter] {gameObject.name}: nessun BulletData assegnato.");
            return;
        }

        float fireCooldown = 1f / Mathf.Max(currentBulletData.fireRate, 0.0001f);
        if (Time.time - _lastFireTime < fireCooldown) return;

        _lastFireTime = Time.time;
        Shoot(spawnPosition, direction, rotation);
    }

    private void Shoot(Vector3 spawnPosition, Vector3 direction, Quaternion rotation)
    {
        GameObject bulletObj = TakeFromPool(currentBulletData.bulletPrefab);
        bulletObj.transform.position = spawnPosition;
        bulletObj.transform.rotation = rotation;
        bulletObj.SetActive(true);

        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet == null)
        {
            Debug.LogError($"[Shooter] Il prefab {currentBulletData.bulletPrefab.name} non ha il componente Bullet!");
            return;
        }

        bullet.Initialize(currentBulletData, direction);
    }

    // -----------------------------------------------------------------------
    // CAMBIA PROIETTILE
    // -----------------------------------------------------------------------
    public void SetBulletData(BulletData newData)
    {
        currentBulletData = newData;
        PrewarmPool();
    }
}