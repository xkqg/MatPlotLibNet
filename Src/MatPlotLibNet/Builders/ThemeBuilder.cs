// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>
/// Provides a fluent API for creating customized themes from an existing base theme.
/// </summary>
/// <example>
/// Build a custom light theme with a bespoke colour cycle and dotted grid, then apply it:
/// <code>
/// Theme myTheme = new ThemeBuilder(Theme.Light)
///     .WithBackground(Color.FromRgb(245, 245, 245))
///     .WithCycleColors(Color.Blue, Color.Orange, Color.Green, Color.Purple)
///     .WithGrid(g => g with { Visible = true, LineStyle = LineStyle.Dotted })
///     .Build();
///
/// string svg = Plt.Create()
///     .WithTheme(myTheme)
///     .Plot(x, y, s => s.Label = "Series A")
///     .WithTitle("Custom Theme")
///     .ToSvg();
/// </code>
/// </example>
public sealed class ThemeBuilder
{
    private Color _background;
    private Color _foregroundText;
    private Color _axesBackground;
    private Color[] _cycleColors;
    private Font _defaultFont;
    private GridStyle _defaultGrid;
    private PropCycler? _propCycler;
    private readonly string _baseName;

    /// <summary>Initializes a new <see cref="ThemeBuilder"/> pre-populated from <paramref name="baseTheme"/>.</summary>
    /// <param name="baseTheme">The theme whose settings are copied as the starting point.</param>
    internal ThemeBuilder(Theme baseTheme)
    {
        _baseName = baseTheme.Name;
        _background = baseTheme.Background;
        _foregroundText = baseTheme.ForegroundText;
        _axesBackground = baseTheme.AxesBackground;
        _cycleColors = [.. baseTheme.CycleColors];
        _defaultFont = baseTheme.DefaultFont;
        _defaultGrid = baseTheme.DefaultGrid;
        _propCycler = baseTheme.PropCycler;
    }

    /// <summary>Sets the figure background color.</summary>
    /// <param name="color">The background color.</param>
    /// <returns>This builder for chaining.</returns>
    public ThemeBuilder WithBackground(Color color) { _background = color; return this; }

    /// <summary>Sets the foreground text color.</summary>
    /// <param name="color">The text color.</param>
    /// <returns>This builder for chaining.</returns>
    public ThemeBuilder WithForegroundText(Color color) { _foregroundText = color; return this; }

    /// <summary>Sets the axes area background color.</summary>
    /// <param name="color">The axes background color.</param>
    /// <returns>This builder for chaining.</returns>
    public ThemeBuilder WithAxesBackground(Color color) { _axesBackground = color; return this; }

    /// <summary>Sets the color cycle used for successive plot series.</summary>
    /// <param name="colors">The colors to cycle through.</param>
    /// <returns>This builder for chaining.</returns>
    public ThemeBuilder WithCycleColors(params Color[] colors) { _cycleColors = [.. colors]; return this; }

    /// <summary>Configures the default font using a transform function.</summary>
    /// <param name="configure">A function that receives the current font and returns a modified copy.</param>
    /// <returns>This builder for chaining.</returns>
    public ThemeBuilder WithFont(Func<Font, Font> configure)
    {
        _defaultFont = configure(_defaultFont);
        return this;
    }

    /// <summary>Configures the default grid style using a transform function.</summary>
    /// <param name="configure">A function that receives the current grid style and returns a modified copy.</param>
    /// <returns>This builder for chaining.</returns>
    public ThemeBuilder WithGrid(Func<GridStyle, GridStyle> configure)
    {
        _defaultGrid = configure(_defaultGrid);
        return this;
    }

    /// <summary>Sets the multi-property cycle. Pass <see langword="null"/> to clear and fall back to <see cref="Theme.CycleColors"/>.</summary>
    /// <param name="cycler">The <see cref="PropCycler"/> to use, or <see langword="null"/> to disable.</param>
    /// <returns>This builder for chaining.</returns>
    public ThemeBuilder WithPropCycler(PropCycler? cycler) { _propCycler = cycler; return this; }

    /// <summary>Builds and returns the customized <see cref="Theme"/>.</summary>
    /// <returns>The constructed theme.</returns>
    public Theme Build() => new(
        $"custom-{_baseName}",
        _background,
        _foregroundText,
        _axesBackground,
        _cycleColors,
        _defaultFont,
        _defaultGrid,
        _propCycler);
}
