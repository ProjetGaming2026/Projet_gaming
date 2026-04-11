using UnityEngine;

public class EntityIncubationVisual : MonoBehaviour
{
    [SerializeField] private PrototypeGameManager gameManager;
    [SerializeField] private GameObject prisonVisual;

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<PrototypeGameManager>();
        }
    }

    private void Update()
    {
        if (gameManager == null || prisonVisual == null)
        {
            return;
        }

        bool showPrison = gameManager.CurrentPhase == PrototypeGameManager.GamePhase.Incubation;
        prisonVisual.SetActive(showPrison);
    }
}
