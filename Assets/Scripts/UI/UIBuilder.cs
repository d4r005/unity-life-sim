using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Construye toda la interfaz desde código (uGUI), estilo cartoon con
/// contornos negros gruesos y esquinas redondeadas, generado en runtime.
/// </summary>
public static class UIBuilder
{
    private const int TexSize = 64;
    private const int Corner = 18;
    private const int OutlineW = 3;
    private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    private static readonly Color Ink = new Color(0.14f, 0.13f, 0.12f, 1f);

    public static Font DefaultFont()
    {
        try { return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
        catch { return Resources.GetBuiltinResource<Font>("Arial.ttf"); }
    }

    // ------------------------------------------------------------ sprites

    /// <summary>¿El punto (x,y) está dentro del rounded-rect con inset y radio r?</summary>
    private static bool InRoundRect(int x, int y, int size, int r, int inset)
    {
        int lo = inset, hi = size - inset;
        if (x < lo || y < lo || x >= hi || y >= hi) return false;
        int cx = Mathf.Min(x - lo, hi - 1 - x);
        int cy = Mathf.Min(y - lo, hi - 1 - y);
        if (cx >= r || cy >= r) return true;
        int dx = cx - r, dy = cy - r;
        return dx * dx + dy * dy <= r * r;
    }

    /// <summary>Sprite 9-slice: relleno del color dado con contorno negro grueso estilo cartoon.</summary>
    public static Sprite OutlineSprite(Color fill)
    {
        string key = "O:" + fill.r.ToString("0.000") + fill.g.ToString("0.000") + fill.b.ToString("0.000") + fill.a.ToString("0.00");
        if (spriteCache.TryGetValue(key, out Sprite cached)) return cached;

        Texture2D tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] px = new Color32[TexSize * TexSize];
        Color32 f32 = fill;
        for (int y = 0; y < TexSize; y++)
        {
            for (int x = 0; x < TexSize; x++)
            {
                bool outer = InRoundRect(x, y, TexSize, Corner, 0);
                bool inner = InRoundRect(x, y, TexSize, Mathf.Max(1, Corner - OutlineW), OutlineW);
                px[y * TexSize + x] = (outer && !inner) ? (Color32)Ink : (inner ? f32 : new Color32(0, 0, 0, 0));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, TexSize, TexSize),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
            new Vector4(Corner, Corner, Corner, Corner));
        spriteCache[key] = sp;
        return sp;
    }

