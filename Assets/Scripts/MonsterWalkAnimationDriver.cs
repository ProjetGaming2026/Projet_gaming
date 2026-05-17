using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class MonsterWalkAnimationDriver : MonoBehaviour
{
    private const string IdleWithSkinPath = "Assets/Characters/Mixamo/Idle_with_skin.fbx";
    private const string WalkPath = "Assets/Characters/Mixamo/Zombie Walk.fbx";
    private const string RunPath = "Assets/Characters/Mixamo/Zombie Running.fbx";
    private const string TurnPath = "Assets/Characters/Mixamo/Zombie Turn.fbx";
    private const string AttackPath = "Assets/Characters/Mixamo/Zombie Attack.fbx";

    private enum ClipSlot
    {
        Idle = 0,
        Walk = 1,
        Run = 2,
        Turn = 3,
        Attack = 4
    }

    [Header("References")]
    [SerializeField] private BasicMonsterController monsterController;
    [SerializeField] private CharacterController movementController;
    [SerializeField] private Animator targetAnimator;
    [SerializeField] private Transform animatedModelRoot;
    [SerializeField] private Transform lookTarget;

    [Header("Mixamo Clips")]
    [SerializeField] private AnimationClip idleClip;
    [SerializeField] private AnimationClip walkClip;
    [SerializeField] private AnimationClip runClip;
    [SerializeField] private AnimationClip turnClip;
    [SerializeField] private AnimationClip attackClip;

    [Header("Locomotion")]
    [SerializeField] private float moveThreshold = 0.08f;
    [SerializeField] private float turnThresholdDegrees = 22f;
    [SerializeField] private float transitionSharpness = 5.5f;
    [SerializeField] private float roamReferenceSpeed = 1.8f;
    [SerializeField] private float huntReferenceSpeed = 6.8f;
    [SerializeField] private float walkSpeedMin = 0.82f;
    [SerializeField] private float walkSpeedMax = 1.16f;
    [SerializeField] private float runSpeedMin = 0.92f;
    [SerializeField] private float runSpeedMax = 1.24f;

    [Header("Presence")]
    [SerializeField] private float idleBreathAmplitude = 0.012f;
    [SerializeField] private float idleBreathFrequency = 1.2f;
    [SerializeField] private float walkBodyRoll = 2.8f;
    [SerializeField] private float runBodyRoll = 5.2f;
    [SerializeField] private float huntLeanAngle = 10f;
    [SerializeField] private float bodyMotionSmoothing = 8f;
    [SerializeField] private float headTrackingWeight = 0.42f;
    [SerializeField] private float investigateHeadTrackingWeight = 0.18f;
    [SerializeField] private float headTrackingSmoothing = 4.4f;

    public bool IsUsingWalkAnimation =>
        targetAnimator != null &&
        ((targetAnimator.runtimeAnimatorController != null && HasAnyPlayableClip()) ||
         (graphInitialized && HasAnyPlayableClip()));

    private static readonly int LocomotionSpeedHash = Animator.StringToHash("LocomotionSpeed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    private PlayableGraph playableGraph;
    private AnimationMixerPlayable mixerPlayable;
    private AnimationClipPlayable[] clipPlayables;
    private float[] currentWeights;
    private bool graphInitialized;
    private float currentPlaybackSpeed = 1f;
    private Quaternion previousRotation;
    private float forcedAttackTimer;
    private Vector3 modelBaseLocalPosition;
    private Quaternion modelBaseLocalRotation;
    private Transform spineBone;
    private Transform chestBone;
    private Transform headBone;

    private void Awake()
    {
        TryAutoAssignReferences();
        TryAutoAssignClips();
        TryInitializeGraph();
        CacheRigReferences();
        previousRotation = transform.rotation;
    }

    private void OnEnable()
    {
        TryAutoAssignReferences();
        TryAutoAssignClips();

        if (graphInitialized && playableGraph.IsValid())
        {
            playableGraph.Play();
        }
        else
        {
            TryInitializeGraph();
        }

        CacheRigReferences();
        previousRotation = transform.rotation;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        TryAutoAssignReferences();
        TryAutoAssignClips();
    }
#endif

    private void Update()
    {
        bool usingAnimatorController = targetAnimator != null && targetAnimator.runtimeAnimatorController != null;

        if (!usingAnimatorController && !graphInitialized)
        {
            TryAutoAssignReferences();
            TryAutoAssignClips();
            TryInitializeGraph();
            if (!graphInitialized)
            {
                return;
            }
        }

        bool isHunting = monsterController != null && monsterController.IsHuntingState;
        bool isInvestigating = monsterController != null && monsterController.IsInvestigatingState;
        bool isGuarding = monsterController != null && monsterController.IsGuardingExitState;
        float planarSpeed = monsterController != null
            ? monsterController.CurrentPlanarSpeed
            : (movementController != null ? new Vector3(movementController.velocity.x, 0f, movementController.velocity.z).magnitude : 0f);
        bool isMoving = monsterController != null
            ? monsterController.HasMovementIntent && planarSpeed > moveThreshold
            : planarSpeed > moveThreshold;

        float angularSpeed = Quaternion.Angle(previousRotation, transform.rotation) / Mathf.Max(Time.deltaTime, 0.0001f);
        previousRotation = transform.rotation;

        bool wantsTurn = !isMoving && angularSpeed > turnThresholdDegrees && (isInvestigating || isGuarding || isHunting);
        if (forcedAttackTimer > 0f)
        {
            forcedAttackTimer -= Time.deltaTime;
        }

        float[] targetWeights = new float[Enum.GetValues(typeof(ClipSlot)).Length];
        ClipSlot dominantSlot = ClipSlot.Idle;

        if (forcedAttackTimer > 0f && attackClip != null)
        {
            targetWeights[(int)ClipSlot.Attack] = 1f;
            dominantSlot = ClipSlot.Attack;
        }
        else if (isHunting && runClip != null && isMoving)
        {
            targetWeights[(int)ClipSlot.Run] = 1f;
            dominantSlot = ClipSlot.Run;
        }
        else if (isMoving && walkClip != null)
        {
            targetWeights[(int)ClipSlot.Walk] = 1f;
            dominantSlot = ClipSlot.Walk;
        }
        else if (wantsTurn && turnClip != null)
        {
            targetWeights[(int)ClipSlot.Turn] = 1f;
            dominantSlot = ClipSlot.Turn;
        }
        else
        {
            targetWeights[(int)ClipSlot.Idle] = 1f;
            dominantSlot = ClipSlot.Idle;
        }

        if (usingAnimatorController)
        {
            ApplyVirtualWeights(targetWeights);
            UpdateAnimatorController(dominantSlot, planarSpeed);
            return;
        }

        ApplyWeights(targetWeights);
        UpdatePlaybackSpeed(dominantSlot, planarSpeed);
    }

    private void LateUpdate()
    {
        if (!graphInitialized || animatedModelRoot == null)
        {
            return;
        }

        float idleWeight = GetCurrentWeight(ClipSlot.Idle);
        float walkWeight = GetCurrentWeight(ClipSlot.Walk);
        float runWeight = GetCurrentWeight(ClipSlot.Run);
        bool isHunting = monsterController != null && monsterController.IsHuntingState;
        bool isInvestigating = monsterController != null && monsterController.IsInvestigatingState;

        Vector3 targetPosition = modelBaseLocalPosition;
        Quaternion targetRotation = modelBaseLocalRotation;

        if (walkWeight > 0.02f || runWeight > 0.02f)
        {
            float phase = GetDominantClipPhase(runWeight > walkWeight ? ClipSlot.Run : ClipSlot.Walk);
            float swayWave = Mathf.Sin(phase * Mathf.PI * 2f);
            float bobWave = Mathf.Abs(Mathf.Sin(phase * Mathf.PI * 2f));
            float runFactor = Mathf.Clamp01(runWeight);
            float rollAmplitude = Mathf.Lerp(walkBodyRoll, runBodyRoll, runFactor);
            float lean = isHunting ? huntLeanAngle : Mathf.Lerp(1.5f, 3.8f, walkWeight + runWeight);

            targetPosition += new Vector3(swayWave * 0.03f, bobWave * 0.018f, 0f);
            targetRotation *= Quaternion.Euler(bobWave * lean, 0f, -swayWave * rollAmplitude);
        }
        else if (idleWeight > 0.02f)
        {
            float breath = Mathf.Sin(Time.unscaledTime * idleBreathFrequency * Mathf.PI * 2f);
            targetPosition += Vector3.up * (breath * idleBreathAmplitude);
            targetRotation *= Quaternion.Euler(breath * 1.2f, 0f, breath * 0.6f);
        }

        animatedModelRoot.localPosition = Vector3.Lerp(
            animatedModelRoot.localPosition,
            targetPosition,
            Time.deltaTime * bodyMotionSmoothing);
        animatedModelRoot.localRotation = Quaternion.Slerp(
            animatedModelRoot.localRotation,
            targetRotation,
            Time.deltaTime * bodyMotionSmoothing);

        UpdateLookBehaviour(isHunting, isInvestigating);
    }

    private void OnDisable()
    {
        if (graphInitialized && playableGraph.IsValid())
        {
            playableGraph.Stop();
        }
    }

    private void OnDestroy()
    {
        DestroyGraph();
    }

    public void ConfigureInEditor(
        BasicMonsterController controller,
        CharacterController characterController,
        Animator animator,
        AnimationClip walkFallbackClip,
        AnimationClip idle,
        AnimationClip walk,
        AnimationClip run,
        AnimationClip turn,
        AnimationClip attack)
    {
        monsterController = controller;
        movementController = characterController;
        targetAnimator = animator;
        animatedModelRoot = animator != null ? animator.transform : animatedModelRoot;
        lookTarget = ResolveLookTarget();

        idleClip = idle;
        walkClip = walk ?? walkFallbackClip;
        runClip = run;
        turnClip = turn;
        attackClip = attack;

        RebuildGraph();
        CacheRigReferences();
    }

    public void RefreshConfiguration()
    {
        TryAutoAssignReferences();
        TryAutoAssignClips();
        RebuildGraph();
        CacheRigReferences();
    }

    public float GetRecommendedAttackLeadTime()
    {
        if (attackClip == null)
        {
            return 0f;
        }

        return Mathf.Clamp(attackClip.length * 0.55f, 0.35f, 1.1f);
    }

    public void PlayCaptureAttack()
    {
        bool usingAnimatorController = targetAnimator != null && targetAnimator.runtimeAnimatorController != null;

        if (usingAnimatorController)
        {
            forcedAttackTimer = Mathf.Max(forcedAttackTimer, GetRecommendedAttackLeadTime());
            targetAnimator.SetTrigger(AttackHash);
            return;
        }

        if (attackClip == null || !graphInitialized)
        {
            return;
        }

        int index = (int)ClipSlot.Attack;
        if (clipPlayables == null || index >= clipPlayables.Length || !clipPlayables[index].IsValid())
        {
            return;
        }

        clipPlayables[index].SetTime(0d);
        clipPlayables[index].SetSpeed(1d);
        forcedAttackTimer = Mathf.Max(forcedAttackTimer, GetRecommendedAttackLeadTime());
    }

    private void TryAutoAssignReferences()
    {
        if (monsterController == null)
        {
            monsterController = GetComponent<BasicMonsterController>();
        }

        if (movementController == null)
        {
            movementController = GetComponent<CharacterController>();
        }

        if (targetAnimator == null)
        {
            targetAnimator = GetComponentsInChildren<Animator>(true)
                .FirstOrDefault(animator => animator.gameObject.name != "EntityPrisonVisual");
        }

        if (animatedModelRoot == null && targetAnimator != null)
        {
            animatedModelRoot = targetAnimator.transform;
        }

        if (lookTarget == null)
        {
            lookTarget = ResolveLookTarget();
        }
    }

    private void TryAutoAssignClips()
    {
#if UNITY_EDITOR
        if (idleClip == null)
        {
            idleClip = LoadClipFromAssetPathEditor(IdleWithSkinPath, "idle", "mixamo.com");
        }

        if (walkClip == null)
        {
            walkClip = LoadClipFromAssetPathEditor(WalkPath);
        }

        if (runClip == null)
        {
            runClip = LoadClipFromAssetPathEditor(RunPath);
        }

        if (turnClip == null)
        {
            turnClip = LoadClipFromAssetPathEditor(TurnPath);
        }

        if (attackClip == null)
        {
            attackClip = LoadClipFromAssetPathEditor(AttackPath);
        }
#endif

        AnimationClip[] clips = Resources.FindObjectsOfTypeAll<AnimationClip>()
            .Where(IsUsableClip)
            .ToArray();

        if (idleClip == null)
        {
            idleClip = FindClip(clips, "idle");
        }

        if (walkClip == null)
        {
            walkClip = FindClip(clips, "walking", "walk");
        }

        if (runClip == null)
        {
            runClip = FindClip(clips, "running", "run");
        }

        if (turnClip == null)
        {
            turnClip = FindClip(clips, "turning", "turn");
        }

        if (attackClip == null)
        {
            attackClip = FindClip(clips, "zombie attack", "attack");
        }
    }

    private bool HasAnyPlayableClip()
    {
        return idleClip != null || walkClip != null || runClip != null || turnClip != null || attackClip != null;
    }

    private void TryInitializeGraph()
    {
        if (graphInitialized || targetAnimator == null || !HasAnyPlayableClip() || targetAnimator.runtimeAnimatorController != null)
        {
            return;
        }

        targetAnimator.enabled = true;
        targetAnimator.applyRootMotion = false;
        targetAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        targetAnimator.Rebind();

        playableGraph = PlayableGraph.Create("MonsterAdvancedAnimation");
        mixerPlayable = AnimationMixerPlayable.Create(playableGraph, Enum.GetValues(typeof(ClipSlot)).Length);
        clipPlayables = new AnimationClipPlayable[Enum.GetValues(typeof(ClipSlot)).Length];
        currentWeights = new float[Enum.GetValues(typeof(ClipSlot)).Length];

        CreateClipPlayable(ClipSlot.Idle, idleClip);
        CreateClipPlayable(ClipSlot.Walk, walkClip);
        CreateClipPlayable(ClipSlot.Run, runClip);
        CreateClipPlayable(ClipSlot.Turn, turnClip);
        CreateClipPlayable(ClipSlot.Attack, attackClip, false);

        var output = AnimationPlayableOutput.Create(playableGraph, "MonsterAnimationOutput", targetAnimator);
        output.SetSourcePlayable(mixerPlayable);

        playableGraph.Play();
        graphInitialized = true;
    }

    private void CreateClipPlayable(ClipSlot slot, AnimationClip clip, bool loop = true)
    {
        int index = (int)slot;
        if (clip == null)
        {
            clipPlayables[index] = default;
            mixerPlayable.SetInputWeight(index, 0f);
            return;
        }

        AnimationClipPlayable playable = AnimationClipPlayable.Create(playableGraph, clip);
        playable.SetApplyFootIK(true);
        playable.SetTime(0d);
        playable.SetSpeed(1d);
        if (!loop)
        {
            playable.SetDuration(clip.length);
        }

        clipPlayables[index] = playable;
        playableGraph.Connect(playable, 0, mixerPlayable, index);
        mixerPlayable.SetInputWeight(index, 0f);
    }

    private void CacheRigReferences()
    {
        if (animatedModelRoot != null)
        {
            modelBaseLocalPosition = animatedModelRoot.localPosition;
            modelBaseLocalRotation = animatedModelRoot.localRotation;
        }

        if (targetAnimator == null || !targetAnimator.isHuman)
        {
            spineBone = null;
            chestBone = null;
            headBone = null;
            return;
        }

        spineBone = targetAnimator.GetBoneTransform(HumanBodyBones.Spine);
        chestBone = targetAnimator.GetBoneTransform(HumanBodyBones.Chest);
        headBone = targetAnimator.GetBoneTransform(HumanBodyBones.Head);
    }

    private void ApplyWeights(float[] targetWeights)
    {
        for (int i = 0; i < targetWeights.Length; i++)
        {
            float nextWeight = Mathf.MoveTowards(
                currentWeights[i],
                targetWeights[i],
                transitionSharpness * Time.deltaTime);
            currentWeights[i] = nextWeight;
            mixerPlayable.SetInputWeight(i, nextWeight);
        }
    }

    private void ApplyVirtualWeights(float[] targetWeights)
    {
        if (currentWeights == null || currentWeights.Length != targetWeights.Length)
        {
            currentWeights = new float[targetWeights.Length];
        }

        for (int i = 0; i < targetWeights.Length; i++)
        {
            currentWeights[i] = Mathf.MoveTowards(
                currentWeights[i],
                targetWeights[i],
                transitionSharpness * Time.deltaTime);
        }
    }

    private void UpdateAnimatorController(ClipSlot dominantSlot, float planarSpeed)
    {
        if (targetAnimator == null)
        {
            return;
        }

        float speedParameter = 0f;
        switch (dominantSlot)
        {
            case ClipSlot.Walk:
                speedParameter = Mathf.Lerp(0.85f, 1.2f, Mathf.Clamp01(planarSpeed / Mathf.Max(roamReferenceSpeed, 0.1f)));
                break;

            case ClipSlot.Run:
                speedParameter = Mathf.Lerp(1.55f, 2f, Mathf.Clamp01(planarSpeed / Mathf.Max(huntReferenceSpeed, 0.1f)));
                break;

            case ClipSlot.Turn:
                speedParameter = 0.2f;
                break;
        }

        targetAnimator.SetFloat(LocomotionSpeedHash, speedParameter);
    }

    private void UpdatePlaybackSpeed(ClipSlot dominantSlot, float planarSpeed)
    {
        float targetSpeed = 1f;
        switch (dominantSlot)
        {
            case ClipSlot.Walk:
                targetSpeed = Mathf.Lerp(
                    walkSpeedMin,
                    walkSpeedMax,
                    Mathf.Clamp01(planarSpeed / Mathf.Max(roamReferenceSpeed, 0.1f)));
                break;

            case ClipSlot.Run:
                targetSpeed = Mathf.Lerp(
                    runSpeedMin,
                    runSpeedMax,
                    Mathf.Clamp01(planarSpeed / Mathf.Max(huntReferenceSpeed, 0.1f)));
                break;

            case ClipSlot.Turn:
                targetSpeed = 0.95f;
                break;

            case ClipSlot.Attack:
                targetSpeed = 1f;
                break;
        }

        currentPlaybackSpeed = Mathf.MoveTowards(currentPlaybackSpeed, targetSpeed, transitionSharpness * Time.deltaTime);

        SetClipSpeed(ClipSlot.Idle, 1f);
        SetClipSpeed(ClipSlot.Walk, currentWeights[(int)ClipSlot.Walk] > 0.01f ? currentPlaybackSpeed : 1f);
        SetClipSpeed(ClipSlot.Run, currentWeights[(int)ClipSlot.Run] > 0.01f ? currentPlaybackSpeed : 1f);
        SetClipSpeed(ClipSlot.Turn, currentWeights[(int)ClipSlot.Turn] > 0.01f ? 0.92f : 1f);
        SetClipSpeed(ClipSlot.Attack, currentWeights[(int)ClipSlot.Attack] > 0.01f ? 1f : 0f);
    }

    private void SetClipSpeed(ClipSlot slot, float speed)
    {
        int index = (int)slot;
        if (!clipPlayables[index].IsValid())
        {
            return;
        }

        clipPlayables[index].SetSpeed(speed);
    }

    private float GetDominantClipPhase(ClipSlot slot)
    {
        int index = (int)slot;
        if (!graphInitialized || !clipPlayables[index].IsValid())
        {
            return 0f;
        }

        AnimationClip clip = GetClip(slot);
        if (clip == null || clip.length <= 0.01f)
        {
            return 0f;
        }

        return (float)(clipPlayables[index].GetTime() % clip.length / clip.length);
    }

    private float GetCurrentWeight(ClipSlot slot)
    {
        return currentWeights != null ? currentWeights[(int)slot] : 0f;
    }

    private AnimationClip GetClip(ClipSlot slot)
    {
        return slot switch
        {
            ClipSlot.Idle => idleClip,
            ClipSlot.Walk => walkClip,
            ClipSlot.Run => runClip,
            ClipSlot.Turn => turnClip,
            ClipSlot.Attack => attackClip,
            _ => null
        };
    }

    private Transform ResolveLookTarget()
    {
        return GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private void UpdateLookBehaviour(bool isHunting, bool isInvestigating)
    {
        if (lookTarget == null || headBone == null)
        {
            return;
        }

        float weight = isHunting ? headTrackingWeight : (isInvestigating ? investigateHeadTrackingWeight : 0f);
        if (weight <= 0.001f)
        {
            return;
        }

        Vector3 targetPoint = lookTarget.position + Vector3.up * 1.35f;
        Vector3 directionToTarget = targetPoint - headBone.position;
        if (directionToTarget.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetHeadRotation = Quaternion.LookRotation(directionToTarget.normalized, Vector3.up);
        headBone.rotation = Quaternion.Slerp(
            headBone.rotation,
            targetHeadRotation,
            Time.deltaTime * headTrackingSmoothing * weight);

        if (chestBone != null)
        {
            chestBone.rotation = Quaternion.Slerp(
                chestBone.rotation,
                targetHeadRotation,
                Time.deltaTime * (headTrackingSmoothing * 0.55f) * weight);
        }

        if (spineBone != null)
        {
            spineBone.rotation = Quaternion.Slerp(
                spineBone.rotation,
                targetHeadRotation,
                Time.deltaTime * (headTrackingSmoothing * 0.35f) * weight);
        }
    }

    private void RebuildGraph()
    {
        DestroyGraph();
        TryInitializeGraph();
    }

    private void DestroyGraph()
    {
        if (!graphInitialized)
        {
            return;
        }

        if (playableGraph.IsValid())
        {
            playableGraph.Destroy();
        }

        graphInitialized = false;
        clipPlayables = null;
        currentWeights = null;
        currentPlaybackSpeed = 1f;
    }

    private static bool IsUsableClip(AnimationClip clip)
    {
        return clip != null && !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase);
    }

    private static AnimationClip FindClip(AnimationClip[] clips, params string[] keywords)
    {
        foreach (string keyword in keywords)
        {
            AnimationClip exact = clips.FirstOrDefault(clip =>
                clip.name.Equals(keyword, StringComparison.OrdinalIgnoreCase));
            if (exact != null)
            {
                return exact;
            }

            AnimationClip partial = clips.FirstOrDefault(clip =>
                clip.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
            if (partial != null)
            {
                return partial;
            }
        }

        return null;
    }

#if UNITY_EDITOR
    private static AnimationClip LoadClipFromAssetPathEditor(string assetPath, params string[] preferredNames)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return null;
        }

        AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<AnimationClip>()
            .Where(IsUsableClip)
            .ToArray();

        foreach (string preferredName in preferredNames)
        {
            AnimationClip exact = clips.FirstOrDefault(clip =>
                clip.name.Equals(preferredName, StringComparison.OrdinalIgnoreCase));
            if (exact != null)
            {
                return exact;
            }

            AnimationClip partial = clips.FirstOrDefault(clip =>
                clip.name.IndexOf(preferredName, StringComparison.OrdinalIgnoreCase) >= 0);
            if (partial != null)
            {
                return partial;
            }
        }

        return clips.FirstOrDefault();
    }
#endif
}
