// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>A drawing surface that keeps what it was told in a <see cref="ThemedSurface"/>: the three surfaces of the library (SVG,
/// Skia, MAUI) hold one and say which faces they set math in, and are told and asked the looks, the math texts, the box metrics and
/// the opacity through this interface's own members, written once. A surface that forwards to another (<see cref="MarkCollectingContext"/>)
/// implements <see cref="IThemedRenderContext"/> itself.</summary>
/// <remarks>Internal, beside the public <see cref="IRenderContext"/>: no member is added to the public interface.</remarks>
internal interface ISurfaceRenderContext : IThemedRenderContext, IOpacityRenderContext
{
    /// <summary>What the surface was told, by reference: the surface holds it as a field.</summary>
    ref ThemedSurface Surface { get; }

    /// <inheritdoc />
    void IThemedRenderContext.UseLooks(ThemeLooks looks) => Surface.Looks = looks;

    /// <inheritdoc />
    ThemeLooks IThemedRenderContext.Looks => Surface.Looks;

    /// <inheritdoc />
    void IThemedRenderContext.UseMathTexts(KeptMathTexts texts) => Surface.MathTexts = texts;

    /// <inheritdoc />
    KeptMathTexts? IThemedRenderContext.MathTexts => Surface.MathTexts;

    /// <inheritdoc />
    IFontMetrics? IThemedRenderContext.BoxMetrics => Surface.BoxMetrics;

    /// <inheritdoc />
    double IOpacityRenderContext.Opacity => Surface.Opacity;
}
