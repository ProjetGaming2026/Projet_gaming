using UnityEngine;

public class StaticTrap : MonoBehaviour
{
    [SerializeField] private float slowDuration = 3f;
    [SerializeField] private float lifetime = 12f;

    [Header("Sons")]
    [SerializeField] private AudioClip placedSound;    // son quand le piège est posé
    [SerializeField] private AudioClip triggeredSound; // son quand le monstre marche dessus
    [SerializeField, Range(0f, 1f)] private float volume = 0.8f;

    private bool triggered;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f; // son 3D (plus fort si proche)
        audioSource.maxDistance = 20f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
    }

    private void Start()
    {
        // Son quand le piège est posé
        PlayClip(placedSound);
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        BasicMonsterController monster = other.GetComponent<BasicMonsterController>();
        if (monster != null)
        {
            triggered = true;
            monster.ApplyStaticTrap(slowDuration);

            // Son quand le monstre déclenche le piège
            PlayClip(triggeredSound);

            Debug.Log("L'Entite a declenche un piege statique.");
            Destroy(gameObject, 0.5f); // petit délai pour laisser le son jouer
        }
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.PlayOneShot(clip, volume);
    }
}
