// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests;

/// <summary>A figure of one subplot as a test of one verb builds and reads it: the axes a chain of calls builds, and the one series
/// the subplot holds.</summary>
internal static class OneSubplotExtensions
{
    extension(FigureBuilder builder)
    {
        /// <summary>Builds the figure with one subplot that <paramref name="configure"/> fills, and returns that subplot's axes.</summary>
        /// <param name="configure">The calls on the subplot.</param>
        public Axes OneAxes(Action<AxesBuilder> configure) => builder.AddSubPlot(1, 1, 1, configure).Build().SubPlots[0];
    }

    extension(Figure figure)
    {
        /// <summary>The one series of the figure's first subplot, as the type it must be; a subplot with no series or more than one,
        /// or one of another type, fails the test.</summary>
        /// <typeparam name="TSeries">The type the series must be.</typeparam>
        public TSeries OnlySeries<TSeries>() where TSeries : ISeries =>
            Assert.IsType<TSeries>(Assert.Single(figure.SubPlots[0].Series));
    }

    extension(LineSeries line)
    {
        /// <summary>Asserts that the line runs through the given x and y values, each to within <paramref name="tolerance"/>.</summary>
        /// <param name="x">The x values the line must have.</param>
        /// <param name="y">The y values the line must have.</param>
        /// <param name="tolerance">How far each value may lie from the expected one, inclusive.</param>
        public void AssertDrawnAs(double[] x, double[] y, double tolerance)
        {
            x.AssertCloseTo(line.XData, tolerance);
            y.AssertCloseTo(line.YData, tolerance);
        }
    }
}
