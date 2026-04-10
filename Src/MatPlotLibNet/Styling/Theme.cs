// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>
/// Defines the visual appearance of a plot, including colors, font, and grid settings.
/// </summary>
public sealed class Theme
{
    /// <summary>Gets the theme name.</summary>
    public string Name { get; }

    /// <summary>Gets the figure background color.</summary>
    public Color Background { get; init; }

    /// <summary>Gets the foreground text color.</summary>
    public Color ForegroundText { get; init; }

    /// <summary>Gets the axes area background color.</summary>
    public Color AxesBackground { get; init; }

    /// <summary>Gets the color cycle used for successive plot series.</summary>
    public Color[] CycleColors { get; init; }

    /// <summary>Gets the default font applied to text elements.</summary>
    public Font DefaultFont { get; init; }

    /// <summary>Gets the default grid style.</summary>
    public GridStyle DefaultGrid { get; init; }

    internal Theme(string name, Color background, Color foregroundText, Color axesBackground,
        Color[] cycleColors, Font defaultFont, GridStyle defaultGrid)
    {
        Name = name;
        Background = background;
        ForegroundText = foregroundText;
        AxesBackground = axesBackground;
        CycleColors = cycleColors;
        DefaultFont = defaultFont;
        DefaultGrid = defaultGrid;
    }

    /// <summary>Creates a <see cref="StyleSheet"/> that mirrors this theme's visual settings as rcParam keys.</summary>
    public StyleSheet ToStyleSheet() => StyleSheet.FromTheme(this);

    // Matplotlib's default tab10 color cycle
    private static readonly Color[] DefaultCycleColors =
    [
        Color.FromHex("#1f77b4"), // blue
        Color.FromHex("#ff7f0e"), // orange
        Color.FromHex("#2ca02c"), // green
        Color.FromHex("#d62728"), // red
        Color.FromHex("#9467bd"), // purple
        Color.FromHex("#8c564b"), // brown
        Color.FromHex("#e377c2"), // pink
        Color.FromHex("#7f7f7f"), // gray
        Color.FromHex("#bcbd22"), // olive
        Color.FromHex("#17becf"), // cyan
    ];

    /// <summary>Gets the default light theme matching matplotlib's standard style.</summary>
    public static Theme Default { get; } = new(
        name: "default",
        background: Colors.White,
        foregroundText: Colors.Black,
        axesBackground: Colors.White,
        cycleColors: DefaultCycleColors,
        defaultFont: new Font(),
        defaultGrid: new GridStyle());

    /// <summary>Gets a dark background theme.</summary>
    public static Theme Dark { get; } = new(
        name: "dark",
        background: Color.FromHex("#1C1C1C"),
        foregroundText: Color.FromHex("#E0E0E0"),
        axesBackground: Color.FromHex("#2D2D2D"),
        cycleColors: DefaultCycleColors,
        defaultFont: new Font { Color = Color.FromHex("#E0E0E0") },
        defaultGrid: new GridStyle { Color = Color.FromHex("#404040"), Visible = true });

    /// <summary>Gets a theme inspired by the Seaborn visualization library.</summary>
    public static Theme Seaborn { get; } = new(
        name: "seaborn",
        background: Colors.White,
        foregroundText: Color.FromHex("#262626"),
        axesBackground: Color.FromHex("#EAEAF2"),
        cycleColors:
        [
            Color.FromHex("#4C72B0"),
            Color.FromHex("#DD8452"),
            Color.FromHex("#55A868"),
            Color.FromHex("#C44E52"),
            Color.FromHex("#8172B3"),
            Color.FromHex("#937860"),
            Color.FromHex("#DA8BC3"),
            Color.FromHex("#8C8C8C"),
            Color.FromHex("#CCB974"),
            Color.FromHex("#64B5CD"),
        ],
        defaultFont: new Font { Family = "sans-serif", Size = 11 },
        defaultGrid: new GridStyle { Visible = true, Color = Colors.White });

