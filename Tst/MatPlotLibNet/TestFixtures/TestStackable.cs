// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests;

/// <summary>A stackable series that is not a bar, as a test hands one to the axes: it draws nothing, spans 0..2 by 0..12 and counts how
/// often its values are read.</summary>
/// <param name="values">The values to stack.</param>
internal sealed class TestStackable(double[] values) : ChartSeries, IStackable
{
    /// <summary>How many times <see cref="Values"/> was read.</summary>
    public int Reads { get; private set; }

    public double[] Values
    {
        get
        {
            Reads++;
            return values;
        }
    }

    public double[]? StackBaseline { get; set; }

    public override void Accept(ISeriesVisitor visitor, RenderArea area) { }
    public override DataRangeContribution ComputeDataRange(IAxesContext context) => new(0, 2, 0, 12);
    public override SeriesDto ToSeriesDto() => new() { Type = "test_stackable" };
}
