using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

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

    [Header("Dash")]
    [SerializeField] private float dashMultiplier = 3f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1.5f;

    private CharacterController _characterController;
    private Player_Input _playerInput;
    private Animator _animator;

    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _sprintAction;
    private InputAction _dashAction;

    private Vector3 _velocity;
    private bool _isGrounded;
    private bool _isSprinting;
    private bool _isDashing;
    private bool _isDashOnCooldown;

    private float _pitch;
    private float _yaw;

    private void Awake()
    {
        // 1. Riferimento al CharacterController (sullo stesso oggetto della capsula)
        _characterController = GetComponent<CharacterController>();

        // 2. Inizializzazione dell'Input System
        _playerInput = new Player_Input();

        // 3. Collegamento delle Azioni (Assicurati che i nomi corrispondano al tuo Input Action Asset)
        _moveAction = _playerInput.Player.Move;
        _jumpAction = _playerInput.Player.Jump;
        _sprintAction = _playerInput.Player.Sprint;
        _dashAction = _playerInput.Player.Dash;

        // 4. Collegamento dell'Animator con Debug Log per verifica
        // Cerca in questo oggetto e in tutti i figli (il modello FBX)
        _animator = GetComponentInChildren<Animator>();

        if (_animator == null)
        {
            Debug.LogError($"<color=red><b>[PlayerController]</b> Animator NON trovato su {gameObject.name} o nei figli! Controlla il modello FBX.</color>");
        }
        else
        {
            Debug.Log($"<color=green><b>[PlayerController]</b> Animator collegato con successo su: {_animator.gameObject.name}</color>");

            // Se l'Avatar non è assegnato nell'inspector, Unity darà un warning qui
            if (_animator.avatar == null)
            {
                Debug.LogWarning("[PlayerController] L'Animator ha un controller ma l'Avatar è NULL! Le animazioni non partiranno.");
            }
        }

        // 5. Configurazione Mouse (per la telecamera)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable() => _playerInput.Enable();
    private void OnDisable() => _playerInput.Disable();

    private void Update()
    {
        GroundCheck();
        HandleCamera();

        if (!_isDashing)
        {
            Move();
            Jump();
        }

        Dash();
        UpdateAnimationParameters();
    }

    private void GroundCheck()
    {
        _isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask);
        if (_isGrounded && _velocity.y < 0) _velocity.y = -2f;
    }

    private void HandleCamera()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        _yaw += mouseDelta.x * mouseSensitivity * 0.1f;
        _pitch -= mouseDelta.y * mouseSensitivity * 0.1f;
        _pitch = Mathf.Clamp(_pitch, minVerticalAngle, maxVerticalAngle);

        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 targetPosition = transform.position + Vector3.up * cameraHeight - (rotation * Vector3.forward * cameraDistance);

        cameraTransform.position = targetPosition;
        cameraTransform.rotation = rotation;
    }

    private void Move()
    {
        Vector2 input = _moveAction.ReadValue<Vector2>();
        _isSprinting = _sprintAction.IsPressed();

        float currentSpeed = _isSprinting ? normalSpeed * sprintMultiplier : normalSpeed;

        if (input.magnitude >= 0.1f)
        {
            // La rotazione ora segue la camera (Yaw)
            float targetAngle = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg + _yaw;
            Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothTime);

            Vector3 moveDir = targetRotation * Vector3.forward;
            _characterController.Move(moveDir.normalized * currentSpeed * Time.deltaTime);
        }

        _velocity.y += gravity * Time.deltaTime;
        _characterController.Move(_velocity * Time.deltaTime);
    }

    private void Jump()
    {
        if (_jumpAction.WasPressedThisFrame() && _isGrounded)
        {
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            if (_animator != null) _animator.SetTrigger("Jump");
        }
    }

    private void Dash()
    {
        if (!_dashAction.WasPressedThisFrame() || _isDashing || _isDashOnCooldown) return;
        Vector2 input = _moveAction.ReadValue<Vector2>();
        if (input.magnitude < 0.1f) return;
        StartCoroutine(DashCoroutine(input));
    }

    private IEnumerator DashCoroutine(Vector2 input)
    {
        _isDashing = true;
        _isDashOnCooldown = true;
        if (_animator != null) _animator.SetTrigger("Dash");

        float dashSpeed = normalSpeed * dashMultiplier;
        float elapsed = 0f;
        float targetAngle = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg + _yaw;
        Vector3 dashDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

        while (elapsed < dashDuration)
        {
            _characterController.Move(dashDirection.normalized * dashSpeed * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        _isDashing = false;
        yield return new WaitForSeconds(dashCooldown);
        _isDashOnCooldown = false;
    }

    private void UpdateAnimationParameters()
    {
        if (_animator == null) return;

        Vector2 input = _moveAction.ReadValue<Vector2>();
        float speedValue = input.magnitude;
        if (_isSprinting && speedValue > 0.1f) speedValue *= sprintMultiplier;

        // I nomi qui sotto devono essere IDENTICI a quelli nell'Animator
        _animator.SetFloat("Speed", speedValue, 0.1f, Time.deltaTime);
        _animator.SetBool("isGrounded", _isGrounded);
    }
}