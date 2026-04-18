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
    public string Title { get; init; } = "";
    public Theme Theme { get; init; } = Theme.Default;
    // 16:9 at 800 px natural width — Phase L.5 of the v1.7.2 plan dropped the
    // playground Width/Height sliders because the SVG is now responsive by default
    // (Phase L.2). These values drive the `viewBox` aspect ratio + the
    // `.WithSize(...)` line in the copyable code snippet, nothing else.
    public int Width { get; init; } = 800;
    public int Height { get; init; } = 450;
    public bool TightLayout { get; init; }
    public bool BrowserInteraction { get; init; }

    // --- Axes ---
    public bool ShowLegend { get; init; } = true;
    public bool ShowGrid { get; init; } = true;
    public bool HideTopSpine { get; init; }
    public bool HideRightSpine { get; init; }
    public bool TightMargins { get; init; }

    // --- Series styling (line/scatter examples) ---
    public LineStyle LineStyle { get; init; } = LineStyle.Solid;
    public double LineWidth { get; init; } = 1.5;
    public MarkerStyle Marker { get; init; } = MarkerStyle.None;
    public int MarkerSize { get; init; } = 6;

    // --- Colormap (heatmap/contour examples) ---
    public IColorMap ColorMap { get; init; } = ColorMaps.Viridis;
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

    /// <summary>Applies all AXES-level options consistently. <see cref="ShowGrid"/>=true
    /// keeps the THEME's default grid (which is theme-tuned); =false explicitly hides it.
    /// We never call <c>ShowGrid(true)</c> because that overrides the theme's grid styling
    /// with a generic faded default — that was the v1.7.0 playground bug where checking
    /// "show grid" produced a thinner/lighter grid than the unchecked state.</summary>
    public AxesBuilder ApplyToAxes(AxesBuilder ax)
    {
        if (!ShowGrid)       ax = ax.ShowGrid(false);
        if (HideTopSpine)    ax = ax.HideTopSpine();
        if (HideRightSpine)  ax = ax.HideRightSpine();
        if (TightMargins)    ax = ax.WithTightMargins();
        if (ShowLegend)      ax = ax.WithLegend();
        return ax;
    }
}
