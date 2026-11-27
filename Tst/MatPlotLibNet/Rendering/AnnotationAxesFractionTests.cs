// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>An annotation may be placed in AXES FRACTION — matplotlib's <c>xycoords='axes fraction'</c> —
/// so a label sits at "top-left of the panel" whatever the data limits are. A label in data coordinates
/// moves, or leaves the panel, the moment the limits change; a panel name must not.</summary>
public class AnnotationAxesFractionTests
{
    private static string Drawn(AnnotationCoordinates coordinates) => Plt.Create().WithSize(400, 200)
        .AddSubPlot(1, 1, 1, ax =>
        {
            ax.Plot([1e6, 2e6], [1e6, 2e6]);
            ax.Annotate("Ldr#02a4", 0.05, 0.9, a => a.Coordinates = coordinates);
        })
        .ToSvg();

    private static (double X, double Y) LabelPixel(AnnotationCoordinates coordinates)
    {
        var label = Drawn(coordinates).TextReading("Ldr#02a4");
        return (label.Number("x"), label.Number("y"));
    }

    [Fact]
    public void AnAxesFractionAnnotation_StaysInsideThePanel_WhateverTheLimits()
    {
        var (x, y) = LabelPixel(AnnotationCoordinates.AxesFraction);

        Assert.InRange(x, 0, 400);
        Assert.InRange(y, 0, 200);
    }

    /// <summary>The same numbers in data coordinates name a point far left of a 1e6..2e6 axis, which the axes do not show, so the
    /// annotation is not drawn (matplotlib's <c>Annotation._check_xy</c>).</summary>
    [Fact]
    public void ADataAnnotation_AtTheSameNumbers_IsOutsideTheAxes_AndNotDrawn()
    {
        Assert.DoesNotContain(Drawn(AnnotationCoordinates.Data).Elements("text"), text => text.Text == "Ldr#02a4");
    }
}
