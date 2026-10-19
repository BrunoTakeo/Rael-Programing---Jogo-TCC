using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tela preta usada para escurecer/clarear a imagem nas transições (portas).
/// Não precisa ser colocada na cena: ela se cria sozinha na primeira vez que for usada.
/// </summary>
public class ScreenFader : MonoBehaviour
{
    private static ScreenFader instance;

    private Image image;

    public static ScreenFader Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("ScreenFader");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<ScreenFader>();
                instance.Build();
            }
            return instance;
        }
    }

    private void Build()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // acima de qualquer outra UI

        GameObject imageGO = new GameObject("Fade", typeof(RectTransform));
        imageGO.transform.SetParent(transform, false);

        image = imageGO.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = false;

        RectTransform rt = image.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>Muda a opacidade da tela preta até "targetAlpha" (0 = transparente, 1 = preto total).</summary>
    public IEnumerator Fade(float targetAlpha, float duration)
    {
        Color c = image.color;
        float startAlpha = c.a;

        if (duration <= 0f)
        {
            c.a = targetAlpha;
            image.color = c;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(startAlpha, targetAlpha, t / duration);
            image.color = c;
            yield return null;
        }

        c.a = targetAlpha;
        image.color = c;
    }
}
