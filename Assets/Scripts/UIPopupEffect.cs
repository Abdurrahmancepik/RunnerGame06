using UnityEngine;

public class UIPopupEffect : MonoBehaviour
{
    public float duration = 0.5f;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private void OnEnable()
    {
        transform.localScale = Vector3.zero;
        StartCoroutine(AnimatePopup());
    }

    System.Collections.IEnumerator AnimatePopup()
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime; // Oyun dursa bile çalýþsýn (Time.timeScale = 0 olsa bile)
            float t = timer / duration;
            float scale = curve.Evaluate(t);

            transform.localScale = Vector3.one * scale;
            yield return null;
        }
        transform.localScale = Vector3.one;
    }
}
