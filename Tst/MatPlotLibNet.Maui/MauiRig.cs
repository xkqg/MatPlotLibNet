// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Maui.Graphics;
using NSubstitute;

namespace MatPlotLibNet.Maui.Tests;

/// <summary>A substitute canvas and the render context drawn onto it, which a test reads back the calls on the canvas from.</summary>
/// <param name="Canvas">The canvas the context draws onto; it records every call.</param>
/// <param name="Context">The render context over <paramref name="Canvas"/>.</param>
public readonly record struct MauiRig(ICanvas Canvas, MauiGraphicsRenderContext Context)
{
    /// <summary>Makes a canvas that records every call, and a context over it.</summary>
    public static MauiRig Fresh()
    {
        var canvas = Substitute.For<ICanvas>();
        return new MauiRig(canvas, new MauiGraphicsRenderContext(canvas));
    }
}
