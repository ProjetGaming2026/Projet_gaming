using UnityEngine;

public class EntityIncubationVisual : MonoBehaviour
{
    [SerializeField] private PrototypeGameManager gameManager;
    [SerializeField] private GameObject prisonVisual;

    private const string GeneratedRootName = "GeneratedPrisonCage";

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<PrototypeGameManager>();
        }

        RemovePrisonVisuals();
    }

    private void Update()
    {
        if (prisonVisual == null)
        {
            return;
        }

        if (prisonVisual.activeSelf)
        {
            prisonVisual.SetActive(false);
        }
    }

    private void RemovePrisonVisuals()
    {
        if (prisonVisual == null)
        {
            return;
        }

        Transform generatedRoot = prisonVisual.transform.Find(GeneratedRootName);
        if (generatedRoot != null)
        {
            Destroy(generatedRoot.gameObject);
        }

        foreach (Renderer renderer in prisonVisual.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = false;
        }

        foreach (Collider collider in prisonVisual.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }

        prisonVisual.SetActive(false);
    }
}
