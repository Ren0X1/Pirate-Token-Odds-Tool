namespace ChestCounter;

/// <summary>Name and logo of the tool, shared by the window, the report and the browser tab (favicon).</summary>
public static class Brand
{
    public const string Name = "Pirate Shop Tracker";

    /// <summary>The logo: a sea-green doubloon stamped with a skull, on a navy tile (same drawing as the .ico).</summary>
    public static string Logo(int size) => TokenPng != null
        ? TokenImg(size, "logo")
        : $"""<svg width="{size}" height="{size}" viewBox="0 0 64 64" aria-hidden="true">{LogoBody}</svg>""";

    private const string LogoBody =
        """<rect width="64" height="64" rx="16" fill="#111a2e"/><rect x=".75" y=".75" width="62.5" height="62.5" rx="15.25" fill="none" stroke="#2a3956" stroke-width="1.5"/>""" +
        """<circle cx="32" cy="32" r="21" fill="#34e6c4"/><circle cx="32" cy="32" r="16.5" fill="none" stroke="#0b1322" stroke-width="2" stroke-dasharray="2.6 2.6"/>""" +
        """<path d="M32 20.5c-6.4 0-10.8 4.5-10.8 10.2 0 3.5 1.7 5.9 4.3 7.2V42h13v-4.1c2.6-1.3 4.3-3.7 4.3-7.2 0-5.7-4.4-10.2-10.8-10.2z" fill="#0b1322"/>""" +
        """<circle cx="27.7" cy="30.6" r="2.8" fill="#34e6c4"/><circle cx="36.3" cy="30.6" r="2.8" fill="#34e6c4"/><path d="M32 34.2l-1.6 2.4h3.2z" fill="#34e6c4"/>""" +
        """<path d="M29.4 38.6v3.4M32 38.6v3.4M34.6 38.6v3.4" stroke="#34e6c4" stroke-width="1.3"/>""";

    /// <summary>The logo as a favicon link (an SVG data URI: nothing to serve).</summary>
    public static string FaviconLink => TokenPng != null
        ? "<link rel=\"icon\" type=\"image/png\" href=\"data:image/png;base64," + Convert.ToBase64String(TokenPng) + "\">"
        : "<link rel=\"icon\" type=\"image/svg+xml\" href=\"data:image/svg+xml;base64," +
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64">{LogoBody}</svg>""")) + "\">";

    /// <summary>The Pirate Shop Access token picture (PNG bytes, embedded), or null if the resource is missing.</summary>
    public static readonly byte[]? TokenPng = LoadToken();

    /// <summary>The token as an &lt;img&gt; (data URI: nothing to serve), or "" without the picture.</summary>
    public static string TokenImg(int size, string cls = "") => TokenPng == null ? "" :
        $"""<img class="{cls}" src="data:image/png;base64,{Convert.ToBase64String(TokenPng)}" width="{size}" height="{size}" alt="">""";

    private static byte[]? LoadToken()
    {
        try
        {
            using var s = typeof(Brand).Assembly.GetManifestResourceStream("token.png");
            if (s == null) return null;
            using var ms = new MemoryStream();
            s.CopyTo(ms);
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Default color of the fighter-jet HUD: military phosphor green.</summary>
    public const string DefaultHudColor = "#5AFF96";

    /// <summary>"#RRGGBB" → (r, g, b), or the default green for anything else.</summary>
    public static (byte R, byte G, byte B) ParseColor(string? hex)
    {
        hex = (hex ?? "").Trim().TrimStart('#');
        if (hex.Length == 6 && int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var v))
        {
            return ((byte)(v >> 16), (byte)(v >> 8), (byte)v);
        }
        return (0x5A, 0xFF, 0x96);
    }

    public static string NormalizeColor(string? hex)
    {
        var (r, g, b) = ParseColor(hex);
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    /// <summary>The token in one color, like the HUD's symbology: each pixel's brightness × the color, transparency kept.</summary>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static System.Drawing.Bitmap? TintedToken(string? hex)
    {
        if (TokenPng == null)
        {
            return null;
        }
        var (cr, cg, cb) = ParseColor(hex);
        using var src = new System.Drawing.Bitmap(new MemoryStream(TokenPng));
        var bmp = new System.Drawing.Bitmap(src.Width, src.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        for (var y = 0; y < src.Height; y++)
        {
            for (var x = 0; x < src.Width; x++)
            {
                var c = src.GetPixel(x, y);
                var l = (0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B) / 255.0;
                l = Math.Clamp(0.18 + 1.05 * l, 0, 1);
                bmp.SetPixel(x, y, System.Drawing.Color.FromArgb(c.A, (int)(cr * l), (int)(cg * l), (int)(cb * l)));
            }
        }
        return bmp;
    }

    /// <summary>The tinted token as a PNG data URI &lt;img&gt; (pages).</summary>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static string TintedTokenImg(string? hex, int size, string cls = "")
    {
        using var bmp = TintedToken(hex);
        if (bmp == null) return "";
        using var ms = new MemoryStream();
        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        return $"""<img class="{cls}" src="data:image/png;base64,{Convert.ToBase64String(ms.ToArray())}" width="{size}" height="{size}" alt="">""";
    }
}
