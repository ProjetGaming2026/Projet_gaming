using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class BasicFirstPersonController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraHolder;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float jumpHeight = 1.2f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 2.2f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    [Header("Smoothing")]
    [SerializeField] private float cameraSmoothTime = 0.05f;

    public Vector3 PlanarVelocity => new Vector3(horizontalVelocity.x, 0f, horizontalVelocity.z);
    public float CurrentPlanarSpeed => PlanarVelocity.magnitude;
    public bool IsSprinting { get; private set; }
    public bool IsControlLocked { get; private set; }

    private CharacterController controller;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float yaw;
    private float pitch;
    private float currentPitch;
    private float pitchSmoothVelocity;
    private Vector3 defaultCameraLocalPosition;
    private Quaternion defaultCameraLocalRotation;
    private bool hasDefaultCameraPose;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraHolder == null && Camera.main != null)
        {
            cameraHolder = Camera.main.transform;
        }

        CacheDefaultCameraPose();
    }

    private void Start()
    {
        Vector3 rotation = transform.eulerAngles;
        yaw = rotation.y;
        pitch = cameraHolder != null ? cameraHolder.localEulerAngles.x : 0f;

        if (pitch > 180f)
        {
            pitch -= 360f;
        }

        currentPitch = pitch;
        ApplyCursorState();
    }

    private void Update()
    {
        if (IsControlLocked)
        {
            IsSprinting = false;
            horizontalVelocity = Vector3.zero;
            ApplyCursorState();
            return;
        }

        if (LevelSelectBootstrap.IsMenuOpen || GameplayPauseOptionsMenu.IsOpen)
        {
            IsSprinting = false;
            ApplyCursorState();
            return;
        }

        if (CipherTerminal.IsAnyTerminalOpen)
        {
            IsSprinting = false;
            return;
        }

        ApplyCursorState();
        HandleLook();
        HandleMovement();
    }

    public void ApplyMovementTuning(float newWalkSpeed, float newSprintSpeed)
    {
        walkSpeed = Mathf.Max(0.1f, newWalkSpeed);
        sprintSpeed = Mathf.Max(walkSpeed, newSprintSpeed);
    }

    public void SetExternalControlLock(bool isLocked)
    {
        IsControlLocked = isLocked;
        IsSprinting = false;

        if (isLocked)
        {
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
        }
    }

    public void ForceLookAt(Vector3 worldPoint)
    {
        Vector3 lookOrigin = cameraHolder != null ? cameraHolder.position : transform.position;
        Vector3 direction = worldPoint - lookOrigin;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        direction.Normalize();
        yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        pitch = -Mathf.Asin(Mathf.Clamp(direction.y, -1f, 1f)) * Mathf.Rad2Deg;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        currentPitch = pitch;
        pitchSmoothVelocity = 0f;

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (cameraHolder != null)
        {
            cameraHolder.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
        }
    }

    public void ForceViewPose(Vector3 worldPosition, float viewYaw, float viewPitch)
    {
        bool hadController = controller != null;

        if (hadController)
        {
            controller.enabled = false;
        }

        transform.position = worldPosition;
        yaw = viewYaw;
        pitch = Mathf.Clamp(viewPitch, minPitch, maxPitch);
        currentPitch = pitch;
        pitchSmoothVelocity = 0f;
        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (cameraHolder != null)
        {
            cameraHolder.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
        }

        if (hadController)
        {
            controller.enabled = true;
        }
    }

    public void ForceCameraWorldPose(Vector3 worldPosition, Vector3 worldEulerAngles)
    {
        if (cameraHolder == null)
        {
            return;
        }

        CacheDefaultCameraPose();

        yaw = worldEulerAngles.y;
        pitch = Mathf.Clamp(NormalizeAngle(worldEulerAngles.x), minPitch, maxPitch);
        currentPitch = pitch;
        pitchSmoothVelocity = 0f;
        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        cameraHolder.SetPositionAndRotation(worldPosition, Quaternion.Euler(pitch, yaw, worldEulerAngles.z));
    }

    public void ForceCameraLookAtWorldPose(Vector3 worldPosition, Vector3 worldLookTarget, float roll = 0f)
    {
        if (cameraHolder == null)
        {
            return;
        }

        Vector3 direction = worldLookTarget - worldPosition;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        direction.Normalize();
        float targetYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        float targetPitch = -Mathf.Asin(Mathf.Clamp(direction.y, -1f, 1f)) * Mathf.Rad2Deg;

        ForceCameraWorldPose(worldPosition, new Vector3(targetPitch, targetYaw, roll));
    }

    public void ForceCameraLocalPose(Vector3 localPosition, Vector3 localEulerAngles)
    {
        if (cameraHolder == null)
        {
            return;
        }

        CacheDefaultCameraPose();

        pitch = Mathf.Clamp(NormalizeAngle(localEulerAngles.x), minPitch, maxPitch);
        currentPitch = pitch;
        pitchSmoothVelocity = 0f;
        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;

        cameraHolder.localPosition = localPosition;
        cameraHolder.localRotation = Quaternion.Euler(pitch, localEulerAngles.y, localEulerAngles.z);
    }

    public void RestoreDefaultCameraLocalPose()
    {
        if (cameraHolder == null || !hasDefaultCameraPose)
        {
            return;
        }

        cameraHolder.localPosition = defaultCameraLocalPosition;
        cameraHolder.localRotation = defaultCameraLocalRotation;
        pitch = NormalizeAngle(cameraHolder.localEulerAngles.x);
        currentPitch = pitch;
        pitchSmoothVelocity = 0f;
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (cameraHolder == null)
        {
            return;
        }

        currentPitch = Mathf.SmoothDamp(currentPitch, pitch, ref pitchSmoothVelocity, cameraSmoothTime);
        cameraHolder.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
    }

    private void HandleMovement()
    {
        bool isGrounded = controller.isGrounded;

        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");
        Vector3 inputDirection = new Vector3(inputX, 0f, inputZ).normalized;
        bool wantsSprint = inputDirection.sqrMagnitude > 0.001f && Input.GetKey(KeyCode.LeftShift);
        IsSprinting = wantsSprint;

        float targetSpeed = wantsSprint ? sprintSpeed : walkSpeed;
        Vector3 targetVelocity = transform.TransformDirection(inputDirection) * targetSpeed;

        horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetVelocity, acceleration * Time.deltaTime);

        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 finalVelocity = horizontalVelocity;
        finalVelocity.y = verticalVelocity;

        controller.Move(finalVelocity * Time.deltaTime);
    }

    private void OnDisable()
    {
        IsSprinting = false;
    }

    private void CacheDefaultCameraPose()
    {
        if (cameraHolder == null || hasDefaultCameraPose)
        {
            return;
        }

        defaultCameraLocalPosition = cameraHolder.localPosition;
        defaultCameraLocalRotation = cameraHolder.localRotation;
        hasDefaultCameraPose = true;
    }

    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f)
        {
            angle -= 360f;
        }

        if (angle < -180f)
        {
            angle += 360f;
        }

        return angle;
    }

    private void ApplyCursorState()
    {
        if (IsControlLocked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (LevelSelectBootstrap.IsMenuOpen || GameplayPauseOptionsMenu.IsOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
