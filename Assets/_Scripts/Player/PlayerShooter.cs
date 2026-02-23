using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Estende Shooter per il player.
/// Calcola posizione, direzione e rotazione del proiettile in stile "lancio del giavellotto":
///   - La direzione orizzontale è il forward della camera proiettato sul piano XZ (ignora il pitch)
///   - La direzione verticale è sempre +45° fissi sopra l'orizzontale
/// Aggiungi questo script sullo stesso GameObject che ha PlayerInput.
/// </summary>
public class PlayerShooter : Shooter
{
    [Header("Player Shooter")]
    [Tooltip("Empty GameObject figlio del player posizionato davanti, da cui partono i proiettili")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("Transform della camera in terza persona, usata per calcolare la direzione di sparo")]
    [SerializeField] private Transform cameraTransform;

    [Tooltip("Angolo verticale fisso di lancio in gradi sopra l'orizzontale (default 45°)")]
    [SerializeField] private float launchAngle = 45f;

    private InputAction _fireAction;

    private void Start()
    {
        PlayerInput playerInput = GetComponentInParent<PlayerInput>();

        if (playerInput == null)
        {
            Debug.LogError("[PlayerShooter] Nessun PlayerInput trovato sul GameObject o nei suoi parent.");
            return;
        }

        var map = playerInput.actions.FindActionMap("Player", throwIfNotFound: true);
        _fireAction = map.FindAction("Fire", throwIfNotFound: true);
        _fireAction.Enable();

        PrewarmPool();
    }

    private void OnDisable()
    {
        _fireAction?.Disable();
    }

    private void Update()
    {
        if (_fireAction != null && _fireAction.WasPressedThisFrame())
            Fire();
    }

    /// <summary>
    /// Calcola posizione, direzione e rotazione del proiettile e le passa a TryShoot.
    /// </summary>
    private void Fire()
    {
        if (spawnPoint == null)
        {
            Debug.LogError("[PlayerShooter] SpawnPoint non assegnato nell'Inspector.");
            return;
        }

        if (cameraTransform == null)
        {
            Debug.LogError("[PlayerShooter] CameraTransform non assegnato nell'Inspector.");
            return;
        }

        // 1. Prendo il forward della camera e lo proietto sul piano orizzontale (ignoro il pitch)
        Vector3 cameraForwardFlat = cameraTransform.forward;
        cameraForwardFlat.y = 0f;
        cameraForwardFlat.Normalize();

        // 2. Aggiungo i 45° verticali fissi sopra l'orizzontale (stile lancio del giavellotto)
        //    Ruoto il vettore orizzontale attorno all'asse destro della camera (sinistra/destra)
        Vector3 right = cameraTransform.right;
        right.y = 0f;
        right.Normalize();

        Vector3 launchDirection = Quaternion.AngleAxis(-launchAngle, right) * cameraForwardFlat;
        launchDirection.Normalize();

        // 3. La rotazione del proiettile è allineata alla direzione di lancio
        Quaternion launchRotation = Quaternion.LookRotation(launchDirection);

        // 4. La posizione di spawn è quella dell'empty figlio del player
        Vector3 spawnPosition = spawnPoint.position;

        TryShoot(spawnPosition, launchDirection, launchRotation);
    }
}