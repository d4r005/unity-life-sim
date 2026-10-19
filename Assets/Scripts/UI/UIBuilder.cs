using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Construye toda la interfaz uGUI desde código: no hace falta armar
/// ninguna escena manualmente en el editor.
/// </summary>
public static class UIBuilder
{
    public static Font DefaultFont()
    {
        try { return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
        catch { return Resources.GetBuiltinResource<Font>("Arial.ttf"); }
    }

    public static Canvas CreateCanvas()
    {
        GameObject go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler s = go.GetComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(720f, 1280f);
        s.matchWidthOrHeight = 0.5f;
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }
        return c;
    }

    public static Image Panel(Transform parent, string name, Vector2 aMin, Vector2 aMax,
                              Vector2 oMin, Vector2 oMax, Color color)
    {
        GameObject go = new GameObject(name, typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = oMin; rt.offsetMax = oMax;
        go.GetComponent<Image>().color = color;
        return go;
    }

    public static Text MakeText(Transform parent, string name, string content, int size, Color color,
                                TextAnchor align, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        GameObject go = new GameObject(name, typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = oMin; rt.offsetMax = oMax;
        Text t = go.GetComponent<Text>();
        t.font = DefaultFont();
        t.text = content;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    public static Button MakeButton(Transform parent, string name, string label, int size, Color bg,
                                    Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, UnityAction onClick)
    {
        GameObject go = new GameObject(name, typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = oMin; rt.offsetMax = oMax;
        Image img = go.GetComponent<Image>();
        img.color = bg;
        Button b = go.GetComponent<Button>();
        if (onClick != null) b.onClick.AddListener(onClick);
        Text t = MakeText(go.transform, "Label", label, size, Color.white, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        t.raycastTarget = false;
        return b;
    }

    public static InputField MakeInput(Transform parent, string name, string placeholder,
                                      Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        GameObject go = new GameObject(name, typeof(Image), typeof(InputField));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = oMin; rt.offsetMax = oMax;
        go.GetComponent<Image>().color = new Color(0.16f, 0.16f, 0.19f, 1f);
        InputField inp = go.GetComponent<InputField>();
        GameObject txtGo = new GameObject("Text", typeof(Text));
        txtGo.transform.SetParent(go.transform, false);
        RectTransform trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(12f, 4f); trt.offsetMax = new Vector2(-12f, -4f);
        Text t = txtGo.GetComponent<Text>();
        t.font = DefaultFont(); t.fontSize = 30; t.color = Color.white;
        t.supportRichText = false;
        inp.textComponent = t;
        GameObject phGo = new GameObject("Placeholder", typeof(Text));
        phGo.transform.SetParent(go.transform, false);
        RectTransform prt = phGo.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
        prt.offsetMin = new Vector2(12f, 4f); prt.offsetMax = new Vector2(-12f, -4f);
        Text ph = phGo.GetComponent<Text>();
        ph.font = DefaultFont(); ph.fontSize = 30; ph.fontStyle = FontStyle.Italic;
        ph.color = new Color(1f, 1f, 1f, 0.4f); ph.text = placeholder;
        inp.placeholder = ph;
        return inp;
    }

    public static void Clear(Transform holder)
    {
        for (int i = holder.childCount - 1; i >= 0; i--) Object.Destroy(holder.GetChild(i).gameObject);
    }
}
