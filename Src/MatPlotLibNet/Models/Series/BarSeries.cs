// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Specifies the orientation of a bar chart.</summary>
public enum BarOrientation
{
    /// <summary>Bars are drawn vertically.</summary>
    Vertical,

    /// <summary>Bars are drawn horizontally.</summary>
    Horizontal
}

/// <summary>Represents a bar chart series displaying categorical data as rectangular bars.</summary>
public sealed class BarSeries : ChartSeries
{
    /// <summary>Gets the category labels for each bar.</summary>
    public string[] Categories { get; }

    /// <summary>Gets the numeric values for each bar.</summary>
    public double[] Values { get; }

    /// <summary>Gets or sets the bar orientation (vertical or horizontal).</summary>
    public BarOrientation Orientation { get; set; } = BarOrientation.Vertical;

    /// <summary>Gets or sets the fill color of the bars.</summary>
    public Color? Color { get; set; }

    /// <summary>Gets or sets the edge color of the bars.</summary>
    public Color? EdgeColor { get; set; }

    /// <summary>Gets or sets the relative width of each bar (0.0 to 1.0).</summary>
    public double BarWidth { get; set; } = 0.8;


    /// <summary>Initializes a new instance of <see cref="BarSeries"/> with the specified categories and values.</summary>
    /// <param name="categories">The category labels for each bar.</param>
    /// <param name="values">The numeric values for each bar.</param>
    public BarSeries(string[] categories, double[] values)
    {
        Categories = categories;
        Values = values;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
