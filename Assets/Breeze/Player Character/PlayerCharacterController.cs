using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerCharacterController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 9f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -19.62f; // 2x gravity feels snappier

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private float maxLookAngle = 85f;
    [SerializeField] private Transform cameraHolder; // Assign the CameraHolder child

    [Header("Crouch")]
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float crouchHeight = 1f;       // Height while crouched
    [SerializeField] private float standingHeight = 2f;     // Regular standing height
    [SerializeField] private float crouchTransitionSpeed = 8f;
    [SerializeField] private float crouchCameraY = 0.4f;    // CameraHolder Y when crouched
    [SerializeField] private float standingCameraY = 0.8f;  // CameraHolder Y when standing
    private bool _isCrouching;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundCheckDistance = 0.05f;

    // Components
    private CharacterController _controller;

    // Input values
    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private bool _jumpPressed;
    private bool _isSprinting;

    // State
    private Vector3 _velocity;
    private float _cameraPitch; // Vertical camera rotation (clamped)
    private bool _isGrounded;

    // -----------------------------------------------------------------------
    // Unity Lifecycle
    // -----------------------------------------------------------------------

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();

        // Lock and hide the cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleGroundCheck();
        HandleGravity();
        HandleCrouch();
        HandleMovement();
        HandleLook();

        Debug.Log($"sprinting: {_isSprinting} | crouching: {_isCrouching} | speed: {(_isCrouching ? crouchSpeed : (_isSprinting ? sprintSpeed : walkSpeed))}");
    }

    // -----------------------------------------------------------------------
    // Input System Callbacks  (matched by PlayerInput component via Send Messages)
    // -----------------------------------------------------------------------

    // Called automatically by PlayerInput when the "Move" action fires
    public void OnMove(InputValue value)
    {
        _moveInput = value.Get<Vector2>();
    }

    // Called automatically by PlayerInput when the "Look" action fires
    public void OnLook(InputValue value)
    {
        _lookInput = value.Get<Vector2>();
    }

    // Called automatically by PlayerInput when the "Jump" action fires
    public void OnJump(InputValue value)
    {
        if (value.isPressed && _isGrounded)
            _jumpPressed = true;
    }

    // Called automatically by PlayerInput when the "Crouch" action fires
    public void OnCrouch(InputValue value)
    {
        if (value.Get<float>() > 0f)
            _isCrouching = !_isCrouching;
    }

    // Called automatically by PlayerInput when the "Sprint" action fires
    public void OnSprint(InputValue value)
    {
        if (value.Get<float>() > 0f)
            _isSprinting = !_isSprinting;
    }

    // -----------------------------------------------------------------------
    // Movement & Physics
    // -----------------------------------------------------------------------

    private void HandleGroundCheck()
    {
        // Sphere cast slightly below the controller's bottom edge
        Vector3 sphereOrigin = transform.position + Vector3.up * (_controller.radius);
        _isGrounded = Physics.SphereCast(
            sphereOrigin,
            _controller.radius - 0.01f,
            Vector3.down,
            out _,
            _controller.radius + groundCheckDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        // Reset downward velocity when grounded so we don't accumulate
        if (_isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;
    }

    private void HandleGravity()
    {
        if (!_isGrounded)
            _velocity.y += gravity * Time.deltaTime;
    }

    private void HandleMovement()
    {
        // Jump
        if (_jumpPressed)
        {
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            _jumpPressed = false;
        }

        // Horizontal movement in local space with speed modifiers for crouching/sprinting
        float speed = _isCrouching ? crouchSpeed : (_isSprinting ? sprintSpeed : walkSpeed);

        Vector3 move = transform.right * _moveInput.x + transform.forward * _moveInput.y;

        _controller.Move((move * speed + _velocity) * Time.deltaTime);
    }

    private void HandleLook()
    {
        if (cameraHolder == null) return;

        // Horizontal look  →  rotate the player body (yaw)
        float yaw = _lookInput.x * mouseSensitivity;
        transform.Rotate(Vector3.up, yaw);

        // Vertical look  →  rotate only the camera holder (pitch), clamped
        _cameraPitch -= _lookInput.y * mouseSensitivity;
        _cameraPitch = Mathf.Clamp(_cameraPitch, -maxLookAngle, maxLookAngle);
        cameraHolder.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
    }

    // Crouch logic: smoothly transition height and camera position
    private void HandleCrouch()
    {
        bool ceilingAbove = Physics.Raycast(transform.position + Vector3.up * _controller.height, 
                                Vector3.up, standingHeight - _controller.height + 0.1f, groundMask);

        bool shouldCrouch = _isCrouching || ceilingAbove;

        float targetHeight = shouldCrouch ? crouchHeight : standingHeight;
        float targetCameraY = shouldCrouch ? crouchCameraY : standingCameraY;

        // Smoothly transition height
        float newHeight = Mathf.Lerp(_controller.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);
        _controller.height = newHeight;

        // Anchor the controller to the ground by adjusting center
        // This must match however your CC is configured — if center was (0,1,0) at start, keep it relative
        _controller.center = Vector3.up * (newHeight / 2f);

        // Move camera holder independently
        Vector3 camPos = cameraHolder.localPosition;
        camPos.y = Mathf.Lerp(camPos.y, targetCameraY, Time.deltaTime * crouchTransitionSpeed);
        cameraHolder.localPosition = camPos;
    }

    // -----------------------------------------------------------------------
    // Public Utilities
    // -----------------------------------------------------------------------

    /// <summary>Unlock the cursor (call from a pause menu, etc.)</summary>
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>Re-lock the cursor.</summary>
    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}