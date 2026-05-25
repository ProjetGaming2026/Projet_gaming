using UnityEngine;

public class PlayerHeldFlashlight : MonoBehaviour
{
    [SerializeField] private KeyCode toggleKey = KeyCode.F;
    [SerializeField] private Vector3 heldLocalPosition = new Vector3(0.26f, -0.24f, 0.5f);
    [SerializeField] private Vector3 heldLocalEulerAngles = new Vector3(2f, 0f, -7f);
    [SerializeField] private Vector3 heldLocalScale = Vector3.one * 0.14f;
    [SerializeField] private float spotIntensity = 7f;
    [SerializeField] private float spotRange = 13f;
    [SerializeField] private float spotAngle = 43f;
    [SerializeField] private float pickupHintDuration = 4f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip turnOnSound;
    [SerializeField] private AudioClip turnOffSound;
    [SerializeField] private float soundVolume = 0.8f;

    private Transform cameraTransform;
    private Transform beamTransform;
    private GameObject heldModel;
    private Light beamLight;
    private bool hasFlashlight;
    private bool isOn;
    private float pickupHintUntil;

    public bool HasFlashlight => hasFlashlight;
    public bool IsOn => isOn;
    public Transform BeamTransform => beamTransform != null ? beamTransform : cameraTransform;

    private void Awake()
    {
        ResolveCamera();
    }

    private void Update()
    {
        if (!hasFlashlight)
        {
            return;
        }

        if (Input.GetKeyDown(toggleKey))
        {
            SetLightActive(!isOn);
        }
    }

    private void LateUpdate()
    {
        if (!hasFlashlight || beamTransform == null || cameraTransform == null)
        {
            return;
        }

        beamTransform.position = cameraTransform.TransformPoint(new Vector3(0.08f, -0.08f, 0.32f));
        beamTransform.rotation = cameraTransform.rotation;
    }

    public void Equip(GameObject flashlightObject)
    {
        ResolveCamera();
        if (cameraTransform == null || flashlightObject == null)
        {
            Debug.LogWarning("PlayerHeldFlashlight: camera or flashlight model missing.");
            return;
        }

        heldModel = flashlightObject;
        heldModel.name = "Held_Old_Flashlight";
        heldModel.transform.SetParent(cameraTransform, false);
        heldModel.transform.localPosition = heldLocalPosition;
        heldModel.transform.localRotation = Quaternion.Euler(heldLocalEulerAngles);
        heldModel.transform.localScale = heldLocalScale;
        heldModel.SetActive(true);

        Collider[] colliders = heldModel.GetComponentsInChildren<Collider>(true);
        foreach (Collider heldCollider in colliders)
        {
            heldCollider.enabled = false;
        }

        EnsureBeamLight();
        EnsureAudioSource();
        hasFlashlight = true;
        pickupHintUntil = Time.time + pickupHintDuration;
        SetLightActive(false, false);
        Debug.Log("PlayerHeldFlashlight: old flashlight equipped. Press F to toggle it on or off.");
    }

    private void OnGUI()
    {
        if (!hasFlashlight || Time.time > pickupHintUntil)
        {
            return;
        }

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            fontStyle = FontStyle.Bold
        };

        GUI.color = new Color(1f, 1f, 1f, 0.9f);
        Rect rect = new Rect(0f, Screen.height - 120f, Screen.width, 36f);
        GUI.Label(rect, "F : allumer / eteindre la lampe", style);
    }

    private void ResolveCamera()
    {
        if (cameraTransform != null)
        {
            return;
        }

        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
            return;
        }

        Camera fallbackCamera = GetComponentInChildren<Camera>(true);
        if (fallbackCamera != null)
        {
            cameraTransform = fallbackCamera.transform;
        }
    }

    private void EnsureBeamLight()
    {
        if (beamLight != null)
        {
            return;
        }

        GameObject beamObject = new GameObject("Old_Flashlight_Beam");
        beamObject.transform.SetParent(cameraTransform, false);
        beamObject.transform.localPosition = new Vector3(0.08f, -0.08f, 0.35f);
        beamObject.transform.localRotation = Quaternion.identity;
        beamTransform = beamObject.transform;

        beamLight = beamObject.AddComponent<Light>();
        beamLight.type = LightType.Spot;
        beamLight.intensity = spotIntensity;
        beamLight.range = spotRange;
        beamLight.spotAngle = spotAngle;
        beamLight.innerSpotAngle = Mathf.Max(spotAngle * 0.55f, 1f);
        beamLight.shadows = LightShadows.Soft;
        beamLight.color = new Color(1f, 0.94f, 0.78f, 1f);
        beamLight.enabled = false;
    }

    private void SetLightActive(bool active, bool playSound = true)
    {
        if (isOn == active && beamLight != null && beamLight.enabled == active)
        {
            return;
        }

        isOn = active;
        if (beamLight != null)
        {
            beamLight.enabled = active;
        }

        if (playSound)
        {
            PlaySound(active ? turnOnSound : turnOffSound, active ? RuntimeInteractionSounds.FlashlightOn : RuntimeInteractionSounds.FlashlightOff);
        }
    }

    private void PlaySound(AudioClip assignedClip, AudioClip fallbackClip)
    {
        EnsureAudioSource();
        AudioClip clip = assignedClip != null ? assignedClip : fallbackClip;
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, soundVolume);
        }
    }

    private void EnsureAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }
}
