using UnityEngine;

[CreateAssetMenu(fileName = "NewWaveData", menuName = "Game/Wave Data")]
public class WaveData : ScriptableObject
{
    [Header("Spawn Object")]
    [Tooltip("Prefab del GameObject da spawnare in questa wave")]
    public GameObject prefab;

    [Header("Wave Settings")]
    [Tooltip("Numero di oggetti da spawnare in questa wave")]
    public int spawnCount = 5;

    [Tooltip("Intervallo minimo in secondi tra uno spawn e il successivo")]
    public float minSpawnInterval = 0.5f;

    [Tooltip("Intervallo massimo in secondi tra uno spawn e il successivo")]
    public float maxSpawnInterval = 4f;

    [Header("Randomness")]
    [Tooltip("Seed per la generazione pseudo-casuale delle posizioni. Stesso seed = stesse posizioni")]
    public int seed = 42;

    [Header("SphereCast")]
    [Tooltip("Raggio dello SphereCast usato per trovare il punto di spawn sui muri")]
    public float sphereCastRadius = 0.3f;

    [Tooltip("Distanza minima tra un punto di spawn e il successivo")]
    public float minDistanceBetweenSpawns = 1f;
}