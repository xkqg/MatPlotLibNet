// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a violin plot series displaying kernel density estimations for datasets.</summary>
public sealed class ViolinSeries : DatasetSeries
{
    /// <summary>Gets or sets the fill color of the violins.</summary>
    public Color? Color { get; set; }

    /// <summary>Gets or sets the opacity of the violins (0.0 to 1.0).</summary>
    public double Alpha { get; set; } = 0.7;


    /// <summary>Initializes a new instance of <see cref="ViolinSeries"/> with the specified datasets.</summary>
    /// <param name="datasets">An array of datasets, each containing the values for one violin.</param>
    public ViolinSeries(double[][] datasets) : base(datasets) { }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        double xMin = context.XAxisMin ?? -1;
        double xMax = context.XAxisMax ?? Datasets.Length;
        double yMin = double.MaxValue, yMax = double.MinValue;
        foreach (var ds in Datasets)
        {
            double dsMin = ds.Min(), dsMax = ds.Max();
            if (dsMin < yMin) yMin = dsMin;
            if (dsMax > yMax) yMax = dsMax;
        }
        return new(xMin, xMax, yMin, yMax);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new() { Type = "violin", Datasets = Datasets };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
