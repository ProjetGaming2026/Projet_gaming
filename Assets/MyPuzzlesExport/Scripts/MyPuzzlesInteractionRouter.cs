using System;
using UnityEngine;

public class MyPuzzlesInteractionRouter : MonoBehaviour
{
    private const string RouterObjectName = "MyPuzzlesInteractionRouter";

    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactDistance = 3.5f;
    [SerializeField] private float interactRadius = 0.22f;

    private UnityEngine.Object promptOwner;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        if (FindFirstObjectByType<MyPuzzlesInteractionRouter>() != null)
        {
            return;
        }

        GameObject router = new GameObject(RouterObjectName);
        router.AddComponent<MyPuzzlesInteractionRouter>();
        DontDestroyOnLoad(router);
    }

    private void Update()
    {
        if (KeypadUIManager.IsUiOpen || PatientUIManager.IsUiOpen || XRayUIManager.IsUiOpen)
        {
            HidePrompt();
            return;
        }

        PuzzleTarget target = FindPuzzleTarget();
        if (target.Component == null)
        {
            HidePrompt();
            return;
        }

        ShowPrompt(target.Prompt);

        if (Input.GetKeyDown(interactKey))
        {
            target.Interact();
        }
    }

    private PuzzleTarget FindPuzzleTarget()
    {
        Transform origin = Camera.main != null ? Camera.main.transform : null;
        if (origin == null)
        {
            return default;
        }

        RaycastHit[] hits = Physics.SphereCastAll(
            new Ray(origin.position, origin.forward),
            interactRadius,
            interactDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide);

        if (hits == null || hits.Length == 0)
        {
            return default;
        }

        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            PuzzleTarget target = ResolvePuzzleTarget(hits[i].collider);
            if (target.Component != null && target.IsUsable())
            {
                return target;
            }
        }

        return default;
    }

    private PuzzleTarget ResolvePuzzleTarget(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return default;
        }

        CodePuzzleManager keypad = FindComponentAroundCollider<CodePuzzleManager>(hitCollider);
        if (keypad != null)
        {
            return new PuzzleTarget(keypad, "Appuyer E pour entrer le code", keypad.Interact);
        }

        PatientFilesTableInteraction table = FindComponentAroundCollider<PatientFilesTableInteraction>(hitCollider);
        if (table != null)
        {
            return new PuzzleTarget(table, "Appuyer E pour lire les dossiers", table.Interact);
        }

        PatientFolder folder = FindComponentAroundCollider<PatientFolder>(hitCollider);
        if (folder != null)
        {
            return new PuzzleTarget(folder, "Appuyer E pour lire les dossiers", folder.Interact);
        }

        XRayItem xray = FindComponentAroundCollider<XRayItem>(hitCollider);
        if (xray != null)
        {
            return new PuzzleTarget(xray, "Appuyer E pour selectionner la radio", xray.Interact);
        }

        return default;
    }

    private T FindComponentAroundCollider<T>(Collider hitCollider) where T : Component
    {
        T component = hitCollider.GetComponent<T>();
        if (component != null)
        {
            return component;
        }

        component = hitCollider.GetComponentInParent<T>();
        if (component != null)
        {
            return component;
        }

        return hitCollider.GetComponentInChildren<T>();
    }

    private void ShowPrompt(string message)
    {
        promptOwner = this;
        KeypadUIManager.Instance?.ShowInteractionPrompt(message, promptOwner);
    }

    private void HidePrompt()
    {
        if (promptOwner == null)
        {
            return;
        }

        KeypadUIManager.Instance?.HideInteractionPrompt(promptOwner);
        promptOwner = null;
    }

    private readonly struct PuzzleTarget
    {
        public PuzzleTarget(Behaviour component, string prompt, Action interact)
        {
            Component = component;
            Prompt = prompt;
            Interact = interact;
        }

        public Behaviour Component { get; }
        public string Prompt { get; }
        public Action Interact { get; }

        public bool IsUsable()
        {
            return Component != null && Component.isActiveAndEnabled && Interact != null;
        }
    }
}
