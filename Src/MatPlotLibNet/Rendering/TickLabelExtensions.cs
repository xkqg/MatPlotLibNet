// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>The labels of a set of ticks.</summary>
internal static class TickLabelExtensions
{
    extension(double[] ticks)
    {
        /// <summary>The ticks written in one number format (the same number of decimals for all), as an axis writes its own.</summary>
        internal string[] Labelled()
        {
            var format = AxesRenderer.BuildUniformTickFormatter(ticks);
            return [.. ticks.Select(format)];
        }
    }
}
