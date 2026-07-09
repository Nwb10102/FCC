using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ImageStep : CutSceneStep
{
    [SerializeField] private Image image;
    [SerializeField] private Sprite sprite;
    [SerializeField] private bool fadeIn = true;
    [SerializeField] private float fadeDuration = 0.5f;

    public override IEnumerator Execute()
    {
        if (image == null) yield break;
        if (sprite != null) image.sprite = sprite;

        float startAlpha = fadeIn ? 0f : 1f;
        float endAlpha = fadeIn ? 1f : 0f;

        if (fadeIn) image.gameObject.SetActive(true);

        Color c = image.color;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, endAlpha, elapsed / fadeDuration);
            image.color = c;
            yield return null;
        }

        c.a = endAlpha;
        image.color = c;

        if (!fadeIn) image.gameObject.SetActive(false);
    }
}