    /// <summary>Gets a theme inspired by the ggplot2 R library.</summary>
    public static Theme Ggplot { get; } = new(
        name: "ggplot",
        background: Colors.White,
        foregroundText: Colors.Black,
        axesBackground: Color.FromHex("#E5E5E5"),
        cycleColors:
        [
            Color.FromHex("#E24A33"),
            Color.FromHex("#348ABD"),
            Color.FromHex("#988ED5"),
            Color.FromHex("#777777"),
            Color.FromHex("#FBC15E"),
            Color.FromHex("#8EBA42"),
            Color.FromHex("#FFB5B8"),
            Color.FromHex("#56B4E9"),
            Color.FromHex("#009E73"),
            Color.FromHex("#F0E442"),
        ],
        defaultFont: new Font(),
        defaultGrid: new GridStyle { Visible = true, Color = Colors.White });

    /// <summary>Gets a theme inspired by Bayesian Methods for Hackers.</summary>
    public static Theme Bmh { get; } = new(
        name: "bmh",
        background: Colors.White,
        foregroundText: Color.FromHex("#555555"),
        axesBackground: Color.FromHex("#EEEEEE"),
        cycleColors:
        [
            Color.FromHex("#348ABD"),
            Color.FromHex("#A60628"),
            Color.FromHex("#7A68A6"),
            Color.FromHex("#467821"),
            Color.FromHex("#D55E00"),
            Color.FromHex("#CC79A7"),
            Color.FromHex("#56B4E9"),
            Color.FromHex("#009E73"),
            Color.FromHex("#F0E442"),
            Color.FromHex("#0072B2"),
        ],
        defaultFont: new Font(),
        defaultGrid: new GridStyle { Visible = true, Color = Colors.White });

    /// <summary>Gets a theme inspired by FiveThirtyEight.com charts.</summary>
    public static Theme FiveThirtyEight { get; } = new(
        name: "fivethirtyeight",
        background: Color.FromHex("#F0F0F0"),
        foregroundText: Color.FromHex("#262626"),
        axesBackground: Color.FromHex("#F0F0F0"),
        cycleColors:
        [
            Color.FromHex("#008FD5"),
            Color.FromHex("#FC4F30"),
            Color.FromHex("#E5AE38"),
            Color.FromHex("#6D904F"),
            Color.FromHex("#8B8B8B"),
            Color.FromHex("#810F7C"),
            Color.FromHex("#137E6D"),
            Color.FromHex("#B2912F"),
            Color.FromHex("#7A68A6"),
            Color.FromHex("#A60628"),
        ],
        defaultFont: new Font { Size = 14 },
        defaultGrid: new GridStyle { Visible = true, Color = Color.FromHex("#CBCBCB") });

    /// <summary>
    /// Creates a <see cref="ThemeBuilder"/> initialized from the specified base theme.
    /// </summary>
    /// <param name="baseTheme">The theme to use as a starting point.</param>
    /// <returns>A new builder pre-populated with the base theme's values.</returns>
    public static ThemeBuilder CreateFrom(Theme baseTheme) => new(baseTheme);
}

/// <summary>
/// Configures the appearance of grid lines on a plot.
/// </summary>
public sealed record GridStyle
{
    /// <summary>Gets whether grid lines are visible.</summary>
    public bool Visible { get; init; }

    /// <summary>Gets the grid line color.</summary>
    public Color Color { get; init; } = Color.FromHex("#CCCCCC");

    /// <summary>Gets the grid line dash style.</summary>
    public LineStyle LineStyle { get; init; } = LineStyle.Solid;

    /// <summary>Gets the grid line width in points.</summary>
    public double LineWidth { get; init; } = 0.5;

    /// <summary>Gets the grid line opacity (0.0 to 1.0).</summary>
    public double Alpha { get; init; } = 0.7;
}
