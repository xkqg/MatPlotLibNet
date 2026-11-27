// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Serialization;

/// <summary>The figure JSON's form of a <see cref="SubPlotSpacing"/> (the figure's, and a theme's default): each
/// property is written only where it differs from a new spacing's, and read back over a new spacing.</summary>
internal sealed record SubPlotSpacingDto
{
    public double? MarginLeft { get; init; }
    public double? MarginRight { get; init; }
    public double? MarginTop { get; init; }
    public double? MarginBottom { get; init; }
    public double? HorizontalGap { get; init; }
    public double? VerticalGap { get; init; }
    public bool? TightLayout { get; init; }
    public bool? ConstrainedLayout { get; init; }
    /// <summary>The rectangle a measured layout fits the subplots into, as matplotlib writes it: left, bottom, right, top.</summary>
    public double[]? LayoutRect { get; init; }
    public bool? IsFractional { get; init; }
    public double? FractLeft { get; init; }
    public double? FractRight { get; init; }
    public double? FractTop { get; init; }
    public double? FractBottom { get; init; }
    /// <summary>The gap between two columns, as a share of the figure's width.</summary>
    public double? HorizontalGapFraction { get; init; }
    /// <summary>The gap between two rows, as a share of the figure's height.</summary>
    public double? VerticalGapFraction { get; init; }

    private static readonly SubPlotSpacing Rest = new();

    /// <summary>The JSON's form of <paramref name="spacing"/>.</summary>
    /// <param name="spacing">The spacing of the figure.</param>
    /// <returns>The record of the spacing.</returns>
    internal static SubPlotSpacingDto From(SubPlotSpacing spacing) => new()
    {
        MarginLeft = spacing.MarginLeft.OrNullAtRest(Rest.MarginLeft),
        MarginRight = spacing.MarginRight.OrNullAtRest(Rest.MarginRight),
        MarginTop = spacing.MarginTop.OrNullAtRest(Rest.MarginTop),
        MarginBottom = spacing.MarginBottom.OrNullAtRest(Rest.MarginBottom),
        HorizontalGap = spacing.HorizontalGap.OrNullAtRest(Rest.HorizontalGap),
        VerticalGap = spacing.VerticalGap.OrNullAtRest(Rest.VerticalGap),
        TightLayout = spacing.TightLayout.OrNullAtRest(Rest.TightLayout),
        ConstrainedLayout = spacing.ConstrainedLayout.OrNullAtRest(Rest.ConstrainedLayout),
        LayoutRect = spacing.LayoutRect is { } rect ? [rect.Left, rect.Bottom, rect.Right, rect.Top] : null,
        IsFractional = spacing.IsFractional.OrNullAtRest(Rest.IsFractional),
        FractLeft = spacing.FractLeft.OrNullAtRest(Rest.FractLeft),
        FractRight = spacing.FractRight.OrNullAtRest(Rest.FractRight),
        FractTop = spacing.FractTop.OrNullAtRest(Rest.FractTop),
        FractBottom = spacing.FractBottom.OrNullAtRest(Rest.FractBottom),
        HorizontalGapFraction = spacing.HorizontalGapFraction,
        VerticalGapFraction = spacing.VerticalGapFraction,
    };

    /// <summary>The spacing this record describes.</summary>
    /// <returns>The spacing of the record.</returns>
    /// <exception cref="ArgumentException">The layout rect is not four fractions of the figure that make a rectangle.</exception>
    internal SubPlotSpacing ToSpacing() => Rest with
    {
        LayoutRect = LayoutRectOf(LayoutRect),
        MarginLeft = MarginLeft ?? Rest.MarginLeft,
        MarginRight = MarginRight ?? Rest.MarginRight,
        MarginTop = MarginTop ?? Rest.MarginTop,
        MarginBottom = MarginBottom ?? Rest.MarginBottom,
        HorizontalGap = HorizontalGap ?? Rest.HorizontalGap,
        VerticalGap = VerticalGap ?? Rest.VerticalGap,
        TightLayout = TightLayout ?? Rest.TightLayout,
        ConstrainedLayout = ConstrainedLayout ?? Rest.ConstrainedLayout,
        IsFractional = IsFractional ?? Rest.IsFractional,
        FractLeft = FractLeft ?? Rest.FractLeft,
        FractRight = FractRight ?? Rest.FractRight,
        FractTop = FractTop ?? Rest.FractTop,
        FractBottom = FractBottom ?? Rest.FractBottom,
        HorizontalGapFraction = HorizontalGapFraction,
        VerticalGapFraction = VerticalGapFraction,
    };

    /// <summary>The rectangle four numbers name, left, bottom, right and top; <see langword="null"/> for none.</summary>
    private static Models.LayoutRect? LayoutRectOf(double[]? values) => values switch
    {
        null => null,
        [var left, var bottom, var right, var top] when Models.LayoutRect.IsRectangle(left, bottom, right, top) => new Models.LayoutRect(left, bottom, right, top),
        _ => throw new ArgumentException(
            "The layoutRect must be four fractions of the figure, left, bottom, right and top, each from 0 to 1, left below right and bottom below top.",
            "layoutRect"),
    };
}
