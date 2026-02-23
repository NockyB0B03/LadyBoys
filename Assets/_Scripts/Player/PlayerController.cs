using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float normalSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.3f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float rotationSmoothTime = 0.1f;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minVerticalAngle = -30f;
    [SerializeField] private float maxVerticalAngle = 60f;
    [SerializeField] private float cameraDistance = 4f;
    [SerializeField] private float cameraHeight = 2f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundMask;

    private bool _isGrounded;

    // Componenti
    private CharacterController _characterController;
    private PlayerInput _playerInput;

    // Input Actions
    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _jumpAction;
    private InputAction _sprintAction;
    private InputAction _fireAction;
    private InputAction _pauseAction;
    private InputAction _punchAction;
    private InputAction _legioniCelestiAction;
    private InputAction _healAction;
    private InputAction _interactAction;

    // Stato interno
    private Vector3 _velocity;
    private float _yaw;    // rotazione orizzontale camera
    private float _pitch;  // rotazione verticale camera
    private float _rotationVelocity;
    private bool _isSprinting;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _playerInput = GetComponent<PlayerInput>();

        // Recupero le action dalla action map "Player"
        var map = _playerInput.actions.FindActionMap("Player", throwIfNotFound: true);
        _moveAction = map.FindAction("Move", throwIfNotFound: true);
        _lookAction = map.FindAction("Look", throwIfNotFound: true);
        _jumpAction = map.FindAction("Jump", throwIfNotFound: true);
        _sprintAction = map.FindAction("Sprint", throwIfNotFound: true);
        _fireAction = map.FindAction("Fire", throwIfNotFound: true);
        _pauseAction = map.FindAction("Pause", throwIfNotFound: true);
        _punchAction = map.FindAction("Punch", throwIfNotFound: true);
        _legioniCelestiAction = map.FindAction("LegioniCelesti", throwIfNotFound: true);
        _healAction = map.FindAction("Heal", throwIfNotFound: true);
        _interactAction = map.FindAction("Interact", throwIfNotFound: true);

        // Inizializzo yaw con la rotazione attuale del player
        _yaw = transform.eulerAngles.y;

        // Nascondo il cursore
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        _moveAction.Enable();
        _lookAction.Enable();
        _jumpAction.Enable();
        _sprintAction.Enable();
        _fireAction.Enable();
        _pauseAction.Enable();
        _punchAction.Enable();
        _legioniCelestiAction.Enable();
        _healAction.Enable();
        _interactAction.Enable();
    }

    private void OnDisable()
    {
        _moveAction?.Disable();
        _lookAction?.Disable();
        _jumpAction?.Disable();
        _sprintAction?.Disable();
        _fireAction?.Disable();
        _pauseAction?.Disable();
        _punchAction?.Disable();
        _legioniCelestiAction?.Disable();
        _healAction?.Disable();
        _interactAction?.Disable();
    }

    private void Update()
    {
        Look();
        Move();
        Jump();
        Sprint();
        Fire();
        Pause();
        Punch();
        LegioniCelesti();
        Heal();
        Interact();

        // Applico la gravità
        _isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask);

        if (_isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;

        _velocity.y += gravity * Time.deltaTime;
        _characterController.Move(new Vector3(0f, _velocity.y, 0f) * Time.deltaTime);
    }

    // -----------------------------------------------------------------------
    // MOVE — WASD
    // -----------------------------------------------------------------------
    private void Move()
    {
        Vector2 input = _moveAction.ReadValue<Vector2>();
        Vector3 direction = new Vector3(input.x, 0f, input.y).normalized;

        if (direction.magnitude >= 0.1f)
        {
            // Ruoto il player nella direzione della camera
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + _yaw;
            float smoothAngle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetAngle,
                ref _rotationVelocity,
                rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);

            // Muovo nella direzione in cui punta la camera
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            float currentSpeed = _isSprinting ? normalSpeed * sprintMultiplier : normalSpeed;
            _characterController.Move(moveDir.normalized * currentSpeed * Time.deltaTime);
        }
    }

    // -----------------------------------------------------------------------
    // LOOK — Mouse: la camera orbita attorno al player
    // -----------------------------------------------------------------------
    private void Look()
    {
        Vector2 mouseDelta = _lookAction.ReadValue<Vector2>();

        _yaw += mouseDelta.x * mouseSensitivity * Time.deltaTime * 100f;
        _pitch -= mouseDelta.y * mouseSensitivity * Time.deltaTime * 100f;
        _pitch = Mathf.Clamp(_pitch, minVerticalAngle, maxVerticalAngle);

        if (cameraTransform == null) return;

        // Calcolo la posizione della camera in orbita attorno al player
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 offset = rotation * new Vector3(0f, cameraHeight, -cameraDistance);
        cameraTransform.position = transform.position + offset;

        // La camera guarda sempre verso il player (leggermente sopra i piedi)
        cameraTransform.LookAt(transform.position + Vector3.up * (cameraHeight * 0.5f));
    }

    // -----------------------------------------------------------------------
    // JUMP — Space
    // -----------------------------------------------------------------------
    private void Jump()
    {
        if (_jumpAction.WasPressedThisFrame() && _isGrounded)
        {
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    // -----------------------------------------------------------------------
    // SPRINT — Left Shift (held)
    // -----------------------------------------------------------------------
    private void Sprint()
    {
        _isSprinting = _sprintAction.IsPressed();
    }

    // -----------------------------------------------------------------------
    // FIRE — placeholder
    // -----------------------------------------------------------------------
    private void Fire()
    {
        // TODO
    }

    // -----------------------------------------------------------------------
    // PAUSE — placeholder
    // -----------------------------------------------------------------------
    private void Pause()
    {
        // TODO
    }

    // -----------------------------------------------------------------------
    // PUNCH — placeholder
    // -----------------------------------------------------------------------
    private void Punch()
    {
        // TODO
    }

    // -----------------------------------------------------------------------
    // LEGIONI CELESTI — placeholder
    // -----------------------------------------------------------------------
    private void LegioniCelesti()
    {
        // TODO
    }

    // -----------------------------------------------------------------------
    // HEAL — placeholder
    // -----------------------------------------------------------------------
    private void Heal()
    {
        // TODO
    }

    // -----------------------------------------------------------------------
    // INTERACT — placeholder
    // -----------------------------------------------------------------------
    private void Interact()
    {
        // TODO
    }
}