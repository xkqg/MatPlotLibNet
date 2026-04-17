// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>
/// Defines the visual appearance of a plot, including colors, font, and grid settings.
/// </summary>
public sealed class Theme
{
    public string Name { get; }

    public Color Background { get; init; }

    public Color ForegroundText { get; init; }

    public Color AxesBackground { get; init; }

    public Color[] CycleColors { get; init; }

    public Font DefaultFont { get; init; }

    public GridStyle DefaultGrid { get; init; }

    public PropCycler? PropCycler { get; init; }

    /// <summary>
    /// Optional default subplot spacing for this theme.  When non-<see langword="null"/> and the
    /// figure's own <see cref="Models.SubPlotSpacing"/> has not been explicitly overridden, the
    /// renderer uses this spacing instead of the library default.  Supports fractional margins via
    /// <see cref="Models.SubPlotSpacing.FromFractions"/>.
    /// </summary>
    public Models.SubPlotSpacing? DefaultSpacing { get; init; }

    /// <summary>
    /// Default edge (outline) color for patch-based series (histogram bars, filled areas).
    /// Mirrors <c>patch.edgecolor</c> in matplotlib style sheets.
    /// <see langword="null"/> means no edge is drawn unless the series specifies one explicitly.
    /// </summary>
    public Color? PatchEdgeColor { get; init; }

    /// <summary>
    /// Override body fill color for violin plots.  When set, <c>ViolinSeriesRenderer</c>
    /// uses this color instead of the normal series cycle color, matching matplotlib themes that
    /// assign a fixed body color (e.g. classic uses <c>'y'</c> = <c>#BFBF00</c>).
    /// <see langword="null"/> falls back to the normal cycle color.
    /// </summary>
    public Color? ViolinBodyColor { get; init; }

    /// <summary>
    /// Override color for violin plot stats lines (extrema bars, whiskers, cbars/cmins/cmaxes).
    /// <see langword="null"/> falls back to the series cycle color.
    /// </summary>
    public Color? ViolinStatsColor { get; init; }

    /// <summary>
    /// Default X-axis margin (data padding as a fraction of the data range). Matches
    /// matplotlib's <c>axes.xmargin</c> rcParam — classic style uses <c>0.0</c>, v2+ uses <c>0.05</c>.
    /// Consumed by <c>CartesianAxesRenderer.ComputeDataRanges</c> when <see cref="Models.Axis.Margin"/>
    /// is <see langword="null"/>.
    /// </summary>
    public double AxisXMargin { get; init; } = 0.05;

    /// <summary>
    /// Default Y-axis margin (data padding as a fraction of the data range). Matches matplotlib's
    /// <c>axes.ymargin</c> rcParam. Classic style uses <c>0.0</c>, v2+ uses <c>0.05</c>.
    /// </summary>
    public double AxisYMargin { get; init; } = 0.05;

    /// <summary>Default background color for 3D axes panes (floor, left wall, right wall).
    /// When <c>null</c>, the renderer uses <c>#F5F5F5</c>. Override in custom themes for
    /// dark-mode 3D charts.</summary>
    public Color? Pane3DColor { get; init; }

