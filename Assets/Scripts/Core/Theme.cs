using UnityEngine;

/// <summary>
/// Paleta del juego: tema claro (Family Go) y tema oscuro.
/// Todos los componentes piden los colores aquí, así el cambio de tema es instantáneo.
/// </summary>
public static class Theme
{
    public static bool Dark = false;

    public static Color Bg => Dark ? new Color(0.12f, 0.13f, 0.16f, 1f)
                                   : new Color(0.918f, 0.949f, 0.792f, 1f);

    public static Color Paper => Dark ? new Color(0.17f, 0.18f, 0.21f, 1f)
                                      : new Color(0.995f, 0.99f, 0.97f, 1f);

    public static Color Ink => Dark ? new Color(0.92f, 0.92f, 0.88f, 1f)
                                    : new Color(0.14f, 0.13f, 0.12f, 1f);

    public static Color Sub => Dark ? new Color(0.62f, 0.63f, 0.66f, 1f)
                                    : new Color(0.45f, 0.43f, 0.39f, 1f);

    public static Color Accent => Dark ? new Color(1.00f, 0.62f, 0.27f, 1f)
                                       : new Color(1.00f, 0.62f, 0.27f, 1f);

    public static Color Cream => Dark ? new Color(0.24f, 0.25f, 0.29f, 1f)
                                      : new Color(1.00f, 0.96f, 0.88f, 1f);

    public static Color Danger => Dark ? new Color(0.82f, 0.36f, 0.36f, 1f)
                                       : new Color(0.86f, 0.36f, 0.36f, 1f);

    public static Color Off => Dark ? new Color(0.28f, 0.29f, 0.33f, 1f)
                                    : new Color(0.82f, 0.80f, 0.76f, 1f);

    public static Color AutoOn => Dark ? new Color(0.42f, 0.70f, 0.38f, 1f)
                                       : new Color(0.44f, 0.74f, 0.38f, 1f);

    public static Color LogBg => Dark ? new Color(0.145f, 0.155f, 0.18f, 1f)
                                      : new Color(0.965f, 0.955f, 0.905f, 1f);

    public static Color DeathFill => Dark ? new Color(0.24f, 0.16f, 0.15f, 1f)
                                          : new Color(0.98f, 0.86f, 0.82f, 1f);

    public static Color TreeLine => Dark ? new Color(0.45f, 0.46f, 0.50f, 0.9f)
                                         : new Color(0.36f, 0.31f, 0.25f, 0.9f);

    public static Color OutlineInk => Dark ? new Color(0.06f, 0.06f, 0.07f, 1f)
                                          : new Color(0.14f, 0.13f, 0.12f, 1f);
}
