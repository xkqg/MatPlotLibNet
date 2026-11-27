// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>
/// Ambient configuration dictionary for global chart styling, modelled after matplotlib's rcParams.
/// <para>
/// Precedence (highest to lowest):
/// 1. Explicit property on the series/axes/figure object.
/// 2. Explicit <see cref="Theme"/> assigned to the figure.
/// 3. <see cref="Current"/> (scoped override via <see cref="StyleContext"/>).
/// 4. <see cref="Default"/> (global defaults, mutable via <see cref="Plt.Style"/>).
/// </para>
/// <para>The parameters reach a picture through the figure's theme, the one thing that gives a figure its look: a figure
/// built by <c>FigureBuilder.Build</c> without a theme of its own wears the builder's default theme with every parameter laid
/// over it that differs from the library's defaults (the colours of the figure, the axes and the text, the font size and
/// family, the grid, and the width of a line). Parameters at their defaults describe the default theme itself, so a caller
/// who sets no style gets the figure they always got.</para>
/// </summary>
public sealed class RcParams
{
    private readonly Dictionary<string, object> _params;

    // Thread-safe async-local scope stack
    private static readonly AsyncLocal<RcParams?> _current = new();

    /// <summary>The global defaults every figure falls back on when no explicit setting, theme or scoped override applies; change them through <see cref="Plt.Style"/>.</summary>
    public static RcParams Default { get; } = CreateDefaults();

    /// <summary>The library's own defaults, never written: what a parameter is measured against to tell whether a style
    /// changed it.</summary>
    private static readonly RcParams Resting = CreateDefaults();

    /// <summary>Gets the current scoped <see cref="RcParams"/>, or <see cref="Default"/> when no scope is active.</summary>
    public static RcParams Current => _current.Value ?? Default;

    /// <summary>Sets the current scoped params (used by <see cref="StyleContext"/>).</summary>
    internal static RcParams? ScopeValue
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    /// <summary>The font size the library's defaults hold, in points (matplotlib's <c>font.size</c>).</summary>
    internal const double RestingFontSize = 10.0;

    /// <summary>The width of a line the library's defaults hold, in points (matplotlib's <c>lines.linewidth</c>).</summary>
    internal const double RestingLineWidth = 1.5;

    /// <summary>The font family the library's defaults hold (matplotlib's <c>font.family</c>), which a style sheet made from a theme
    /// that names no family writes.</summary>
    internal const string RestingFontFamily = "sans-serif";

    /// <summary>The font size, in points (matplotlib's <c>font.size</c>); a theme laid over by it holds the size in pixels, at
    /// 100 / 72 to the point.</summary>
    public double FontSize
    {
        get => Get(RcParamKeys.FontSize, RestingFontSize);
        set => Set(RcParamKeys.FontSize, value);
    }

    /// <summary>The width of a line that names none, in points (matplotlib's <c>lines.linewidth</c>); a theme laid over by it
    /// draws such a line that many pixels at 100 / 72 to the point.</summary>
    public double LinesLineWidth
    {
        get => Get(RcParamKeys.LinesLineWidth, RestingLineWidth);
        set => Set(RcParamKeys.LinesLineWidth, value);
    }

    /// <summary>Initializes a new empty <see cref="RcParams"/>.</summary>
    public RcParams() => _params = [];

    private RcParams(Dictionary<string, object> defaults) => _params = defaults;

    /// <summary>Sets a parameter value.</summary>
    public void Set<T>(string key, T value) where T : notnull
        => Set(key, (object)value);

    /// <summary>Sets a parameter value (untyped, for dictionary initializer use).</summary>
    public void Set(string key, object value)
    {
        _params[key] = value;
        Interlocked.Increment(ref _version);
    }

    /// <summary>How many times a parameter was written: the theme <see cref="ThemeOver"/> made last is the theme these
    /// parameters describe for as long as this count stands.</summary>
    private int _version;

    /// <summary>The theme <see cref="ThemeOver"/> made last, with the base theme it was laid over and the
    /// <see cref="_version"/> of the parameters it read; <see langword="null"/> before the first call.</summary>
    private LaidOver? _laidOver;

    /// <summary>The theme these parameters described over a base theme when they stood at a version. One object, so a build
    /// on another thread reads all three or none.</summary>
    /// <param name="Base">The base theme it was laid over.</param>
    /// <param name="Version">The <see cref="_version"/> of the parameters it read.</param>
    /// <param name="Theme">The theme made.</param>
    private sealed record LaidOver(Theme Base, int Version, Theme Theme);

