// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet;

/// <summary>
/// A faceted figure preset that renders a joint scatter plot with X and Y marginal histograms
/// in a 2×2 grid: top marginal (X distribution), center scatter, right marginal (Y distribution).
/// </summary>
/// <example>
/// <code>
/// var fb = new JointPlotFigure(x, y) { Title = "Joint", Bins = 20, Hue = groups }.Build();
/// fb.ToSvg();
/// </code>
/// </example>
public sealed class JointPlotFigure : FacetedFigure
{
    private readonly double[] _x;
    private readonly double[] _y;

    /// <summary>Initializes a new joint plot with the given X and Y data.</summary>
    public JointPlotFigure(double[] x, double[] y) { _x = x; _y = y; }

    /// <summary>Number of histogram bins for the marginal distributions (default 30).</summary>
    public int Bins { get; init; } = 30;

    /// <summary>
    /// Optional hue label per observation. When set, the center scatter and both marginals
    /// are rendered with one series per unique hue value.
    /// </summary>
    public string[]? Hue { get; init; }

    /// <inheritdoc/>
    protected override void BuildCore(FigureBuilder fb)
    {
        fb.WithGridSpec(2, 2, heightRatios: [1.0, 4.0], widthRatios: [4.0, 1.0]);

        // Top marginal: X distribution
        fb.AddSubPlot(new GridPosition(0, 1, 0, 1), ax =>
        {
            AddHistograms(ax, _x, Bins, Hue);
            ConfigurePanelDefaults(ax);
        });

        // Center: joint scatter
        fb.AddSubPlot(new GridPosition(1, 2, 0, 1), ax =>
        {
            AddScatters(ax, _x, _y, Hue);
        });

        // Right marginal: Y distribution
        fb.AddSubPlot(new GridPosition(1, 2, 1, 2), ax =>
        {
            AddHistograms(ax, _y, Bins, Hue);
            ConfigurePanelDefaults(ax);
        });
    }
}
