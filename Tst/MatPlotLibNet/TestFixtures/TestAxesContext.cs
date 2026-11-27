// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests;

/// <summary>The axes context a test hands a series: what the axes would tell it, set by name. Left as it stands it is axes that have not been
/// given a limit, grouped bars and no other series on the axis pair, which is what a series sees before the axes decide anything.</summary>
/// <param name="XAxisMin">The lower x limit the user pinned, or <see langword="null"/>.</param>
/// <param name="XAxisMax">The upper x limit the user pinned, or <see langword="null"/>.</param>
/// <param name="YAxisMin">The lower y limit the user pinned, or <see langword="null"/>.</param>
/// <param name="YAxisMax">The upper y limit the user pinned, or <see langword="null"/>.</param>
/// <param name="BarMode">How the bars on these axes share the category slot.</param>
/// <param name="Series">The series that share the axis pair, or <see langword="null"/> for none.</param>
internal sealed record TestAxesContext(
    double? XAxisMin = null,
    double? XAxisMax = null,
    double? YAxisMin = null,
    double? YAxisMax = null,
    BarMode BarMode = BarMode.Grouped,
    IReadOnlyList<ISeries>? Series = null) : IAxesContext
{
    /// <inheritdoc />
    public IReadOnlyList<ISeries> AllSeries => Series ?? [];
}
