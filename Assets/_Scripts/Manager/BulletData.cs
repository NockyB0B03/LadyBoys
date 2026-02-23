using UnityEngine;

[CreateAssetMenu(fileName = "NewBulletData", menuName = "Game/Bullet Data")]
public class BulletData : ScriptableObject
{
    [Header("Visuals")]
    [Tooltip("Prefab che rappresenta visivamente il proiettile (freccia, libro, ecc.)")]
    public GameObject bulletPrefab;

    [Header("Combat")]
    [Tooltip("Danno inflitto al colpito")]
    public float damage = 10f;

    [Header("Physics")]
    [Tooltip("Velocità iniziale del lancio in m/s")]
    public float speed = 20f;

    [Tooltip("Moltiplicatore della gravità applicata al proiettile (1 = gravità normale, 0 = nessuna gravità)")]
    public float gravityScale = 1f;

    [Header("Fire Rate")]
    [Tooltip("Numero di proiettili sparabili al secondo")]
    public float fireRate = 2f;

    [Header("Collision")]
    [Tooltip("Layer mask che definisce cosa può colpire questo proiettile")]
    public LayerMask hitLayers;
}