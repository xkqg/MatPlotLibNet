// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Rendering.LegendRendering;

/// <summary>
/// Abstract base for legend-positioning strategies. Each concrete subtype encodes
/// the box-placement formula for one <see cref="LegendPosition"/> value, replacing
/// the 14-arm switch that previously appeared TWICE in <see cref="AxesRenderer"/>
/// (once in <c>RenderLegend</c>, once in <c>ComputeLegendBounds</c> — a DRY violation).
/// </summary>
/// <remarks>
/// Phase B.2 of the strict-90 floor plan (2026-04-20). Strategy pattern via subclass
/// polymorphism; the factory <see cref="LegendPositionStrategyFactory"/> does the
/// single static dispatch from enum to instance.
/// </remarks>
public abstract record LegendPositionStrategy
{
    /// <summary>The pixel inset from the plot edge for non-outside positions.</summary>
    public const double Inset = 10;

    /// <summary>The pixel gap from the plot edge for outside positions
    /// (kept clear of spine + tick marks).</summary>
    public const double OutsideGap = 8;

    /// <summary>Computes the top-left (X, Y) pixel position of the legend box.</summary>
    /// <param name="plotArea">The pixel-space rectangle that bounds the plot.</param>
    /// <param name="boxW">The pre-measured legend box width in pixels.</param>
    /// <param name="boxH">The pre-measured legend box height in pixels.</param>
    public abstract Point ComputeBox(Rect plotArea, double boxW, double boxH);

    // Helpers used by the centered/outside strategies (avoid recomputing per-call).
    /// <summary>The horizontal centre of the plot area.</summary>
    protected static double CenterX(Rect plotArea) => plotArea.X + plotArea.Width / 2;

    /// <summary>The vertical centre of the plot area.</summary>
    protected static double CenterY(Rect plotArea) => plotArea.Y + plotArea.Height / 2;
}

internal sealed record UpperRightStrategy : LegendPositionStrategy
{
    public override Point ComputeBox(Rect plotArea, double boxW, double boxH)
        => new(plotArea.X + plotArea.Width - boxW - Inset, plotArea.Y + Inset);
}

internal sealed record UpperLeftStrategy : LegendPositionStrategy
{
    public override Point ComputeBox(Rect plotArea, double boxW, double boxH)
        => new(plotArea.X + Inset, plotArea.Y + Inset);
}

internal sealed record LowerRightStrategy : LegendPositionStrategy
{
    public override Point ComputeBox(Rect plotArea, double boxW, double boxH)
        => new(plotArea.X + plotArea.Width - boxW - Inset, plotArea.Y + plotArea.Height - boxH - Inset);
}

internal sealed record LowerLeftStrategy : LegendPositionStrategy
{
    public override Point ComputeBox(Rect plotArea, double boxW, double boxH)
        => new(plotArea.X + Inset, plotArea.Y + plotArea.Height - boxH - Inset);
}

internal sealed record RightStrategy : LegendPositionStrategy
{
    public override Point ComputeBox(Rect plotArea, double boxW, double boxH)
        => new(plotArea.X + plotArea.Width - boxW - Inset, CenterY(plotArea) - boxH / 2);
}

internal sealed record CenterLeftStrategy : LegendPositionStrategy
{
    public override Point ComputeBox(Rect plotArea, double boxW, double boxH)
        => new(plotArea.X + Inset, CenterY(plotArea) - boxH / 2);
}

internal sealed record CenterRightStrategy : LegendPositionStrategy
{
    public override Point ComputeBox(Rect plotArea, double boxW, double boxH)
        => new(plotArea.X + plotArea.Width - boxW - Inset, CenterY(plotArea) - boxH / 2);
}

internal sealed record LowerCenterStrategy : LegendPositionStrategy
{
    public override Point ComputeBox(Rect plotArea, double boxW, double boxH)
        => new(CenterX(plotArea) - boxW / 2, plotArea.Y + plotArea.Height - boxH - Inset);
}

internal sealed record UpperCenterStrategy : LegendPositionStrategy
{
    public override Point ComputeBox(Rect plotArea, double boxW, double boxH)
        => new(CenterX(plotArea) - boxW / 2, plotArea.Y + Inset);
}

internal sealed record CenterStrategy : LegendPositionStrategy
{
    public override Point ComputeBox(Rect plotArea, double boxW, double boxH)
        => new(CenterX(plotArea) - boxW / 2, CenterY(plotArea) - boxH / 2);
}

internal sealed record OutsideRightStrategy : LegendPositionStrategy
{
    public override Point ComputeBox(Rect plotArea, double boxW, double boxH)
        => new(plotArea.X + plotArea.Width + OutsideGap, CenterY(plotArea) - boxH / 2);
}

internal sealed record OutsideLeftStrategy : LegendPositionStrategy
{
    public override Point ComputeBox(Rect plotArea, double boxW, double boxH)
        => new(plotArea.X - boxW - OutsideGap, CenterY(plotArea) - boxH / 2);
}

internal sealed record OutsideTopStrategy : LegendPositionStrategy
{
    public override Point ComputeBox(Rect plotArea, double boxW, double boxH)
        => new(CenterX(plotArea) - boxW / 2, plotArea.Y - boxH - OutsideGap);
}

internal sealed record OutsideBottomStrategy : LegendPositionStrategy
{
    public override Point ComputeBox(Rect plotArea, double boxW, double boxH)
        => new(CenterX(plotArea) - boxW / 2, plotArea.Y + plotArea.Height + OutsideGap);
}

/// <summary>
/// "Best" position falls back to <see cref="UpperRightStrategy"/>'s placement
/// (matching matplotlib's default and the OLD inline switch's `_` arm).
/// </summary>
internal sealed record BestStrategy : LegendPositionStrategy
{
    public override Point ComputeBox(Rect plotArea, double boxW, double boxH)
        => new(plotArea.X + plotArea.Width - boxW - Inset, plotArea.Y + Inset);
}
