#region

using SkiaSharp;

#endregion

namespace Browser.Utils;

public static class FontUtils {
    private static readonly Dictionary<(string, string), SKTypeface> Fonts = new();

    public static SKFont GetFont(float size, string weight, string style) {
        var key = (weight, style);
        if (Fonts.TryGetValue(key, out var typeface)) return new SKFont(typeface, size);
        var skiaWeight = weight == "bold" ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
        var skiaStyle = style == "italic" ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;
        const SKFontStyleWidth skiaWidth = SKFontStyleWidth.Normal;
        var styleInfo = new SKFontStyle(skiaWeight, skiaWidth, skiaStyle);
        var skTypeface = SKTypeface.FromFamilyName("Arial", styleInfo);
        Fonts[key] = skTypeface;
        return new SKFont(Fonts[key], size);
    }

    public static float GetLineHeight(SKFont font) {
        var metrics = font.Metrics;
        return metrics.Descent - metrics.Ascent;
    }

    public static float ParseFontSize(string fontSize) {
        return (float)Convert.ToDouble(fontSize) * 0.75f;
    }
}