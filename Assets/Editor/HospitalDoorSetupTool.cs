using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HospitalDoorSetupTool
{
    private const string OpenDoorClipPath = "Assets/audio/opendoor.mp3";
    private const string CloseDoorClipPath = "Assets/audio/closedoor.mp3";

    [MenuItem("Tools/Gameplay/Setup Interactive Doors")]
    public static void SetupInteractiveDoors()
    {
        var updatedDoors = new List<string>();

        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.isLoaded)
            {
                continue;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!HospitalDoorUtility.IsDoorLeaf(candidate))
                    {
                        continue;
                    }

                    bool changed = ConfigureDoor(candidate.gameObject);
                    if (changed)
                    {
                        updatedDoors.Add(candidate.name);
                    }
                }
            }

            if (updatedDoors.Count > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        string message = updatedDoors.Count == 0
            ? "Aucune nouvelle porte exploitable n'a ete trouvee."
            : $"Portes configurees: {updatedDoors.Count}";

        EditorUtility.DisplayDialog("Interactive Doors", message, "OK");
    }

    [MenuItem("Tools/Gameplay/Assign Door Sounds")]
    public static void AssignDoorSounds()
    {
        AudioClip openClip = AssetDatabase.LoadAssetAtPath<AudioClip>(OpenDoorClipPath);
        AudioClip closeClip = AssetDatabase.LoadAssetAtPath<AudioClip>(CloseDoorClipPath);

        if (openClip == null || closeClip == null)
        {
            EditorUtility.DisplayDialog(
                "Door Sounds",
                "Impossible de charger opendoor.mp3 ou closedoor.mp3 depuis Assets/audio.",
                "OK");
            return;
        }

        int assignedCount = 0;

        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.isLoaded)
            {
                continue;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (DoorInteractable door in root.GetComponentsInChildren<DoorInteractable>(true))
                {
                    door.AssignAudioClips(openClip, closeClip);
                    EditorUtility.SetDirty(door);
                    assignedCount++;
                }
            }

            if (assignedCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        EditorUtility.DisplayDialog(
            "Door Sounds",
            assignedCount == 0 ? "Aucune porte configuree n'a ete trouvee." : $"Sons assignes a {assignedCount} portes.",
            "OK");
    }

    private static bool ConfigureDoor(GameObject doorObject)
    {
        bool changed = false;
        AudioClip openClip = AssetDatabase.LoadAssetAtPath<AudioClip>(OpenDoorClipPath);
        AudioClip closeClip = AssetDatabase.LoadAssetAtPath<AudioClip>(CloseDoorClipPath);

        DoorInteractable doorInteractable = doorObject.GetComponent<DoorInteractable>();
        if (doorInteractable == null)
        {
            doorInteractable = Undo.AddComponent<DoorInteractable>(doorObject);
            changed = true;
        }

        AudioSource audioSource = doorObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = Undo.AddComponent<AudioSource>(doorObject);
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 1.5f;
            audioSource.maxDistance = 12f;
            changed = true;
        }

        if (doorInteractable != null && (openClip != null || closeClip != null))
        {
            doorInteractable.AssignAudioClips(openClip, closeClip);
            EditorUtility.SetDirty(doorInteractable);
            changed = true;
        }

        if (!HospitalDoorUtility.HasAnyCollider(doorObject))
        {
            BoxCollider collider = Undo.AddComponent<BoxCollider>(doorObject);
            if (HospitalDoorUtility.TryGetLocalBounds(doorObject, out Bounds bounds))
            {
                collider.center = bounds.center;
                collider.size = bounds.size;
            }

            changed = true;
        }

        ClearStaticFlagsRecursively(doorObject.transform);
        return changed;
    }

    private static void ClearStaticFlagsRecursively(Transform root)
    {
        foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
        {
            GameObjectUtility.SetStaticEditorFlags(item.gameObject, 0);
        }
    }
}
