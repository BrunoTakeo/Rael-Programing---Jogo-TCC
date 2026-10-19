using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Funções auxiliares para montar a interface dos desafios por código.</summary>
public static class ChallengeUI
{
    public static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        return rt;
    }

    /// <summary>Estica o retângulo até as bordas do pai, deixando margens (em pixels).</summary>
    public static void Stretch(RectTransform rt, float left, float bottom, float right, float top)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    /// <summary>Posiciona um elemento num pai de tamanho fixo: x/y contados a partir do canto superior esquerdo.</summary>
    public static void Place(RectTransform rt, float parentW, float parentH, float x, float y, float width, float height)
    {
        Stretch(rt, x, parentH - y - height, parentW - x - width, y);
    }

    public static Text NewText(string name, Transform parent, int size, TextAnchor anchor, Color color, Font font)
    {
        RectTransform rt = NewRect(name, parent);
        Text text = rt.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = anchor;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.supportRichText = false; // evita que "<" e ">" do código virem tags
        text.raycastTarget = false;
        return text;
    }

    public static Button NewButton(string name, Transform parent, Color color, float height, int fontSize, TextAnchor anchor, Font font, out Text label)
    {
        RectTransform rt = NewRect(name, parent);

        Image image = rt.gameObject.AddComponent<Image>();
        image.color = color;

        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        Navigation nav = new Navigation();
        nav.mode = Navigation.Mode.None; // evita que Enter "clique" o botão selecionado
        button.navigation = nav;

        LayoutElement element = rt.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = height;

        label = NewText("Label", rt, fontSize, anchor, Color.white, font);
        Stretch(label.rectTransform, 10f, 4f, 10f, 4f);

        return button;
    }

    public static void ClearSelection()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }
}
