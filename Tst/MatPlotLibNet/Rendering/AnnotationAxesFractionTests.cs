// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>An annotation may be placed in AXES FRACTION — matplotlib's <c>xycoords='axes fraction'</c> —
/// so a label sits at "top-left of the panel" whatever the data limits are. A label in data coordinates
/// moves, or leaves the panel, the moment the limits change; a panel name must not.</summary>
public class AnnotationAxesFractionTests
{
    private static readonly Regex Text = new("<text[^>]*x=\"([-0-9.]+)\"[^>]*y=\"([-0-9.]+)\"[^>]*>Ldr#02a4<", RegexOptions.Compiled);

    private static (double X, double Y) LabelPixel(AnnotationCoordinates coordinates)
    {
        string svg = Plt.Create().WithSize(400, 200)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([1e6, 2e6], [1e6, 2e6]);
                ax.Annotate("Ldr#02a4", 0.05, 0.9, a => a.Coordinates = coordinates);
            })
            .ToSvg();
        var m = Text.Match(svg);
        Assert.True(m.Success, "the label is drawn");
        return (double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void AnAxesFractionAnnotation_StaysInsideThePanel_WhateverTheLimits()
    {
        var (x, y) = LabelPixel(AnnotationCoordinates.AxesFraction);

        Assert.InRange(x, 0, 400);
        Assert.InRange(y, 0, 200);
    }

    [Fact]
    public void ADataAnnotation_AtTheSameNumbers_IsFarOutside()
    {
        var (x, _) = LabelPixel(AnnotationCoordinates.Data);

        Assert.True(x < 0, $"a data-coordinate 0.05 on a 1e6..2e6 axis lands left of the figure, was {x}");
    }

    [Fact]
    public void TheDefault_IsData()
    {
        Assert.Equal(AnnotationCoordinates.Data, new Annotation("t", 0, 0).Coordinates);
        Assert.Equal(0, (int)AnnotationCoordinates.Data);
        Assert.Equal(1, (int)AnnotationCoordinates.AxesFraction);
    }
}
