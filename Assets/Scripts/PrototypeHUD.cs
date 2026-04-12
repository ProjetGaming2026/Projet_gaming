using UnityEngine;

public class PrototypeHUD : MonoBehaviour
{
    [SerializeField] private PrototypeGameManager gameManager;

    private BasicFirstPersonController playerController;
    private BasicMonsterController monsterController;
    private Transform playerTransform;
    private Transform monsterTransform;

    private GUIStyle textStyle;
    private GUIStyle glitchStyle;
    private GUIStyle dangerStyle;
    private GUIStyle endStyle;

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<PrototypeGameManager>();
        }

        playerController = FindFirstObjectByType<BasicFirstPersonController>();
        monsterController = FindFirstObjectByType<BasicMonsterController>();

        if (playerController != null)
        {
            playerTransform = playerController.transform;
        }

        if (monsterController != null)
        {
            monsterTransform = monsterController.transform;
        }
    }

    private void OnGUI()
    {
        if (gameManager == null)
        {
            return;
        }

        InitializeStyles();

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
                hudText += "\nGlitch actif : PhaseCharge " + playerController.PhaseTimeRemaining.ToString("F1") + "s";
            }
            else if (playerController.IsOverclockActive)
            {
                hudText += "\nGlitch actif : Overclock " + playerController.OverclockTimeRemaining.ToString("F1") + "s";
            }
            else if (playerController.IsHiddenActive)
            {
                hudText += "\nGlitch actif : Hidden " + playerController.HiddenTimeRemaining.ToString("F1") + "s";
            }
            else if (playerController.HasPhaseCharge)
            {
                hudText += "\nGlitch en stock : PhaseCharge x" + playerController.PhaseChargeCount + " (F)";
            }
            else if (playerController.HasStaticTrap)
            {
                hudText += "\nGlitch en stock : StaticTrap x" + playerController.StaticTrapCount + " (G)";
            }
            else
            {
                hudText += "\nGlitch : Aucun";
            }
        }

        GUI.Box(new Rect(20, 20, 520, 220), "");
        GUI.Label(new Rect(35, 35, 480, 180), hudText, textStyle);

        if (playerController != null && playerController.LastReceivedGlitch != BasicFirstPersonController.GlitchType.None)
        {
            string lastGlitchText = "Glitch recu : " + playerController.LastReceivedGlitch;
            GUI.Label(new Rect(Screen.width / 2 - 180, 40, 360, 50), lastGlitchText, glitchStyle);
        }

        DrawDangerWarning();
        DrawEndMessage();
    }

    private void InitializeStyles()
    {
        if (textStyle == null)
        {
            textStyle = new GUIStyle(GUI.skin.label);
            textStyle.fontSize = 28;
            textStyle.normal.textColor = Color.white;
        }

        if (glitchStyle == null)
        {
            glitchStyle = new GUIStyle(GUI.skin.label);
            glitchStyle.fontSize = 32;
            glitchStyle.normal.textColor = Color.yellow;
            glitchStyle.alignment = TextAnchor.MiddleCenter;
        }

        if (dangerStyle == null)
        {
            dangerStyle = new GUIStyle(GUI.skin.label);
            dangerStyle.fontSize = 36;
            dangerStyle.alignment = TextAnchor.MiddleCenter;
        }

        if (endStyle == null)
        {
            endStyle = new GUIStyle(GUI.skin.label);
            endStyle.fontSize = 56;
            endStyle.fontStyle = FontStyle.Bold;
            endStyle.alignment = TextAnchor.MiddleCenter;
        }
    }

    private void DrawDangerWarning()
    {
        if (playerTransform == null || monsterTransform == null)
        {
            return;
        }

        float distance = Vector3.Distance(playerTransform.position, monsterTransform.position);

        if (distance <= 4f)
        {
            dangerStyle.normal.textColor = Color.red;
            GUI.Label(
                new Rect(Screen.width / 2 - 220, Screen.height - 120, 440, 50),
                "DANGER TRES PROCHE",
                dangerStyle
            );
        }
        else if (distance <= 8f)
        {
            dangerStyle.normal.textColor = Color.yellow;
            GUI.Label(
                new Rect(Screen.width / 2 - 180, Screen.height - 120, 360, 50),
                "Danger proche",
                dangerStyle
            );
        }
    }

    private void DrawEndMessage()
    {
        if (gameManager.CurrentPhase == PrototypeGameManager.GamePhase.Win)
        {
            endStyle.normal.textColor = Color.green;
            GUI.Label(
                new Rect(Screen.width / 2 - 250, Screen.height / 2 - 50, 500, 100),
                "VICTOIRE",
                endStyle
            );
        }
        else if (gameManager.CurrentPhase == PrototypeGameManager.GamePhase.Lose)
        {
            endStyle.normal.textColor = Color.red;
            GUI.Label(
                new Rect(Screen.width / 2 - 250, Screen.height / 2 - 50, 500, 100),
                "DEFAITE",
                endStyle
            );
        }
    }
}
