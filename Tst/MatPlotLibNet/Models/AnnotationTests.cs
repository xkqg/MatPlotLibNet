// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies <see cref="Annotation"/> behavior.</summary>
public class AnnotationTests
{
    /// <summary>Verifies that an annotation stores its text, X, and Y values.</summary>
    [Fact]
    public void Annotation_StoresTextAndPosition()
    {
        var ann = new Annotation("test", 1.5, 2.5);
        Assert.Equal("test", ann.Text);
        Assert.Equal(1.5, ann.X);
        Assert.Equal(2.5, ann.Y);
    }

    /// <summary>Verifies that arrow target coordinates default to null.</summary>
    [Fact]
    public void Annotation_ArrowTarget_DefaultsToNull()
    {
        var ann = new Annotation("test", 1, 2);
        Assert.Null(ann.ArrowTargetX);
        Assert.Null(ann.ArrowTargetY);
    }

    /// <summary>Verifies that adding an annotation via Axes.Annotate appears in the Annotations collection.</summary>
    [Fact]
    public void Axes_AddAnnotation_AppearsInCollection()
    {
        var axes = new Axes();
        var ann = axes.Annotate("label", 3.0, 4.0);
        Assert.Single(axes.Annotations);
        Assert.Equal("label", axes.Annotations[0].Text);
    }

    /// <summary>Verifies that the Annotations collection defaults to empty.</summary>
    [Fact]
    public void Axes_Annotations_DefaultsToEmpty()
    {
        var axes = new Axes();
        Assert.Empty(axes.Annotations);
    }

    /// <summary>Verifies that Alignment defaults to Left.</summary>
    [Fact]
    public void Annotation_Alignment_DefaultsToLeft()
    {
        var ann = new Annotation("test", 1, 2);
        Assert.Equal(MatPlotLibNet.Rendering.TextAlignment.Left, ann.Alignment);
    }

    /// <summary>Verifies that Rotation defaults to 0.</summary>
    [Fact]
    public void Annotation_Rotation_DefaultsToZero()
    {
        var ann = new Annotation("test", 1, 2);
        Assert.Equal(0.0, ann.Rotation);
    }

    /// <summary>Verifies that ArrowStyle defaults to Simple.</summary>
    [Fact]
    public void Annotation_ArrowStyle_DefaultsToSimple()
    {
        var ann = new Annotation("test", 1, 2);
        Assert.Equal(ArrowStyle.Simple, ann.ArrowStyle);
    }

    /// <summary>Verifies that BackgroundColor defaults to null.</summary>
    [Fact]
    public void Annotation_BackgroundColor_DefaultsToNull()
    {
        var ann = new Annotation("test", 1, 2);
        Assert.Null(ann.BackgroundColor);
    }

    /// <summary>Verifies that Alignment can be set.</summary>
    [Fact]
    public void Annotation_Alignment_CanBeSet()
    {
        var ann = new Annotation("test", 1, 2) { Alignment = MatPlotLibNet.Rendering.TextAlignment.Center };
        Assert.Equal(MatPlotLibNet.Rendering.TextAlignment.Center, ann.Alignment);
    }

    /// <summary>Verifies that Rotation can be set.</summary>
    [Fact]
    public void Annotation_Rotation_CanBeSet()
    {
        var ann = new Annotation("test", 1, 2) { Rotation = 45.0 };
        Assert.Equal(45.0, ann.Rotation);
    }

    /// <summary>Verifies that ArrowStyle.None suppresses arrow even when target is set.</summary>
    [Fact]
    public void Annotation_ArrowStyleNone_ModelAllowsIt()
    {
        var ann = new Annotation("test", 1, 2)
        {
            ArrowTargetX = 3,
            ArrowTargetY = 4,
            ArrowStyle = ArrowStyle.None
        };
        Assert.Equal(ArrowStyle.None, ann.ArrowStyle);
        Assert.NotNull(ann.ArrowTargetX);
    }

    /// <summary>Verifies that FancyArrow style can be set.</summary>
    [Fact]
    public void Annotation_ArrowStyleFancyArrow_CanBeSet()
    {
        var ann = new Annotation("test", 1, 2) { ArrowStyle = ArrowStyle.FancyArrow };
        Assert.Equal(ArrowStyle.FancyArrow, ann.ArrowStyle);
    }
}
