using UnityEngine;

public class ExitZone : MonoBehaviour
{
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private Collider triggerZone;
    [SerializeField] private float escapeDuration = 10f;

    public bool IsActive { get; private set; }
    public bool PlayerInside => playerInside;
    public float EscapeProgress => escapeDuration <= 0f ? 0f : escapeTimer / escapeDuration;
    public float EscapeTimeRemaining => Mathf.Max(0f, escapeDuration - escapeTimer);
    public GameObject VisualRoot => visualRoot;
    public Collider TriggerZone => triggerZone;

    private PrototypeGameManager gameManager;
    private float escapeTimer;
    private float logTimer;
    private bool playerInside;

    private void Awake()
    {
        RefreshRuntimeReferences();
        SetActiveState(false);
    }

    private void Update()
    {
        if (!IsActive || !playerInside || gameManager == null)
        {
            return;
        }

        if (gameManager.CurrentPhase != PrototypeGameManager.GamePhase.Escape)
        {
            return;
        }

        escapeTimer += Time.deltaTime;
        logTimer += Time.deltaTime;

        if (logTimer >= 1f)
        {
            logTimer = 0f;
            Debug.Log("Extraction en cours : " + escapeTimer.ToString("F1") + " / " + escapeDuration + " secondes");
        }

        if (escapeTimer >= escapeDuration)
        {
            Debug.Log("Extraction terminee. Le joueur s'echappe.");
            gameManager.TriggerWin();
        }
    }

    public void ActivateExit()
    {
        escapeTimer = 0f;
        logTimer = 0f;
        playerInside = false;
        SetActiveState(true);
        Debug.Log("La sortie est maintenant active.");
    }

    public void DeactivateExit()
    {
        escapeTimer = 0f;
        logTimer = 0f;
        playerInside = false;
        SetActiveState(false);
    }

    public void RefreshRuntimeReferences()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<PrototypeGameManager>();
        }

        if (visualRoot == null)
        {
            visualRoot = gameObject;
        }

        if (triggerZone == null)
        {
            triggerZone = GetComponent<Collider>();

            if (triggerZone == null)
            {
                triggerZone = GetComponentInChildren<Collider>(true);
            }
        }
    }

    public void AssignVisualRoot(GameObject newVisualRoot)
    {
        if (newVisualRoot != null)
        {
            visualRoot = newVisualRoot;
        }
    }

    public void AssignTriggerZone(Collider newTriggerZone)
    {
        if (newTriggerZone != null)
        {
            triggerZone = newTriggerZone;
        }
    }

    private void SetActiveState(bool activeState)
    {
        IsActive = activeState;

        if (visualRoot != null)
        {
            visualRoot.SetActive(activeState);
        }

        if (triggerZone != null)
        {
            triggerZone.enabled = activeState;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsActive)
        {
            return;
        }

        if (other.CompareTag("Player"))
        {
            playerInside = true;
            escapeTimer = 0f;
            logTimer = 0f;
            Debug.Log("Extraction commencee. Restez dans la zone.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsActive)
        {
            return;
        }

        if (other.CompareTag("Player"))
        {
            playerInside = false;
            escapeTimer = 0f;
            logTimer = 0f;
            Debug.Log("Extraction annulee. Le joueur a quitte la zone.");
        }
    }
}