    /// <summary>Gets a parameter value, throwing <see cref="KeyNotFoundException"/> if absent.</summary>
    public T Get<T>(string key)
        => _params.TryGetValue(key, out var v) ? (T)v
            : throw new KeyNotFoundException($"rcParam key '{key}' not found.");

    /// <summary>Gets a parameter value, returning <paramref name="fallback"/> if absent.</summary>
    public T Get<T>(string key, T fallback)
        => _params.TryGetValue(key, out var v) ? (T)v : fallback;

    /// <summary>Returns <see langword="true"/> if the key is present.</summary>
    public bool ContainsKey(string key) => _params.ContainsKey(key);

    /// <summary>The theme these parameters describe, laid over <paramref name="baseTheme"/>: every parameter that differs from
    /// the library's defaults replaces what the base theme says of it, the inverse of <see cref="StyleSheet.FromTheme"/> over
    /// the keys a style sheet carries, plus the width of a line (<see cref="RcParamKeys.LinesLineWidth"/>), which becomes the
    /// width the theme draws a line that names none with. A value that is not of the key's type says nothing and is passed
    /// over. The font size, the grid width and the width of a line are matplotlib's points (<c>font.size</c>,
    /// <c>grid.linewidth</c>, <c>lines.linewidth</c>) and are turned into the pixels a theme holds at 100 / 72 to the point, as
    /// the matplotlib themes turn their own. The theme's cycler is left as it is: a width it names wins over the style's, as a
    /// <c>prop_cycle</c>'s does over <c>lines.linewidth</c> in matplotlib.</summary>
    /// <param name="baseTheme">The theme the figure would wear with no style.</param>
    /// <returns><paramref name="baseTheme"/> itself when no parameter differs from the defaults; otherwise a copy of it with the
    /// differing parameters laid over. Until a parameter is written again, a second call over the same base theme returns the
    /// theme the first one made: every build reads this, and the parameters are read once per change of them, not per
    /// figure.</returns>
    internal Theme ThemeOver(Theme baseTheme)
    {
        // The count is read before the parameters, so a write that lands while they are read leaves a memo that is already
        // out of date, and the next call lays them over again.
        int version = Volatile.Read(ref _version);
        if (_laidOver is { } laidOver && laidOver.Version == version && ReferenceEquals(laidOver.Base, baseTheme))
            return laidOver.Theme;

        var theme = LayOver(baseTheme);
        _laidOver = new LaidOver(baseTheme, version, theme);
        return theme;
    }

    /// <summary>Lays every parameter a theme carries (<see cref="ThemeRcKey.OfATheme"/>) that differs from the defaults over
    /// <paramref name="baseTheme"/> (<see cref="ThemeOver"/>).</summary>
    private Theme LayOver(Theme baseTheme)
    {
        var parts = ThemeRcParts.Of(baseTheme);
        bool laid = false;
        foreach (var key in ThemeRcKey.OfATheme)
        {
            if (_params.TryGetValue(key.Key, out var held)) laid |= key.LaysOver(held, Resting._params[key.Key], ref parts);
        }

        return laid ? parts.Over(baseTheme) : baseTheme;
    }
    /// <summary>Creates a shallow copy of this instance for scoped overrides.</summary>
    internal RcParams Clone()
    {
        var copy = new Dictionary<string, object>(_params);
        return new RcParams(copy);
    }

    private static RcParams CreateDefaults() => new(new Dictionary<string, object>
    {
        [RcParamKeys.FontFamily]          = RestingFontFamily,
        [RcParamKeys.FontSize]            = RestingFontSize,
        [RcParamKeys.FontWeight]          = "normal",
        [RcParamKeys.LinesLineWidth]      = RestingLineWidth,
        [RcParamKeys.LinesLineStyle]      = "solid",
        [RcParamKeys.AxesFaceColor]       = Colors.White,
        [RcParamKeys.AxesGrid]            = false,
        [RcParamKeys.GridColor]           = GridStyle.Plain.Color,
        [RcParamKeys.GridLineWidth]       = GridStyle.Plain.LineWidth,
        [RcParamKeys.GridAlpha]           = Themes.MatplotlibThemeFactory.GridAlpha,
        [RcParamKeys.FigureFigSizeWidth]  = Models.Figure.RestingWidth,
        [RcParamKeys.FigureFigSizeHeight] = Models.Figure.RestingHeight,
        [RcParamKeys.FigureDpi]           = 100.0,
        [RcParamKeys.FigureFaceColor]     = Colors.White,
        [RcParamKeys.TextColor]           = Colors.Black,
        [RcParamKeys.ScatterMarker]       = "circle",
        [RcParamKeys.ImageCmap]           = "viridis",
    });
}
