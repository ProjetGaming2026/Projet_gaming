using UnityEngine;
using UnityEngine.Events;

public class MonsterActivationController : MonoBehaviour
{
    [SerializeField] private GameObject monsterEntity;
    [SerializeField] private Behaviour[] behavioursToEnable;
    [SerializeField] private bool activateGameObject = true;

    public UnityEvent OnMonsterTriggered = new UnityEvent();

    private bool hasTriggered;

    public void TriggerMonster()
    {
        if (hasTriggered)
        {
            return;
        }

        hasTriggered = true;

        if (monsterEntity == null)
        {
            monsterEntity = GameObject.Find("Monster_Entity");
        }

        if (monsterEntity != null && activateGameObject)
        {
            monsterEntity.SetActive(true);
        }
        else if (monsterEntity == null)
        {
            Debug.LogWarning("MonsterActivationController could not find Monster_Entity. Assign it in the Inspector if needed.");
        }

        if ((behavioursToEnable == null || behavioursToEnable.Length == 0) && monsterEntity != null)
        {
            behavioursToEnable = monsterEntity.GetComponentsInChildren<Behaviour>(true);
        }

        if (behavioursToEnable != null)
        {
            for (int i = 0; i < behavioursToEnable.Length; i++)
            {
                if (behavioursToEnable[i] != null)
                {
                    behavioursToEnable[i].enabled = true;
                }
            }
        }

        PuzzleFailureMonsterAlert.Alert(monsterEntity, transform);
        OnMonsterTriggered.Invoke();
        Debug.Log("Monster activation triggered and sent to the player's last known position.");
    }
}
