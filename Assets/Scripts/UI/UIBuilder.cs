using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Construye toda la interfaz uGUI desde código, con esquinas redondeadas,
/// avatares procedurales y barras de progreso. No hace falta armar escenas.
/// </summary>
public static class UIBuilder
{
    private static Font _font;
    public static Font DefaultFont()
    {
        if (_font == null)
        {
            try { _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { _font = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
        }
        return _font;
    }

    // ------------------------------------------------------------ sprites procedurales

    private static Sprite _rounded;
    /// <summary>Rectángulo blanco con esquinas redondeadas (9-slice), para teñir con Image.color.</summary>
    public static Sprite Rounded()
    {
        if (_rounded != null) return _rounded;
        const int size = 96;
        const int r = 22;
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int cx = Math.Min(x, size - 1 - x);
                int cy = Math.Min(y, size - 1 - y);
                bool inside;
                if (cx >= r || cy >= r) inside = true;
                else
                {
                    int dx = cx - r, dy = cy - r;
                    inside = dx * dx + dy * dy <= r * r;
                }
                px[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            }
        }
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.SetPixels32(px);
        tex.Apply();
        _rounded = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
        return _rounded;
    }

    private static Color32 Pick(System.Random r, int[][] palettes)
    {
        int[] p = palettes[r.Next(palettes.Length)];
        return new Color32((byte)p[0], (byte)p[1], (byte)p[2], 255);
    }

    /// <summary>Retrato procedural determinista a partir de una semilla.</summary>
    public static Sprite Avatar(int seed)
    {
        var r = new System.Random(seed);
        int[][] skins = { new[] { 244, 204, 171 }, new[] { 226, 181, 141 }, new[] { 186, 137, 98 }, new[] { 133, 87, 60 }, new[] { 94, 60, 42 } };
        int[][] hairs = { new[] { 32, 24, 22 }, new[] { 82, 52, 26 }, new[] { 168, 116, 54 }, new[] { 224, 176, 96 }, new[] { 150, 50, 40 } };
        int[][] shirts = { new[] { 74, 122, 191 }, new[] { 200, 82, 72 }, new[] { 90, 160, 96 }, new[] { 140, 100, 170 }, new[] { 60, 74, 92 } };
        Color32 skin = Pick(r, skins), hair = Pick(r, hairs), shirt = Pick(r, shirts);
        Color32 bg1 = new Color32(38, 42, 54, 255), bg2 = new Color32(30, 33, 43, 255);

        const int S = 112;
        var px = new Color32[S * S];
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                Color32 c;
                // fondo: degradado diagonal
                float t = (x + (S - y)) / (2f * S);
                c = Color32.Lerp(bg2, bg1, t);

                int dyTop = S - 1 - y; // 0 abajo
                // torso (hombros redondeados)
                float bx = Math.Abs(x - 56f);
                if (dyTop <= 30 && bx <= 34f - (30 - dyTop) * 0.45f) c = shirt;
                // cuello
                if (dyTop <= 36 && bx <= 7f) c = skin;
                // cabeza
                int hx = x - 56, hy = dyTop - 52; // centro (56, 52) desde arriba
                float hd = (float)Math.Sqrt(hx * hx + hy * hy);
                if (hd <= 23f) c = skin;
                // pelo: arco superior + laterales
                if (hd <= 24f && hy < -6f) c = hair;
                else if (hd <= 24f && Math.Abs(hx) > 13f && hy < 6f) c = hair;
                // ojos
                float e1 = (float)Math.Sqrt((x - 48) * (x - 48) + (dyTop - 55) * (dyTop - 55));
                float e2 = (float)Math.Sqrt((x - 64) * (x - 64) + (dyTop - 55) * (dyTop - 55));
                if (e1 <= 2.6f || e2 <= 2.6f) c = new Color32(26, 26, 30, 255);
                // sonrisa
                if (Math.Abs(hy - 11f) < 1.4f && Math.Abs(hx) < 6f) c = new Color32(150, 84, 66, 255);
                px[y * S + x] = c;
            }
        }
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
    }

    // ------------------------------------------------------------ controles

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
        Image img = go.GetComponent<Image>();
        img.sprite = Rounded();
        img.type = Image.Type.Sliced;
        img.color = color;
        return img;
    }

    public static Image Plain(Transform parent, string name, Vector2 aMin, Vector2 aMax,
                              Vector2 oMin, Vector2 oMax, Color color)
    {
        GameObject go = new GameObject(name, typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = oMin; rt.offsetMax = oMax;
        Image img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    public static Text MakeText(Transform parent, string name, string content, int size, Color color,
                                TextAnchor align, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax,
                                FontStyle style = FontStyle.Normal)
    {
        GameObject go = new GameObject(name, typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = oMin; rt.offsetMax = oMax;
        Text t = go.GetComponent<Text>();
        t.font = DefaultFont();
        t.text = content;
        t.fontSize = size;
        t.fontStyle = style;
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
        img.sprite = Rounded();
        img.type = Image.Type.Sliced;
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
        Image img = go.GetComponent<Image>();
        img.sprite = Rounded();
        img.type = Image.Type.Sliced;
        img.color = new Color(0.10f, 0.10f, 0.12f, 1f);
        InputField inp = go.GetComponent<InputField>();
        GameObject txtGo = new GameObject("Text", typeof(Text));
        txtGo.transform.SetParent(go.transform, false);
        RectTransform trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(14f, 4f); trt.offsetMax = new Vector2(-14f, -4f);
        Text t = txtGo.GetComponent<Text>();
        t.font = DefaultFont(); t.fontSize = 30; t.color = Color.white;
        t.supportRichText = false;
        inp.textComponent = t;
        GameObject phGo = new GameObject("Placeholder", typeof(Text));
        phGo.transform.SetParent(go.transform, false);
        RectTransform prt = phGo.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
        prt.offsetMin = new Vector2(14f, 4f); prt.offsetMax = new Vector2(-14f, -4f);
        Text ph = phGo.GetComponent<Text>();
        ph.font = DefaultFont(); ph.fontSize = 30; ph.fontStyle = FontStyle.Italic;
        ph.color = new Color(1f, 1f, 1f, 0.4f); ph.text = placeholder;
        inp.placeholder = ph;
        return inp;
    }

    public static void Clear(Transform holder)
    {
        for (int i = holder.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(holder.GetChild(i).gameObject);
    }
}
