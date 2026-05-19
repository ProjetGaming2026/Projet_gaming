using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class BasicFirstPersonController : MonoBehaviour
{
    public enum GlitchType
    {
        None,
        Overclock,
        Hidden,
        PhaseCharge,
        StaticTrap
    }

    [Header("References")]
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private GameObject staticTrapPrefab;

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

    [Header("Glitch Bonus")]
    [SerializeField] private float overclockSpeed = 12f;
    [SerializeField] private KeyCode phaseKey = KeyCode.F;
    [SerializeField] private float phaseDuration = 0.75f;
    [SerializeField] private KeyCode trapKey = KeyCode.G;

    public bool IsOverclockActive => overclockTimer > 0f;
    public bool IsHiddenActive => hiddenTimer > 0f;
    public bool HasPhaseCharge => phaseCharges > 0;
    public bool IsPhaseActive => phaseTimer > 0f;
    public bool HasStaticTrap => staticTrapCharges > 0;
    public float OverclockTimeRemaining => Mathf.Max(0f, overclockTimer);
    public float HiddenTimeRemaining => Mathf.Max(0f, hiddenTimer);
    public float PhaseTimeRemaining => Mathf.Max(0f, phaseTimer);
    public int PhaseChargeCount => phaseCharges;
    public int StaticTrapCount => staticTrapCharges;
    public Vector3 PlanarVelocity => new Vector3(horizontalVelocity.x, 0f, horizontalVelocity.z);
    public float CurrentPlanarSpeed => PlanarVelocity.magnitude;
    public bool IsSprinting { get; private set; }
    public GlitchType LastReceivedGlitch { get; private set; } = GlitchType.None;

    private CharacterController controller;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float yaw;
    private float pitch;
    private float currentPitch;
    private float pitchSmoothVelocity;
    private float overclockTimer;
    private float hiddenTimer;
    private float phaseTimer;
    private int phaseCharges;
    private int staticTrapCharges;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraHolder == null && Camera.main != null)
        {
            cameraHolder = Camera.main.transform;
        }
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
        if (LevelSelectBootstrap.IsMenuOpen || KeypadUIManager.IsUiOpen || PatientUIManager.IsUiOpen || XRayUIManager.IsUiOpen)
        {
            IsSprinting = false;
            ApplyCursorState();
            return;
        }

        ApplyCursorState();
        UpdateTimers();
        HandleGlitchInput();
        HandleLook();
        HandleMovement();
    }

    public void GiveRandomGlitch(float overclockDuration, float hiddenDuration)
    {
        int randomIndex = Random.Range(0, 4);

        switch (randomIndex)
        {
            case 0:
                ActivateOverclock(overclockDuration);
                LastReceivedGlitch = GlitchType.Overclock;
                Debug.Log("Glitch obtenu : Overclock");
                break;

            case 1:
                ActivateHidden(hiddenDuration);
                LastReceivedGlitch = GlitchType.Hidden;
                Debug.Log("Glitch obtenu : Hidden");
                break;

            case 2:
                AddPhaseCharge(1);
                LastReceivedGlitch = GlitchType.PhaseCharge;
                Debug.Log("Glitch obtenu : PhaseCharge");
                break;

            case 3:
                AddStaticTrap(1);
                LastReceivedGlitch = GlitchType.StaticTrap;
                Debug.Log("Glitch obtenu : StaticTrap");
                break;
        }
    }

    public void ActivateOverclock(float duration)
    {
        overclockTimer = Mathf.Max(overclockTimer, duration);
        Debug.Log("Overclock actif pendant " + duration + " secondes.");
    }

    public void ActivateHidden(float duration)
    {
        hiddenTimer = Mathf.Max(hiddenTimer, duration);
        Debug.Log("Hidden actif pendant " + duration + " secondes.");
    }

    public void AddPhaseCharge(int amount)
    {
        phaseCharges += amount;
        Debug.Log("Charge de Phase recue. Total : " + phaseCharges);
    }

    public void AddStaticTrap(int amount)
    {
        staticTrapCharges += amount;
        Debug.Log("Piege statique recu. Total : " + staticTrapCharges);
    }

    public bool ConsumePhaseCharge()
    {
        if (phaseCharges <= 0)
        {
            return false;
        }

        phaseCharges--;
        return true;
    }

    public bool ConsumeStaticTrap()
    {
        if (staticTrapCharges <= 0)
        {
            return false;
        }

        staticTrapCharges--;
        return true;
    }

    private void UpdateTimers()
    {
        if (overclockTimer > 0f)
        {
            overclockTimer -= Time.deltaTime;
        }

        if (hiddenTimer > 0f)
        {
            hiddenTimer -= Time.deltaTime;
        }

        if (phaseTimer > 0f)
        {
            phaseTimer -= Time.deltaTime;

            if (phaseTimer <= 0f)
            {
                EndPhaseMode();
            }
        }
    }

    private void HandleGlitchInput()
    {
        if (Input.GetKeyDown(phaseKey) && HasPhaseCharge && !IsPhaseActive)
        {
            StartPhaseMode();
        }

        if (Input.GetKeyDown(trapKey) && HasStaticTrap)
        {
            PlaceStaticTrap();
        }
    }

    private void StartPhaseMode()
    {
        if (!ConsumePhaseCharge())
        {
            return;
        }

        phaseTimer = phaseDuration;
        controller.detectCollisions = false;
        Debug.Log("PhaseCharge utilise. Traversee temporaire active.");
    }

    private void EndPhaseMode()
    {
        phaseTimer = 0f;
        controller.detectCollisions = true;
        Debug.Log("PhaseCharge termine.");
    }

    private void PlaceStaticTrap()
    {
        if (!ConsumeStaticTrap())
        {
            return;
        }

        if (staticTrapPrefab == null)
        {
            Debug.LogWarning("Aucun prefab de piege statique assigne.");
            return;
        }

        Vector3 spawnPosition = transform.position + transform.forward * 1.2f;
        spawnPosition.y = 0.15f;

        Instantiate(staticTrapPrefab, spawnPosition, Quaternion.identity);
        Debug.Log("Piege statique pose.");
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
        bool isGrounded = controller.isGrounded || IsPhaseActive;

        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");
        Vector3 inputDirection = new Vector3(inputX, 0f, inputZ).normalized;
        bool wantsSprint = inputDirection.sqrMagnitude > 0.001f && Input.GetKey(KeyCode.LeftShift) && !IsOverclockActive;
        IsSprinting = wantsSprint;

        float targetSpeed = IsOverclockActive
            ? overclockSpeed
            : (wantsSprint ? sprintSpeed : walkSpeed);

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
        controller.detectCollisions = true;
    }

    private void ApplyCursorState()
    {
        if (LevelSelectBootstrap.IsMenuOpen || KeypadUIManager.IsUiOpen || PatientUIManager.IsUiOpen || XRayUIManager.IsUiOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
