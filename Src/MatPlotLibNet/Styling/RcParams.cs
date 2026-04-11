// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

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
/// </summary>
public sealed class RcParams
{
    private readonly Dictionary<string, object> _params;

    // Thread-safe async-local scope stack
    private static readonly AsyncLocal<RcParams?> _current = new();

    public static RcParams Default { get; } = CreateDefaults();

    /// <summary>Gets the current scoped <see cref="RcParams"/>, or <see cref="Default"/> when no scope is active.</summary>
    public static RcParams Current => _current.Value ?? Default;

    /// <summary>Sets the current scoped params (used by <see cref="StyleContext"/>).</summary>
    internal static RcParams? ScopeValue
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    /// <summary>Gets the font size (points).</summary>
    public double FontSize
    {
        get => Get(RcParamKeys.FontSize, 10.0);
        set => Set(RcParamKeys.FontSize, value);
    }

    /// <summary>Gets the default line width.</summary>
    public double LinesLineWidth
    {
        get => Get(RcParamKeys.LinesLineWidth, 1.5);
        set => Set(RcParamKeys.LinesLineWidth, value);
    }

    /// <summary>Initializes a new empty <see cref="RcParams"/>.</summary>
    public RcParams() => _params = [];

    private RcParams(Dictionary<string, object> defaults) => _params = defaults;

    /// <summary>Sets a parameter value.</summary>
    public void Set<T>(string key, T value) where T : notnull
        => _params[key] = value;

    /// <summary>Sets a parameter value (untyped, for dictionary initializer use).</summary>
    public void Set(string key, object value)
        => _params[key] = value;

    /// <summary>Gets a parameter value, throwing <see cref="KeyNotFoundException"/> if absent.</summary>
    public T Get<T>(string key)
        => _params.TryGetValue(key, out var v) ? (T)v
            : throw new KeyNotFoundException($"rcParam key '{key}' not found.");

    /// <summary>Gets a parameter value, returning <paramref name="fallback"/> if absent.</summary>
    public T Get<T>(string key, T fallback)
        => _params.TryGetValue(key, out var v) ? (T)v : fallback;

    /// <summary>Returns <see langword="true"/> if the key is present.</summary>
    public bool ContainsKey(string key) => _params.ContainsKey(key);

    /// <summary>Creates a shallow copy of this instance for scoped overrides.</summary>
    internal RcParams Clone()
    {
        var copy = new Dictionary<string, object>(_params);
        return new RcParams(copy);
    }

    private static RcParams CreateDefaults() => new(new Dictionary<string, object>
    {
        [RcParamKeys.FontFamily]          = "sans-serif",
        [RcParamKeys.FontSize]            = 10.0,
        [RcParamKeys.FontWeight]          = "normal",
        [RcParamKeys.LinesLineWidth]      = 1.5,
        [RcParamKeys.LinesLineStyle]      = "solid",
        [RcParamKeys.AxesFaceColor]       = Colors.White,
        [RcParamKeys.AxesGrid]            = false,
        [RcParamKeys.GridColor]           = Color.FromHex("#b0b0b0"),
        [RcParamKeys.GridLineWidth]       = 0.8,
        [RcParamKeys.GridAlpha]           = 1.0,
        [RcParamKeys.FigureFigSizeWidth]  = 800.0,
        [RcParamKeys.FigureFigSizeHeight] = 600.0,
        [RcParamKeys.FigureDpi]           = 100.0,
        [RcParamKeys.FigureFaceColor]     = Colors.White,
        [RcParamKeys.TextColor]           = Colors.Black,
        [RcParamKeys.ScatterMarker]       = "circle",
        [RcParamKeys.ImageCmap]           = "viridis",
    });
}
