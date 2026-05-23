using UnityEngine;
using UnityEngine.SceneManagement;

public static class ObjectiveWorldVisualBootstrap
{
    private static bool isRegistered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!isRegistered)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            isRegistered = true;
        }

        Apply(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Apply(scene);
    }

    private static void Apply(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        TaskTerminal[] terminals = Object.FindObjectsByType<TaskTerminal>(FindObjectsSortMode.None);
        for (int i = 0; i < terminals.Length; i++)
        {
            if (terminals[i] != null && terminals[i].gameObject.scene == scene)
            {
                HideTerminalVisual(terminals[i]);
            }
        }

        ExitZone[] exits = Object.FindObjectsByType<ExitZone>(FindObjectsSortMode.None);
        for (int i = 0; i < exits.Length; i++)
        {
            if (exits[i] != null && exits[i].gameObject.scene == scene)
            {
                HideExitVisual(exits[i]);
            }
        }
    }

    private static void HideTerminalVisual(TaskTerminal terminal)
    {
        DisableRenderers(terminal.transform);
    }

    private static void HideExitVisual(ExitZone exitZone)
    {
        Transform host = exitZone.VisualRoot != null ? exitZone.VisualRoot.transform : exitZone.transform;
        DisableRenderers(host);
    }

    private static void DisableRenderers(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = false;
        }
    }
}
