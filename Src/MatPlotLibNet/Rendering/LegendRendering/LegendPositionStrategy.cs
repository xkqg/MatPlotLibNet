// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Rendering.LegendRendering;

/// <summary>
/// Abstract base for legend-positioning strategies. Each concrete subtype encodes
/// the box-placement formula for one <see cref="LegendPosition"/> value, so
/// <see cref="AxesRenderer"/> carries no switch over the positions.
/// </summary>
/// <remarks>
/// Strategy pattern via subclass polymorphism; the factory
/// <see cref="LegendPositionStrategyFactory"/> does the single static dispatch from enum
/// to instance.
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

    /// <summary>Computes the top-left pixel position of the legend box knowing the marks the chart drew. A position that
    /// names its place (every one but <see cref="LegendPosition.Best"/>) stands there whatever is under it, so this is the
    /// answer of <see cref="ComputeBox(Rect, double, double)"/>; <c>Best</c> overrides it to look.</summary>
    /// <param name="plotArea">The pixel-space rectangle that bounds the plot.</param>
    /// <param name="boxW">The pre-measured legend box width in pixels.</param>
    /// <param name="boxH">The pre-measured legend box height in pixels.</param>
    /// <param name="obstacles">The marks the chart drew on this axes.</param>
    internal virtual Point ComputeBox(Rect plotArea, double boxW, double boxH, MarkObstacles obstacles)
        => ComputeBox(plotArea, boxW, boxH);

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
/// The "best" position: the first of ten places that covers the fewest of the chart's marks, tried in matplotlib's order
/// (upper right, upper left, lower left, lower right, right, centre left, centre right, lower centre, upper centre,
/// centre), each placed by its own strategy. With no marks to look at it is the upper right, as it always was; the marks
/// come in through <see cref="ComputeBox(Rect, double, double, MarkObstacles)"/>.
/// </summary>
internal sealed record BestStrategy : LegendPositionStrategy
{
    private static readonly LegendPositionStrategy[] Places =
    [
        new UpperRightStrategy(), new UpperLeftStrategy(), new LowerLeftStrategy(), new LowerRightStrategy(),
        new RightStrategy(), new CenterLeftStrategy(), new CenterRightStrategy(), new LowerCenterStrategy(),
        new UpperCenterStrategy(), new CenterStrategy(),
    ];

    public override Point ComputeBox(Rect plotArea, double boxW, double boxH)
        => Places[0].ComputeBox(plotArea, boxW, boxH);

    internal override Point ComputeBox(Rect plotArea, double boxW, double boxH, MarkObstacles obstacles)
    {
        var corners = new Point[Places.Length];
        var boxes = new Rect[Places.Length];
        for (int i = 0; i < Places.Length; i++)
        {
            corners[i] = Places[i].ComputeBox(plotArea, boxW, boxH);
            boxes[i] = new Rect(corners[i].X, corners[i].Y, boxW, boxH);
        }
        return corners[obstacles.FirstLeastCovered(boxes)];
    }
}
