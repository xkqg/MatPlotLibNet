// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.GraphQL;

/// <summary>Wraps a user-supplied factory function that creates a <see cref="Figure"/> for a given chart identifier.</summary>
public sealed class ChartFigureFactory(Func<string, Figure> factory)
{
    /// <summary>Creates a figure for the specified chart identifier.</summary>
    public Figure Create(string chartId) => factory(chartId);
}
