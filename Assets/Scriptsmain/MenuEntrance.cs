using UnityEngine;
using System.Collections;

public class MenuEntrance : MonoBehaviour
{
    public GameObject[] buttons;
    public float delay = 0.12f;
    public float duration = 0.5f;

    void Start()
    {
        foreach (var btn in buttons)
        {
            CanvasGroup cg = btn.GetComponent<CanvasGroup>();
            if (cg != null)
                cg.alpha = 0;
        }

        StartCoroutine(AnimateIn());
    }

    IEnumerator AnimateIn()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            yield return new WaitForSeconds(delay);
            StartCoroutine(FadeUp(buttons[i]));
        }
    }

    IEnumerator FadeUp(GameObject btn)
    {
        CanvasGroup cg = btn.GetComponent<CanvasGroup>();
        RectTransform rt = btn.GetComponent<RectTransform>();

        Vector2 start = rt.anchoredPosition - new Vector2(0, 20);
        Vector2 end = rt.anchoredPosition;

        float t = 0;

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            float ease = 1 - Mathf.Pow(1 - p, 3);

            if (cg != null)
                cg.alpha = ease;

            rt.anchoredPosition = Vector2.Lerp(start, end, ease);

            yield return null;
        }

        if (cg != null) cg.alpha = 1;
        rt.anchoredPosition = end;
    }
}