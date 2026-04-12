using UnityEngine;

public class StaticTrap : MonoBehaviour
{
    [SerializeField] private float slowDuration = 3f;
    [SerializeField] private float lifetime = 12f;

    private bool triggered;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered)
        {
            return;
        }

        BasicMonsterController monster = other.GetComponent<BasicMonsterController>();
        if (monster != null)
        {
            triggered = true;
            monster.ApplyStaticTrap(slowDuration);
            Debug.Log("L'Entite a declenche un piege statique.");
            Destroy(gameObject);
        }
    }
}
