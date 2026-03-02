using UnityEngine;

[CreateAssetMenu(fileName = "WaveData", menuName = "Waves/WaveData")]
public class WaveData : ScriptableObject
{
    [Tooltip("Prefab da spawnare per questa wave")]
    public GameObject spawnPrefab;

    [Tooltip("Numero di oggetti da spawnare")]
    public int spawnCount = 10;
}