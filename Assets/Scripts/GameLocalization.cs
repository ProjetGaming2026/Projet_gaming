using UnityEngine;

public static class GameLocalization
{
    private const string LanguageKey = "Options.Language";

    public static int CurrentLanguageIndex => Mathf.Clamp(PlayerPrefs.GetInt(LanguageKey, 0), 0, 2);

    public static string Text(string english, string french, string spanish)
    {
        switch (CurrentLanguageIndex)
        {
            case 1:
                return french;
            case 2:
                return spanish;
            default:
                return english;
        }
    }

    public static string PhaseName(PrototypeGameManager.GamePhase phase)
    {
        switch (phase)
        {
            case PrototypeGameManager.GamePhase.Incubation:
                return Text("Incubation", "Incubation", "Incubacion");
            case PrototypeGameManager.GamePhase.Chase:
                return Text("Chase", "Chasse", "Persecucion");
            case PrototypeGameManager.GamePhase.Escape:
                return Text("Escape", "Sortie", "Escape");
            case PrototypeGameManager.GamePhase.Win:
                return Text("Win", "Victoire", "Victoria");
            case PrototypeGameManager.GamePhase.Lose:
                return Text("Lose", "Defaite", "Derrota");
            default:
                return phase.ToString();
        }
    }

    public static string PressE(string action)
    {
        return Text(
            "Press E to " + action,
            "Appuie sur E pour " + action,
            "Pulsa E para " + action
        );
    }
}
