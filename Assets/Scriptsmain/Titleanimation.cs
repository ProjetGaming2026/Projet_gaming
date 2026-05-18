using UnityEngine;
using TMPro;

public class TitlePulse : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public float minAlpha = 0.4f;
    public float speed = 1.5f;

    void Update()
    {
        float alpha = Mathf.Lerp(minAlpha, 1f,
            (Mathf.Sin(Time.time * speed) + 1f) / 2f);
        titleText.color = new Color(
            titleText.color.r,
            titleText.color.g,
            titleText.color.b,
            alpha);
    }
}