    /// <summary>Sprite 9-slice simple: relleno del color, sin contorno.</summary>
    public static Sprite RoundedSprite(Color fill)
    {
        string key = "R:" + fill.r.ToString("0.000") + fill.g.ToString("0.000") + fill.b.ToString("0.000") + fill.a.ToString("0.00");
        if (spriteCache.TryGetValue(key, out Sprite cached)) return cached;

        Texture2D tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] px = new Color32[TexSize * TexSize];
        Color32 f32 = fill;
        for (int y = 0; y < TexSize; y++)
            for (int x = 0; x < TexSize; x++)
                px[y * TexSize + x] = InRoundRect(x, y, TexSize, Corner, 0) ? f32 : new Color32(0, 0, 0, 0);
        tex.SetPixels32(px);
        tex.Apply();
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, TexSize, TexSize),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
            new Vector4(Corner, Corner, Corner, Corner));
        spriteCache[key] = sp;
        return sp;
    }

    /// <summary>Retrato cartoon determinista a partir de la semilla del personaje.</summary>
    public static Sprite Avatar(int seed)
    {
        string key = "A:" + seed;
        if (spriteCache.TryGetValue(key, out Sprite cached)) return cached;

        System.Random r = new System.Random(seed);
        Color32[] skins = {
            new Color32(255, 219, 183, 255), new Color32(240, 184, 140, 255),
            new Color32(198, 134, 92, 255), new Color32(141, 90, 62, 255),
            new Color32(92, 58, 40, 255) };
        Color32[] hairs = {
            new Color32(45, 36, 30, 255), new Color32(105, 66, 38, 255),
            new Color32(196, 148, 84, 255), new Color32(220, 88, 60, 255),
            new Color32(120, 120, 125, 255), new Color32(240, 230, 200, 255) };
        Color32[] shirts = {
            new Color32(86, 160, 220, 255), new Color32(235, 110, 150, 255),
            new Color32(110, 190, 110, 255), new Color32(250, 170, 70, 255),
            new Color32(150, 120, 200, 255), new Color32(240, 240, 235, 255) };
        Color32 skin = skins[r.Next(skins.Length)];
        Color32 hair = hairs[r.Next(hairs.Length)];
        Color32 shirt = shirts[r.Next(shirts.Length)];
        Color32 ink = (Color32)Ink;
        Color32 blush = new Color32(250, 160, 150, 255);
        Color32 paper = new Color32(253, 250, 242, 255);

        int S = 128;
        Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] px = new Color32[S * S];
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                Color32 c = new Color32(0, 0, 0, 0);
                int dy = y - 58; int dx = x - 64;

                // fondo tipo placa circular con contorno
                int bd = Mathf.Max(Mathf.Abs(x - 64), Mathf.Abs(y - 64));
                int bc = (int)Mathf.Sqrt((x - 64) * (x - 64) + (y - 64) * (y - 64));
                if (bc <= 62) c = paper;
                if (bc > 58 && bc <= 62) c = ink;

                // torso
                if (y >= 92 && x >= 40 && x <= 88)
                {
                    int edge = Mathf.Min(x - 40, 88 - x, y - 92);
                    c = shirt;
                    if (edge < 3) c = ink;
                }

                // cabeza
                float hd = Mathf.Sqrt(dx * dx + dy * dy);
                if (hd <= 30) c = skin;
                if (hd > 27 && hd <= 30) c = ink;

                // cabello (casquete superior + patillas)
                if (hd <= 30 && (dy < -6 || (Mathf.Abs(dx) > 22 && dy < 10)))
                    c = hair;
                if (dy < -6 && hd <= 30 && dy > -14 && Mathf.Abs(dx) < 10) c = hair; // flequillo

                // ojos y mejillas
                float e1 = Mathf.Sqrt((x - 54) * (x - 54) + (y - 58) * (y - 58));
                float e2 = Mathf.Sqrt((x - 74) * (x - 74) + (y - 58) * (y - 58));
                if (e1 <= 2.6f || e2 <= 2.6f) c = ink;
                float b1 = Mathf.Sqrt((x - 46) * (x - 46) + (y - 66) * (y - 66));
                float b2 = Mathf.Sqrt((x - 82) * (x - 82) + (y - 66) * (y - 66));
                if ((b1 <= 3.4f || b2 <= 3.4f) && dy > -20) c = blush;

                // sonrisa
                if (y >= 70 && y <= 72 && x >= 59 && x <= 69) c = ink;

                px[y * S + x] = c;
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
        spriteCache[key] = sp;
        return sp;
    }

    // ------------------------------------------------------------ widgets

    public static Canvas CreateCanvas()
    {
        GameObject go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler s = go.GetComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(720, 1280);
        s.matchWidthOrHeight = 0.5f;
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
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
        img.color = Color.white;
        img.sprite = color.a > 0.01f ? OutlineSprite(color) : RoundedSprite(color);
        img.type = Image.Type.Sliced;
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
        t.color = color;
        t.alignment = align;
        t.fontStyle = style;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        t.supportRichText = false;
        return t;
    }

    public static Button MakeButton(Transform parent, string name, string label, int size, Color fill,
                                    Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax,
                                    UnityAction onClick)
    {
        GameObject go = new GameObject(name, typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = oMin; rt.offsetMax = oMax;
        Image img = go.GetComponent<Image>();
        img.color = Color.white;
        img.sprite = OutlineSprite(fill);
        img.type = Image.Type.Sliced;
        Button b = go.GetComponent<Button>();
        if (onClick != null) b.onClick.AddListener(onClick);

        GameObject lblGo = new GameObject("Label", typeof(Text));
        lblGo.transform.SetParent(go.transform, false);
        RectTransform lrt = lblGo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(6, 4); lrt.offsetMax = new Vector2(-6, -4);
        Text t = lblGo.GetComponent<Text>();
        t.font = DefaultFont();
        t.text = label;
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.fontStyle = FontStyle.Bold;
        // texto oscuro sobre fondos claros, blanco sobre oscuros
        t.color = (fill.r + fill.g + fill.b) / 3f > 0.5f ? new Color(0.14f, 0.13f, 0.12f) : Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;
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
        img.color = Color.white;
        img.sprite = OutlineSprite(new Color(1f, 1f, 1f, 1f));
        img.type = Image.Type.Sliced;
        InputField inp = go.GetComponent<InputField>();

        GameObject txtGo = new GameObject("Text", typeof(Text));
        txtGo.transform.SetParent(go.transform, false);
        RectTransform trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(14, 6); trt.offsetMax = new Vector2(-14, -6);
        Text t = txtGo.GetComponent<Text>();
        t.font = DefaultFont();
        t.fontSize = 26;
        t.color = new Color(0.14f, 0.13f, 0.12f);
        t.alignment = TextAnchor.MiddleLeft;
        inp.textComponent = t;

        GameObject phGo = new GameObject("Placeholder", typeof(Text));
        phGo.transform.SetParent(go.transform, false);
        RectTransform prt = phGo.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
        prt.offsetMin = new Vector2(14, 6); prt.offsetMax = new Vector2(-14, -6);
        Text ph = phGo.GetComponent<Text>();
        ph.font = DefaultFont();
        ph.fontSize = 26;
        ph.text = placeholder;
        ph.color = new Color(0.6f, 0.58f, 0.55f);
        ph.supportRichText = false;
        inp.placeholder = ph;
        return inp;
    }

    public static void Clear(Transform holder)
    {
        for (int i = holder.childCount - 1; i >= 0; i--)
            Object.Destroy(holder.GetChild(i).gameObject);
    }
}