    /// <summary>Initializes a new <see cref="Theme"/> with all visual properties.</summary>
    internal Theme(string name, Color background, Color foregroundText, Color axesBackground,
        Color[] cycleColors, Font defaultFont, GridStyle defaultGrid, PropCycler? propCycler = null,
        Models.SubPlotSpacing? defaultSpacing = null)
    {
        Name = name;
        Background = background;
        ForegroundText = foregroundText;
        AxesBackground = axesBackground;
        CycleColors = cycleColors;
        DefaultFont = defaultFont;
        DefaultGrid = defaultGrid;
        PropCycler = propCycler;
        DefaultSpacing = defaultSpacing;
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

    public static Theme Default { get; } = new(
        name: "default",
        background: Colors.White,
        foregroundText: Colors.Black,
        axesBackground: Colors.White,
        cycleColors: DefaultCycleColors,
        defaultFont: new Font(),
        defaultGrid: new GridStyle { Visible = true, Color = Color.FromHex("#B0B0B0"), LineStyle = LineStyle.Solid, LineWidth = 0.8 });

    public static Theme Dark { get; } = new(
        name: "dark",
        background: Color.FromHex("#1C1C1C"),
        foregroundText: Color.FromHex("#E0E0E0"),
        axesBackground: Color.FromHex("#2D2D2D"),
        cycleColors: DefaultCycleColors,
        defaultFont: new Font { Color = Color.FromHex("#E0E0E0") },
        defaultGrid: new GridStyle { Color = Color.FromHex("#404040"), Visible = true });

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

    private static readonly Color[] ColorBlindSafeCycleColors =
    [
        Color.FromHex("#E69F00"), // orange
        Color.FromHex("#56B4E9"), // sky blue
        Color.FromHex("#009E73"), // bluish green
        Color.FromHex("#F0E442"), // yellow
        Color.FromHex("#0072B2"), // blue
        Color.FromHex("#D55E00"), // vermillion
        Color.FromHex("#CC79A7"), // reddish purple
        Color.FromHex("#000000"), // black
    ];

    /// <summary>
    /// Okabe-Ito color-blind safe theme — safe for all three common forms of color vision deficiency.
    /// </summary>
    public static Theme ColorBlindSafe { get; } = new(
        name: "colorblind-safe",
        background: Colors.White,
        foregroundText: Colors.Black,
        axesBackground: Colors.White,
        cycleColors: ColorBlindSafeCycleColors,
        defaultFont: new Font(),
        defaultGrid: new GridStyle { Visible = true, Color = Color.FromHex("#B0B0B0"), LineStyle = LineStyle.Solid, LineWidth = 0.8 });

    private static readonly Color[] HighContrastCycleColors =
    [
        Color.FromHex("#0000FF"), // pure blue
        Color.FromHex("#FF0000"), // pure red
        Color.FromHex("#00AA00"), // strong green
        Color.FromHex("#FF8800"), // strong orange
        Color.FromHex("#AA00FF"), // strong purple
        Color.FromHex("#00CCCC"), // strong cyan
        Color.FromHex("#CC0088"), // strong magenta
        Color.FromHex("#666600"), // dark olive
    ];

    /// <summary>
    /// High-contrast theme targeting WCAG AAA (7:1 contrast ratio). Pure white background, pure black text,
    /// bold font at 13pt, and a thick dark grid.
    /// </summary>
    public static Theme HighContrast { get; } = new(
        name: "high-contrast",
        background: Colors.White,
        foregroundText: Colors.Black,
        axesBackground: Colors.White,
        cycleColors: HighContrastCycleColors,
        defaultFont: new Font { Family = "DejaVu Sans, sans-serif", Size = 13, Weight = FontWeight.Bold },
        defaultGrid: new GridStyle { Visible = true, Color = Color.FromHex("#666666"), LineStyle = LineStyle.Solid, LineWidth = 1.5 });

    /// <summary>
    /// Matplotlib classic (pre-2.0) look — white background, the iconic <c>bgrcmyk</c> 7-color
    /// cycle, and grid hidden by default. Faithful to the matplotlib style every scientific
    /// paper printed up to 2017.
    /// </summary>
    public static Theme MatplotlibClassic { get; } = Themes.MatplotlibThemeFactory.CreateClassic();

    /// <summary>
    /// Matplotlib v2.0+ default look (since 2017) — white background, soft-black <c>#262626</c>
    /// text, the modern <c>tab10</c> 10-color cycle, and grid hidden by default. The look every
    /// Jupyter notebook ships with today.
    /// </summary>
    public static Theme MatplotlibV2 { get; } = Themes.MatplotlibThemeFactory.CreateV2();

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
    public bool Visible { get; init; }

    public Color Color { get; init; } = Color.FromHex("#CCCCCC");

    public LineStyle LineStyle { get; init; } = LineStyle.Solid;

    public double LineWidth { get; init; } = 0.5;

    public double Alpha { get; init; } = 0.7;

    public GridWhich Which { get; init; } = GridWhich.Major;

    public GridAxis Axis { get; init; } = GridAxis.Both;
}
