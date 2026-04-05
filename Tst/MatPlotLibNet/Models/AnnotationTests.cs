// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Models;

public class AnnotationTests
{
    [Fact]
    public void Annotation_StoresTextAndPosition()
    {
        var ann = new Annotation("test", 1.5, 2.5);
        Assert.Equal("test", ann.Text);
        Assert.Equal(1.5, ann.X);
        Assert.Equal(2.5, ann.Y);
    }

    [Fact]
    public void Annotation_ArrowTarget_DefaultsToNull()
    {
        var ann = new Annotation("test", 1, 2);
        Assert.Null(ann.ArrowTargetX);
        Assert.Null(ann.ArrowTargetY);
    }

    [Fact]
    public void Axes_AddAnnotation_AppearsInCollection()
    {
        var axes = new Axes();
        var ann = axes.Annotate("label", 3.0, 4.0);
        Assert.Single(axes.Annotations);
        Assert.Equal("label", axes.Annotations[0].Text);
    }

    [Fact]
    public void Axes_Annotations_DefaultsToEmpty()
    {
        var axes = new Axes();
        Assert.Empty(axes.Annotations);
    }
}
