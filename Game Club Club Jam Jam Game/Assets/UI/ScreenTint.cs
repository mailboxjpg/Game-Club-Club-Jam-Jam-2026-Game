using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenTint : MonoBehaviour
{
    [SerializeField] private Image tintImage;

    private Color originalColor;
    private Coroutine tintRoutine;

    private void Start()
    {
        originalColor = tintImage.color;
    }

    public void StartTint(Color tint, float fadeTime, float duration)
    {
        if (tintRoutine != null)
            StopCoroutine(tintRoutine);
        tintRoutine = StartCoroutine(ApplyTint(tint, fadeTime, duration));
    }

    private IEnumerator ApplyTint(Color tint, float fadeTime, float duration)
    {
        float t = 0f;
        while(t < fadeTime)
        {
            tintImage.color = Color.Lerp(originalColor, tint, t);
            t += Time.deltaTime;
            yield return null;
        }
        tintImage.color = tint;
        yield return new WaitForSeconds(duration);
        t = 0f;
        while(t < fadeTime)
        {
            tintImage.color = Color.Lerp(tint, originalColor, t);
            t += Time.deltaTime;
            yield return null;
        }
        tintImage.color = originalColor;
        tintRoutine = null;
    }
}
