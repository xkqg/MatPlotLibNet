// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Tools;

namespace MatPlotLibNet;

/// <summary>The two parts a Q-Q plot is made of, handed to the callback of <see cref="DistributionAxesExtensions.QQPlot"/>
/// so each can be styled on its own: the points of the sample and the reference line. They are the very series and trendline
/// on the axes, so a change made through them is the chart's.
/// <para>A value of this type can only be made by the library: it has no public constructor, and its properties have no
/// setters.</para></summary>
public readonly record struct QQPlotParts
{
    /// <summary>Makes the parts of one Q-Q plot.</summary>
    /// <param name="points">The points: the sorted sample against the reference's quantiles.</param>
    /// <param name="reference">The least-squares line through the points.</param>
    internal QQPlotParts(ScatterSeries points, Trendline reference)
    {
        Points = points;
        Reference = reference;
    }

    /// <summary>The points: one for each observation, the sample sorted up the Y axis against the quantiles of the reference
    /// distribution (the standard normal one for a Q-Q plot) at Filliben's plotting positions along the X axis. They wear the theme's colour for the chart's one series and have no
    /// legend label until one is given here.</summary>
    public ScatterSeries Points { get; }

    /// <summary>The reference: the least-squares line through the points, drawn from the first theoretical quantile to the
    /// last as a solid red line, 1.5 points wide. A trendline takes no colour slot and has no legend entry; set
    /// <see cref="Trendline.IsExtended"/> to run it across the whole plot.</summary>
    public Trendline Reference { get; }
}
