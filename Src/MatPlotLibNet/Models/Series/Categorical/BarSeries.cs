// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
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
public sealed class BarSeries : ChartSeries, ICategoryLabeled, IStackable
{
    /// <summary>Gets the category labels for each bar.</summary>
    public string[] Categories { get; }

    /// <inheritdoc />
    string[]? ICategoryLabeled.CategoryLabels => Categories;

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

    /// <summary>Gets the stack baseline offsets computed by the renderer for stacked bar mode. Null when not stacking.</summary>
    /// <inheritdoc />
    public double[]? StackBaseline { get; set; }

    /// <summary>Initializes a new instance of <see cref="BarSeries"/> with the specified categories and values.</summary>
    /// <param name="categories">The category labels for each bar.</param>
    /// <param name="values">The numeric values for each bar.</param>
    public BarSeries(string[] categories, double[] values)
    {
        Categories = categories;
        Values = values;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        double xMin = context.XAxisMin ?? -0.5;
        double xMax = context.XAxisMax ?? (Categories.Length - 0.5);
        double yMin = 0, yMax = double.MinValue;

        if (context.BarMode == BarMode.Stacked)
        {
            var allStackable = context.AllSeries.OfType<IStackable>().ToList();
            if (allStackable.Count > 0)
            {
                int catCount = allStackable[0].Values.Length;
                for (int c = 0; c < catCount; c++)
                {
                    double sum = allStackable.Sum(s => c < s.Values.Length ? s.Values[c] : 0);
                    if (sum > yMax) yMax = sum;
                }
            }
        }
        else
        {
            yMax = Values.Max();
        }

        return new(xMin, xMax, yMin, yMax);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "bar",
        Categories = Categories, Values = Values, Color = Color,
        Orientation = Orientation.ToString().ToLowerInvariant(),
        BarWidth = BarWidth
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
