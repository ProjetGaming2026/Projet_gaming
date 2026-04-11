using UnityEngine;

public class PrototypeHUD : MonoBehaviour
{
    [SerializeField] private PrototypeGameManager gameManager;

    private BasicFirstPersonController playerController;
    private GUIStyle textStyle;

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<PrototypeGameManager>();
        }

        playerController = FindFirstObjectByType<BasicFirstPersonController>();
    }

    private void OnGUI()
    {
        if (gameManager == null)
        {
            return;
        }

        if (textStyle == null)
        {
            textStyle = new GUIStyle(GUI.skin.label);
            textStyle.fontSize = 28;
            textStyle.normal.textColor = Color.white;
        }

        string hudText =
            "Phase : " + gameManager.CurrentPhase + "\n" +
            "Taches : " + gameManager.CompletedTasks + " / " + gameManager.RequiredTasks;

        if (gameManager.CurrentPhase == PrototypeGameManager.GamePhase.Escape &&
            gameManager.ActiveExitZone != null &&
            gameManager.ActiveExitZone.IsActive)
        {
            if (gameManager.ActiveExitZone.PlayerInside)
            {
                hudText += "\nExtraction : " + gameManager.ActiveExitZone.EscapeTimeRemaining.ToString("F1") + "s";
            }
            else
            {
                hudText += "\nExtraction : trouvez et tenez la sortie";
            }
        }

                if (playerController != null)
        {
            if (playerController.IsPhaseActive)
            {
                hudText += "\nGlitch : Phase ACTIVE " + playerController.PhaseTimeRemaining.ToString("F1") + "s";
            }
            else if (playerController.IsOverclockActive)
            {
                hudText += "\nGlitch : Overclock " + playerController.OverclockTimeRemaining.ToString("F1") + "s";
            }
            else if (playerController.IsHiddenActive)
            {
                hudText += "\nGlitch : Hidden " + playerController.HiddenTimeRemaining.ToString("F1") + "s";
            }
            else if (playerController.HasPhaseCharge)
            {
                hudText += "\nGlitch : PhaseCharge x" + playerController.PhaseChargeCount + " (F)";
            }
            else
            {
                hudText += "\nGlitch : Aucun";
            }
        }

        GUI.Box(new Rect(20, 20, 500, 210), "");
        GUI.Label(new Rect(35, 35, 460, 170), hudText, textStyle);
    }
}
