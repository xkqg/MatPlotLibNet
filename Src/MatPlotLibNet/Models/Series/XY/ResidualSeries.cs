// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a residual plot series that displays regression residuals as a scatter plot with an optional zero line.</summary>
public sealed class ResidualSeries : ChartSeries, IHasColor
{
    /// <summary>The x coordinate of each observation, in data units; the residuals are plotted against these.</summary>
    public Vec XData { get; }

    /// <summary>The y coordinate of each observation, in data units; one value per <see cref="XData"/> entry. A residual
    /// is the distance of an observation from the fitted polynomial.</summary>
    public Vec YData { get; }

    /// <summary>The degree of the polynomial whose misfit is plotted: 1, the default, is a straight line, 2 a parabola,
    /// and so on.</summary>
    public int Degree { get; set; } = 1;

    /// <summary>The diameter of each residual marker, in pixels. Default 6.</summary>
    public double MarkerSize { get; set; } = 6;

    /// <inheritdoc />
    public Color? Color { get; set; }

    /// <summary>Whether a dashed horizontal line is drawn at a residual of zero, as the reference for a perfect fit. True
    /// by default.</summary>
    public bool ShowZeroLine { get; set; } = true;

    /// <summary>Initializes a new instance of <see cref="ResidualSeries"/> with the specified X and Y data.</summary>
    /// <param name="xData">The X data values.</param>
    /// <param name="yData">The Y data values.</param>
    public ResidualSeries(Vec xData, Vec yData)
    {
        XData = xData;
        YData = yData;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (XData.Length == 0) return new(0, 1, -1, 1);
        // The fit is made over the pairs that can be drawn: a pair with a missing value (NaN) or an infinity is left out.
        var pairs = XData.Data.DrawablePairs(YData.Data);
        if (pairs.X.DrawableRange() is not { } x) return new(null, null, null, null);
        try
        {
            double[] coeffs = LeastSquares.PolyFit(pairs.X, pairs.Y, Degree);
            double[] predicted = LeastSquares.PolyEval(coeffs, pairs.X);
            Vec residuals = new Vec(pairs.Y) - new Vec(predicted);
            double rMin = residuals.Min();
            double rMax = residuals.Max();
            double padding = Math.Max(Math.Abs(rMax - rMin) * 0.1, 0.1);
            return new(x.Lo, x.Hi, rMin - padding, rMax + padding);
        }
        catch (Exception ex) when (ex is ArgumentException or IndexOutOfRangeException)
        {
            // Same reachable failure as ResidualSeriesRenderer.Render (mismatched XData/YData
            // lengths) — a series whose range can't be computed contributes nothing, mirroring the
            // empty-series default above, rather than crashing axes-range aggregation for the whole
            // figure.
            ChartDiagnostics.Emit(new ChartDiagnostic(
                nameof(ResidualSeries),
                $"Residual range computation failed and was skipped: {ex.Message}",
                ex));
            return new(0, 1, -1, 1);
        }
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "residual",
        XData = XData,
        YData = YData,
        Degree = Degree,
        MarkerSize = MarkerSize,
        Color = Color,
        ShowZeroLine = ShowZeroLine ? null : false,
    };

    /// <summary>Reconstructs a <see cref="ResidualSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static ResidualSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Residplot(dto.XData ?? [], dto.YData ?? []);
        if (dto.Degree.HasValue)
        {
            s.Degree = dto.Degree.Value;
        }
        if (dto.MarkerSize.HasValue)
        {
            s.MarkerSize = dto.MarkerSize.Value;
        }
        if (dto.Color.HasValue)
        {
            s.Color = dto.Color.Value;
        }
        s.ShowZeroLine = dto.ShowZeroLine ?? true;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>The observations; the residual the chart draws is derived from them by the renderer.</remarks>
    public override ChartDataTable? ToDataTable() =>
        ChartDataTable.FromNumberColumns([new("x", Axis: DataAxis.X), new(Label ?? "y", Axis: DataAxis.Y)], [XData.Data, YData.Data]);

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
