using UnityEngine;
using UnityEngine.SceneManagement;

public static class MonsterPresenceEffectsBootstrap
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

        BasicMonsterController monster = Object.FindFirstObjectByType<BasicMonsterController>();
        if (monster == null)
        {
            return;
        }

        if (monster.GetComponent<MonsterPresenceEffects>() == null)
        {
            monster.gameObject.AddComponent<MonsterPresenceEffects>();
        }
    }
}
