// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Builders;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Playground;

/// <summary>Single source of truth for every Playground toggle/value. All playground
/// examples receive an instance of this record and must respect its values consistently.
/// New options should be added here so they apply uniformly across all examples.
/// <para>Phase N.1 of v1.7.2: every categorical property is now typed — the
/// pre-Phase-N magic-string soup (<c>ThemeName</c> / <c>LineStyle</c> /
/// <c>MarkerStyle</c> / <c>ColorMap</c> as free-form strings with 25-case /
/// 4-case / 8-case resolver switches) was the root of several "advertised but
/// silently collapsed" bugs. Typed properties make a typo unbuildable.</para></summary>
public sealed record PlaygroundOptions
{
    // --- Figure ---
    /// <summary>The title drawn above the figure. Empty (the default) draws no title.</summary>
    public string Title { get; init; } = "";
    /// <summary>The visual theme (colours, fonts, grid look) applied to the whole figure. <c>Theme.Default</c> unless the reader picks another.</summary>
    public Theme Theme { get; init; } = Theme.Default;
    // 16:9 at 800 px natural width — Phase L.5 of the v1.7.2 plan dropped the
    // playground Width/Height sliders because the SVG is now responsive by default
    // (Phase L.2). These values drive the `viewBox` aspect ratio + the
    // `.WithSize(...)` line in the copyable code snippet, nothing else.
    /// <summary>The figure width in pixels. The SVG scales to its container, so this sets the aspect ratio and the size in the copyable code. Default 800.</summary>
    public int Width { get; init; } = 800;
    /// <summary>The figure height in pixels (with <see cref="Width"/>, a 16:9 shape). Default 450.</summary>
    public int Height { get; init; } = 450;
    /// <summary>When <see langword="true"/>, the figure measures its content and trims the margins to fit it. Off by default.</summary>
    public bool TightLayout { get; init; }
    /// <summary>When <see langword="true"/>, the SVG carries the inline scripts that let a reader zoom, pan and hover in the browser without a server. Off by default.</summary>
    public bool BrowserInteraction { get; init; }

    // --- Axes ---
    /// <summary>Whether the legend naming each series is drawn. On by default.</summary>
    public bool ShowLegend { get; init; } = true;
    /// <summary>Whether grid lines are drawn behind the data. On by default.</summary>
    public bool ShowGrid { get; init; } = true;
    /// <summary>When <see langword="true"/>, the top edge line of the plot area is not drawn. Off by default.</summary>
    public bool HideTopSpine { get; init; }
    /// <summary>When <see langword="true"/>, the right edge line of the plot area is not drawn. Off by default.</summary>
    public bool HideRightSpine { get; init; }
    /// <summary>When <see langword="true"/>, the data touches the axis spines with no padding around it. Off by default, which leaves the usual padding.</summary>
    public bool TightMargins { get; init; }

    // --- Series styling (line/scatter examples) ---
    /// <summary>The dash pattern of the lines in the line examples. Solid by default.</summary>
    public LineStyle LineStyle { get; init; } = LineStyle.Solid;
    /// <summary>The thickness of the lines in the line examples, in points. Default 1.5.</summary>
    public double LineWidth { get; init; } = 1.5;
    /// <summary>The shape drawn at each data point of the line and scatter examples. <see cref="MarkerStyle.None"/> (the default) draws no markers on a line; the scatter example then falls back to circles.</summary>
    public MarkerStyle Marker { get; init; } = MarkerStyle.None;
    /// <summary>The size of the marker drawn at each point, in points. The line examples draw markers only when <see cref="Marker"/> is set. Default 6.</summary>
    public int MarkerSize { get; init; } = 6;

    // --- Colormap (heatmap/contour examples) ---
    /// <summary>The colour map that turns values into colours in the heatmap and contour examples. Viridis by default.</summary>
    public IColorMap ColorMap { get; init; } = ColorMaps.Viridis;
    /// <summary>Whether the heatmap and contour examples draw a colour bar beside the plot to show what each colour means. On by default.</summary>
    public bool ShowColorBar { get; init; } = true;

    /// <summary>Applies all FIGURE-level options (theme, size, title, browser interaction)
    /// EXCEPT TightLayout — which must be applied AFTER all subplots are added (call
    /// <see cref="ApplyTightLayout"/> on the builder after constructing subplots).</summary>
    public FigureBuilder ApplyToFigure(FigureBuilder b)
    {
        b = b.WithTitle(Title).WithTheme(Theme).WithSize(Width, Height);
        if (BrowserInteraction) b = b.WithBrowserInteraction();
        return b;
    }

    /// <summary>Final pass after subplots are added — TightLayout's measurement engine
    /// needs the subplots to exist before it can compute correct margins.</summary>
    public FigureBuilder ApplyTightLayout(FigureBuilder b)
        => TightLayout ? b.TightLayout() : b;

    /// <summary>Applies all AXES-level options consistently. Phase P fix (v1.7.2 follow-on,
    /// 2026-04-18): ShowGrid and ShowLegend are now set EXPLICITLY in both directions so
    /// toggling the playground checkbox produces a visible change. Pre-P the "skip the
    /// call when toggle is off" pattern left <c>Axes.Legend</c> and <c>Axes.Grid</c> at
    /// their default-visible state, making the "off" render identical to "on" — user
    /// reported the checkboxes having no visual effect. <c>AxesBuilder.ShowGrid(bool)</c>
    /// only toggles <c>Grid.Visible</c>; it does NOT override the theme's grid styling
    /// (color/width/style), so the old "never call ShowGrid(true)" rule was stale.</summary>
    public AxesBuilder ApplyToAxes(AxesBuilder ax)
    {
        ax = ax.ShowGrid(ShowGrid);                      // explicit both ways
        ax = ax.WithLegend(visible: ShowLegend);          // explicit both ways
        if (HideTopSpine)    ax = ax.HideTopSpine();
        if (HideRightSpine)  ax = ax.HideRightSpine();
        if (TightMargins)    ax = ax.WithTightMargins();
        return ax;
    }
}
