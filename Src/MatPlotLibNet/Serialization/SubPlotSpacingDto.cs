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
    public bool? IsFractional { get; init; }
    public double? FractLeft { get; init; }
    public double? FractRight { get; init; }
    public double? FractTop { get; init; }
    public double? FractBottom { get; init; }

    private static readonly SubPlotSpacing Rest = new();

    /// <summary>The JSON's form of <paramref name="spacing"/>.</summary>
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
        IsFractional = spacing.IsFractional.OrNullAtRest(Rest.IsFractional),
        FractLeft = spacing.FractLeft.OrNullAtRest(Rest.FractLeft),
        FractRight = spacing.FractRight.OrNullAtRest(Rest.FractRight),
        FractTop = spacing.FractTop.OrNullAtRest(Rest.FractTop),
        FractBottom = spacing.FractBottom.OrNullAtRest(Rest.FractBottom),
    };

    /// <summary>The spacing this record describes.</summary>
    internal SubPlotSpacing ToSpacing() => Rest with
    {
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
    };
}
