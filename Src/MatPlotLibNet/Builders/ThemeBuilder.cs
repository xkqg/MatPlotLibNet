// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>
/// Provides a fluent API for creating customized themes from an existing base theme.
/// </summary>
/// <example>
/// Build a custom light theme with a bespoke colour cycle and dotted grid, then apply it:
/// <code>
/// Theme myTheme = new ThemeBuilder(Theme.Default)
///     .WithBackground(new Color(245, 245, 245))
///     .WithCycleColors(Colors.Blue, Colors.Orange, Colors.Green, Css4Colors.Purple)
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
    /// <summary>The theme being built: a copy of the base theme with every change made so far. Each verb makes the next copy through
    /// <see cref="Theme"/>'s copy constructor, the one place a theme's settings are copied, so a setting added to a theme survives a
    /// derived theme without a field here to forget, and the base theme is never changed.</summary>
    private Theme _theme;

    /// <summary>The name the built theme is given.</summary>
    private string _name;

    /// <summary>Initializes a new <see cref="ThemeBuilder"/> pre-populated from <paramref name="baseTheme"/>.</summary>
    /// <param name="baseTheme">The theme whose settings are copied as the starting point.</param>
    internal ThemeBuilder(Theme baseTheme)
    {
        _name = $"custom-{baseTheme.Name}";
        _theme = new(baseTheme.Name, baseTheme) { CycleColors = [.. baseTheme.CycleColors] };
    }

    /// <summary>Sets the figure background color.</summary>
    /// <param name="color">The background color.</param>
    /// <returns>This builder for chaining.</returns>
    public ThemeBuilder WithBackground(Color color) => Then(new(_theme.Name, _theme) { Background = color });

    /// <summary>Sets the foreground text color.</summary>
    /// <param name="color">The text color.</param>
    /// <returns>This builder for chaining.</returns>
    public ThemeBuilder WithForegroundText(Color color) => Then(new(_theme.Name, _theme) { ForegroundText = color });

    /// <summary>Sets the axes area background color.</summary>
    /// <param name="color">The axes background color.</param>
    /// <returns>This builder for chaining.</returns>
    public ThemeBuilder WithAxesBackground(Color color) => Then(new(_theme.Name, _theme) { AxesBackground = color });

    /// <summary>Sets the color cycle used for successive plot series.</summary>
    /// <param name="colors">The colors to cycle through.</param>
    /// <returns>This builder for chaining.</returns>
    public ThemeBuilder WithCycleColors(params Color[] colors) => Then(new(_theme.Name, _theme) { CycleColors = [.. colors] });

    /// <summary>Configures the default font using a transform function.</summary>
    /// <param name="configure">A function that receives the current font and returns a modified copy.</param>
    /// <returns>This builder for chaining.</returns>
    public ThemeBuilder WithFont(Func<Font, Font> configure)
        => Then(new(_theme.Name, _theme) { DefaultFont = configure(_theme.DefaultFont) });

    /// <summary>Configures the default grid style using a transform function.</summary>
    /// <param name="configure">A function that receives the current grid style and returns a modified copy.</param>
    /// <returns>This builder for chaining.</returns>
    public ThemeBuilder WithGrid(Func<GridStyle, GridStyle> configure)
        => Then(new(_theme.Name, _theme) { DefaultGrid = configure(_theme.DefaultGrid) });

    /// <summary>Sets the multi-property cycle. Pass <see langword="null"/> to clear and fall back to <see cref="Theme.CycleColors"/>.</summary>
    /// <param name="cycler">The <see cref="PropCycler"/> to use, or <see langword="null"/> to disable.</param>
    /// <returns>This builder for chaining.</returns>
    public ThemeBuilder WithPropCycler(PropCycler? cycler) => Then(new(_theme.Name, _theme) { PropCycler = cycler });

    /// <summary>Sets the default 3-D pane color (floor, left wall, right wall). Pass <see langword="null"/> to use the renderer default.</summary>
    /// <param name="color">The pane color, or <see langword="null"/> to let the renderer choose.</param>
    /// <returns>This builder for chaining.</returns>
    public ThemeBuilder WithPane3DColor(Color? color) => Then(new(_theme.Name, _theme) { Pane3DColor = color });

    /// <summary>Names the grey that reference graphics wear over the plot (reference lines, trendlines, levels, a map's
    /// outlines and the like). Without this the theme derives it from its plot: #808080 where that reaches the 3 : 1 contrast
    /// WCAG asks of a graphic, otherwise the nearest grey that does, so a theme whose plot is changed afterwards keeps a grey
    /// that can be seen. Pass <see langword="null"/> to go back to deriving it.</summary>
    /// <param name="color">The neutral colour, or <see langword="null"/> to let the theme derive it.</param>
    /// <returns>This builder for chaining.</returns>
    public ThemeBuilder WithNeutralColor(Color? color) => Then(new(_theme.Name, _theme) { NeutralColor = color });

    /// <summary>Sets the figure's colour, the plot's and the ink at once: the three a preset recolours its base by.</summary>
    /// <param name="ground">The three colours.</param>
    /// <returns>This builder for chaining.</returns>
    internal ThemeBuilder WithGround(ThemeGround ground) => Then(new(_theme.Name, _theme)
    {
        Background = ground.Background,
        AxesBackground = ground.AxesBackground,
        ForegroundText = ground.ForegroundText,
    });

    /// <summary>Names the theme. Without this a derived theme is called <c>custom-&lt;base&gt;</c>, which makes
    /// four sibling presets indistinguishable in a style sheet.</summary>
    /// <param name="name">The theme name.</param>
    /// <returns>This builder for chaining.</returns>
    public ThemeBuilder WithName(string name) { _name = name; return this; }

    /// <summary>Sets the alarm palette — the colours reserved for states that need attention, and the neutral
    /// shade a resting state wears.</summary>
    /// <param name="palette">The alarm palette.</param>
    /// <returns>This builder for chaining.</returns>
    public ThemeBuilder WithAlarmPalette(AlarmPalette palette) => Then(new(_theme.Name, _theme) { Alarm = palette });

    /// <summary>Takes <paramref name="next"/> as the theme being built: what every verb that changes a setting ends with.</summary>
    /// <param name="next">The copy with the verb's change.</param>
    /// <returns>This builder for chaining.</returns>
    private ThemeBuilder Then(Theme next)
    {
        _theme = next;
        return this;
    }

    /// <summary>Builds the customized <see cref="Theme"/>: a copy of the base theme under the builder's name, with every change made.</summary>
    /// <returns>The built theme.</returns>
    public Theme Build() => new(_name, _theme);
}
