// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.FluentApi;

/// <summary>Verifies <see cref="Builders.AxesBuilder"/> convenience methods for annotations, reference lines, and spans.</summary>
public class AnnotationBuilderTests
{
    /// <summary>Verifies that the configure action on Annotate can set ConnectionStyle.</summary>
    [Fact]
    public void Annotate_WithConfigure_SetsConnectionStyle()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([0.0, 3.0], [0.0, 3.0]);

        Annotation? captured = null;
        var svg = Plt.Create()
            .Plot([0.0, 3.0], [0.0, 3.0])
            .Annotate("lbl", 1.0, 1.0, ann =>
            {
                ann.ConnectionStyle = ConnectionStyle.Angle;
                ann.ArrowTargetX = 2.0;
                ann.ArrowTargetY = 2.0;
                captured = ann;
            })
            .ToSvg();

        Assert.NotNull(captured);
        Assert.Equal(ConnectionStyle.Angle, captured!.ConnectionStyle);
    }

    /// <summary>Verifies that the new Annotate overload with arrow coordinates sets ArrowTargetX/Y.</summary>
    [Fact]
    public void Annotate_WithArrowTarget_SetsAllArrowProperties()
    {
        Annotation? ann = null;
        Plt.Create()
            .Plot([0.0, 3.0], [0.0, 3.0])
            .Annotate("point", 1.0, 1.0, 2.5, 2.5, a => { ann = a; })
            .ToSvg();

        Assert.NotNull(ann);
        Assert.Equal(2.5, ann!.ArrowTargetX);
        Assert.Equal(2.5, ann.ArrowTargetY);
    }

    /// <summary>Verifies that the configure action can set BoxStyle and box appearance.</summary>
    [Fact]
    public void Annotate_WithCalloutBox_SetsBoxProperties()
    {
        Annotation? ann = null;
        Plt.Create()
            .Plot([0.0, 3.0], [0.0, 3.0])
            .Annotate("boxed", 1.5, 1.5, a =>
            {
                ann = a;
                a.BoxStyle = BoxStyle.Round;
                a.BoxPadding = 6;
                a.BoxFaceColor = Colors.White;
            })
            .ToSvg();

        Assert.NotNull(ann);
        Assert.Equal(BoxStyle.Round, ann!.BoxStyle);
        Assert.Equal(6.0, ann.BoxPadding);
    }

    /// <summary>Verifies that AxHLine with configure action can set Label.</summary>
    [Fact]
    public void AxHLine_WithConfigure_SetsLabel()
    {
        ReferenceLine? line = null;
        Plt.Create()
            .Plot([0.0, 3.0], [0.0, 3.0])
            .AxHLine(1.5, l => { line = l; l.Label = "mean"; })
            .ToSvg();

        Assert.NotNull(line);
        Assert.Equal("mean", line!.Label);
    }

    /// <summary>Verifies that AxVSpan with configure action can set border and label.</summary>
    [Fact]
    public void AxVSpan_WithConfigure_SetsBorderAndLabel()
    {
        SpanRegion? span = null;
        Plt.Create()
            .Plot([0.0, 3.0], [0.0, 3.0])
            .AxVSpan(1.0, 2.0, s =>
            {
                span = s;
                s.LineStyle = LineStyle.Solid;
                s.Label = "zone";
            })
            .ToSvg();

        Assert.NotNull(span);
        Assert.Equal(LineStyle.Solid, span!.LineStyle);
        Assert.Equal("zone", span.Label);
    }
}
