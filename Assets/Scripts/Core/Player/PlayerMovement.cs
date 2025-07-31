using UnityEngine;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Transform bodyTransform;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Animator animator;

    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 4f;
    [SerializeField, Tooltip("How quickly the player accelerates/decelerates.")]
    private float accelerationSmoothTime = 0.1f; // Adjustable smoothing factor

    // New: Expose the camera transform (could be the Cinemachine dolly's camera)
    [Header("Camera Settings")]
    [SerializeField] private Transform cameraTransform; // Set this in Inspector if using a camera spline dolly

    private Vector2 previousMovementInput;
    private Vector3 currentVelocity; // Used by SmoothDamp

    // Network sync fields
    private Vector3 networkVelocity = Vector3.zero;
    private Vector3 networkPosition = Vector3.zero;
    private Vector3 estimatedPosition = Vector3.zero;

    public Vector3 MovementDirection;
    public float MovementSpeed;

    public NetworkVariable<bool> IsMoving = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) { return; }
        
        // Ensure we have a camera transform: try assigned, else fallback to Camera.main.
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        inputReader.MoveEvent += HandleMove;
        IsMoving.Value = false;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) { return; }
        inputReader.MoveEvent -= HandleMove;
    }

    private void FixedUpdate()
    {
        animator.SetBool("IsMoving", IsMoving.Value);

        if (IsOwner)
        {
            CharacterMover();
            // SendPositionToServerRpc(rb.position, rb.linearVelocity); // Additional networking can be added here.
        }
        else
        {
            // ExtrapolateMovementFromPreviousData(); // For non-owners with interpolation.
        }
    }

    private void HandleMove(Vector2 movementInput)
    {
        previousMovementInput = movementInput;
    }

    private void CharacterMover()
    {
        Vector3 movementDirection;
        if (cameraTransform != null)
        {
            // Use camera's forward and right directions ignoring their vertical (y) part
            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = cameraTransform.right;
            camRight.y = 0f;
            camRight.Normalize();

            // Calculate movement direction relative to the camera orientation.
            movementDirection = (camForward * previousMovementInput.y + camRight * previousMovementInput.x).normalized;
        }
        else
        {
            // Fallback: use world space axes if cameraTransform is not available.
            movementDirection = new Vector3(previousMovementInput.x, 0f, previousMovementInput.y).normalized;
        }

        // Calculate desired velocity based on the movement direction
        Vector3 desiredVelocity = movementDirection * movementSpeed;

        // Smoothly interpolate velocity for smoother acceleration and deceleration
        Vector3 smoothedVelocity = Vector3.SmoothDamp(
            rb.linearVelocity,
            desiredVelocity,
            ref currentVelocity,
            accelerationSmoothTime
        );

        Vector3 velocityChange = smoothedVelocity - rb.linearVelocity;
        velocityChange.y = 0f; // Prevent vertical force changes

        rb.AddForce(velocityChange, ForceMode.VelocityChange);

        // Update movement parameters for animation and networking
        Vector3 horizontalVelocity = rb.linearVelocity;
        horizontalVelocity.y = 0f;
        MovementDirection = horizontalVelocity.sqrMagnitude > 0.001f ? horizontalVelocity.normalized : Vector3.zero;
        MovementSpeed = horizontalVelocity.magnitude;

        IsMoving.Value = MovementSpeed > 3f;
    }

    // ...existing networking and extrapolation code...
}