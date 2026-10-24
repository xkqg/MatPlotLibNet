// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The three series a raincloud chart is made of, handed to the callback of
/// <see cref="DistributionAxesExtensions.Raincloud"/> so each can be styled on its own: the half violin, the box and the
/// jittered points, every group's drawn by the one of each, all three in the same colour. They are the very series on
/// the axes, so a change made through them is the chart's.
/// <para>A value of this type can only be made by the library: it has no public constructor, and its properties have no
/// setters.</para></summary>
public readonly record struct RaincloudParts
{
    /// <summary>Makes the parts of one raincloud chart.</summary>
    /// <param name="violin">The half violin, one dataset per group.</param>
    /// <param name="box">The box, one dataset per group, narrower than the violin.</param>
    /// <param name="points">The raw, jittered points of every group together.</param>
    internal RaincloudParts(ViolinSeries violin, BoxSeries box, ScatterSeries points)
    {
        Violin = violin;
        Box = box;
        Points = points;
    }

    /// <summary>The half violin: one density per group, standing at the group's index.</summary>
    public ViolinSeries Violin { get; }

    /// <summary>The box: one per group, narrower than the violin, drawn without its outliers (the points already show
    /// every observation).</summary>
    public BoxSeries Box { get; }

    /// <summary>The raw points of every group, jittered a fixed, deterministic amount to the side of the box.</summary>
    public ScatterSeries Points { get; }
}
