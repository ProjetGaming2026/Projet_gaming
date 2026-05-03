using UnityEngine;
using UnityEngine.SceneManagement;

public class HospitalDoorRuntimeBootstrap : MonoBehaviour
{
    private static bool isRegistered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapDoors()
    {
        if (!isRegistered)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            isRegistered = true;
        }

        ConfigureSceneDoors(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ConfigureSceneDoors(scene);
    }

    private static void ConfigureSceneDoors(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        AudioClip openClip = Resources.Load<AudioClip>("audio/opendoor");
        AudioClip closeClip = Resources.Load<AudioClip>("audio/closedoor");

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (!HospitalDoorUtility.IsDoorLeaf(candidate))
                {
                    continue;
                }

                EnsureDoorSetup(candidate.gameObject, openClip, closeClip);
            }
        }
    }

    private static void EnsureDoorSetup(GameObject doorObject, AudioClip openClip, AudioClip closeClip)
    {
        DoorInteractable doorInteractable = doorObject.GetComponent<DoorInteractable>();
        if (doorInteractable == null)
        {
            doorInteractable = doorObject.AddComponent<DoorInteractable>();
        }

        AudioSource audioSource = doorObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = doorObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 1.5f;
            audioSource.maxDistance = 12f;
        }

        if (openClip != null || closeClip != null)
        {
            doorInteractable.AssignAudioClips(openClip, closeClip);
        }

        if (!HospitalDoorUtility.HasAnyCollider(doorObject))
        {
            BoxCollider collider = doorObject.AddComponent<BoxCollider>();
            if (HospitalDoorUtility.TryGetLocalBounds(doorObject, out Bounds bounds))
            {
                collider.center = bounds.center;
                collider.size = bounds.size;
            }
        }
    }
}
