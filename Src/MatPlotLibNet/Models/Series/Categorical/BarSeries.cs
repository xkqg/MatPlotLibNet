// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
public sealed class BarSeries : ChartSeries, ICategoryLabeled, IStackable, IHasColor, IHasAlpha, IHasEdgeColor, ILabelable
{
    public string[] Categories { get; }

    /// <inheritdoc />
    string[]? ICategoryLabeled.CategoryLabels => Categories;

    public double[] Values { get; }

    public BarOrientation Orientation { get; set; } = BarOrientation.Vertical;

    public Color? Color { get; set; }

    public Color? EdgeColor { get; set; }

    public double BarWidth { get; set; } = 0.8;

    public double[]? StackBaseline { get; set; }

    public bool ShowLabels { get; set; }

    public string? LabelFormat { get; set; }

    public double Alpha { get; set; } = 1.0;

    public double LineWidth { get; set; } = 0.0;

    public BarAlignment Align { get; set; } = BarAlignment.Center;

    public HatchPattern Hatch { get; set; } = HatchPattern.None;

    public Color? HatchColor { get; set; }

    /// <summary>Pixel-space X offset within the slot applied when multiple bar series share categories (set by renderer, not user).</summary>
    internal double BarGroupOffset { get; set; }

    /// <summary>Override for bar width when rendering as part of a group (set by renderer, not user).</summary>
    internal double? BarGroupWidth { get; set; }

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
        // Report the union of actual bar EDGES, not the slot indices [0, N].
        // matplotlib contributes [first_bar_left, last_bar_right]; so should we.
        // This removes ~14 px of extra whitespace on each side of the bar group
        // (matches matplotlib's `axes.bar(...)` contribution to `DataLim`).
        double halfW = BarWidth / 2.0;
        double xMin = context.XAxisMin ?? (0.5 - halfW);
        double xMax = context.XAxisMax ?? (Categories.Length - 0.5 + halfW);
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

        // Sticky edges: matplotlib's BarContainer registers `sticky_edges.y = [0]` so that
        // `axes.ymargin` never pads below the bar baseline. Mirror that here — yMin=0 is a
        // hard floor the margin-expansion pass in CartesianAxesRenderer will not cross.
        return new(xMin, xMax, yMin, yMax, StickyYMin: 0);
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
