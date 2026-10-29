// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Transforms;

/// <summary>The transform registered for an extension whose format core cannot write by itself. It exists so that
/// <c>Figure.Save("chart.png")</c> says what is missing instead of writing an SVG document under a raster name. It refuses
/// before it writes anything, so <c>Save</c> never opens the file and a file that was already there survives. The
/// exporters of <c>MatPlotLibNet.Skia</c> take the place of the PNG and PDF refusals when that assembly loads; the GIF
/// refusal stays, because a GIF is an animation and not a figure.</summary>
internal sealed class RefusingTransform : IFigureTransform
{
    private readonly string _message;

    private RefusingTransform(string message) => _message = message;

    /// <summary>The refusal of an extension whose exporter lives in <c>MatPlotLibNet.Skia</c>.</summary>
    internal static RefusingTransform WithoutSkia(string extension) => new(
        $"A {extension} file needs the exporters of MatPlotLibNet.Skia, which is not loaded: reference the package and call "
        + "any of its members (for example figure.ToPng()) so that it registers them. Core writes SVG and JSON.");

    /// <summary>The refusal of <c>.gif</c>: an animation is saved from its builder, not from a figure.</summary>
    internal static RefusingTransform ForAnimation() => new(
        "A .gif file is an animation, not a figure: build it with an AnimationBuilder and call SaveGif(path) from "
        + "MatPlotLibNet.Skia.");

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Always: the message names the package or the call that writes the format.</exception>
    public void Transform(Figure figure, Stream output) => throw new InvalidOperationException(_message);
}
