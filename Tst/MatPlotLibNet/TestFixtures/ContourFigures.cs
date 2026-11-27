// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests;

/// <summary>The labelled contour the tests of a clipped label draw, written once for the core suite and the native ones.</summary>
public static class ContourFigures
{
    /// <summary>The values 0 to 10 along both axes of the grid.</summary>
    private static readonly double[] Grid = [.. Enumerable.Range(0, 11).Select(i => (double)i)];

    extension(Theme theme)
    {
        /// <summary>A labelled contour of the x values 0 to 10 across a grid, the same on every row, so each level is one vertical line,
        /// under this theme: its line at 8.5 stands past the axes (the x axis is cut at 8), so its label does too.</summary>
        public Figure LabelledContourPastTheAxes()
        {
            var z = new double[11, 11];
            for (int r = 0; r < 11; r++)
                for (int c = 0; c < 11; c++)
                    z[r, c] = c;
            return Plt.Create().WithSize(400, 300).WithTheme(theme)
                .AddSubPlot(1, 1, 1, ax => ax.Contour(Grid, Grid, z, s =>
                {
                    s.LevelValues = [2.5, 5.5, 8.5];
                    s.ShowLabels = true;
                }).SetXLim(0, 8))
                .Build();
        }
    }
}
