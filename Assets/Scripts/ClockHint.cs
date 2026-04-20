using UnityEngine;

// À mettre sur chaque mur qui peut afficher un indice
// Nécessite un composant TextMesh sur le même objet ou un enfant
public class ClockHint : MonoBehaviour
{
    [SerializeField] private TextMesh textMesh; // texte 3D affiché sur le mur

    private void Awake()
    {
        // Cherche automatiquement le TextMesh si pas assigné
        if (textMesh == null)
            textMesh = GetComponentInChildren<TextMesh>();
    }

    // Affiche le chiffre des heures sur ce mur
    public void ShowHour(int hour)
    {
        gameObject.SetActive(true);
        if (textMesh != null)
            textMesh.text = hour.ToString();
    }

    // Affiche le chiffre des minutes sur ce mur
    public void ShowMinute(int minute)
    {
        gameObject.SetActive(true);
        if (textMesh != null)
            textMesh.text = minute.ToString("D2"); // ex: "05", "45"
    }

    // Cache le mur (pas utilisé cette partie)
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
