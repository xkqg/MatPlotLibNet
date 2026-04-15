// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>
/// A 3-D bar chart where each bar is rendered as a single flat, translucent rectangle in the
/// XZ plane at a fixed Y — matplotlib's <c>ax.bar(xs, heights, zs=y, zdir='y')</c> pattern,
/// also known as a "planar bar chart", "skyscraper plot", or "2D bars in different planes".
/// Use this when you want multiple rows of bars stacked on Y planes with alpha compositing,
/// instead of solid rectangular prisms (use <see cref="Bar3DSeries"/> for the cuboid variant).
/// </summary>
public sealed class PlanarBar3DSeries : XYZSeries, IHasColor, IHasAlpha, IHasEdgeColor
{
    /// <summary>Width of each bar along X. Default 0.8 (matplotlib's <c>ax.bar</c> default).</summary>
    public double BarWidth { get; set; } = 0.8;

    /// <summary>Fill translucency. Default 0.8 so rear planes show through the front ones.</summary>
    public double Alpha { get; set; } = 0.8;

    /// <summary>Uniform fill colour when <see cref="Colors"/> is null.</summary>
    public Color? Color { get; set; }

    /// <summary>Edge colour for the rectangle outline. Defaults to black in the renderer.</summary>
    public Color? EdgeColor { get; set; }

    /// <summary>
    /// Per-bar fill colours, parallel to <see cref="X"/>/<see cref="Y"/>/<see cref="Z"/>.
    /// When set, overrides <see cref="Color"/> on a per-bar basis. Bars past the array length
    /// fall back to <see cref="Color"/>. Matches the parallel-array convention used by
    /// <see cref="ScatterSeries"/>, <see cref="PieSeries"/>, <see cref="DonutSeries"/>, etc.
    /// </summary>
    /// <remarks>
    /// Three colour lookup modes are expressible via this single property:
    /// <list type="bullet">
    ///   <item><b>Per Y / per plane</b> — one <c>PlanarBar3D</c> call per Y, set <see cref="Color"/> on each.</item>
    ///   <item><b>Per X</b> — set <c>Colors = xs.Select(x =&gt; lookup(x)).ToArray()</c>.</item>
    ///   <item><b>Combined</b> — set <see cref="Color"/> as the per-plane default and use <see cref="Colors"/> to override specific X values.</item>
    /// </list>
    /// </remarks>
    public Color[]? Colors { get; set; }

    /// <summary>Initializes a new instance of <see cref="PlanarBar3DSeries"/>.</summary>
    public PlanarBar3DSeries(Vec x, Vec y, Vec z) : base(x, y, z) { }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (X.Length == 0) return new(null, null, null, null);
        // Planar bars: X spans [X[i], X[i]+BarWidth]; Y is a POINT (no width, the bar
        // is flat in that dimension); Z spans [0, Z[i]] with sticky-zero baseline.
        return new Box3D(
            X: new(X.Min(), X.Max() + BarWidth),
            Y: new(Y.Min(), Y.Max()),
            Z: new(Math.Min(0, Z.Min()), Math.Max(0, Z.Max()))
        ).ToContribution(stickyZMin: 0);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "planarbar3d",
        XData = X,
        YData = Y,
        ZData = Z,
        BarWidth = BarWidth,
        Color = Color,
        // Alpha, EdgeColor, Colors[] omitted — matches the ScatterSeries/PieSeries
        // convention of keeping per-element arrays and cosmetic overrides client-side.
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
