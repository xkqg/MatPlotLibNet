// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Benchmarks.Drag;

/// <summary>How a frame of the drag is drawn.</summary>
public enum DragPaint
{
    /// <summary>The figure written as an SVG document, which is what a browser viewer is sent.</summary>
    Svg,

    /// <summary>The figure drawn straight onto a Skia canvas through the library's renderer.</summary>
    Skia,

    /// <summary>The figure painted the way a native control paints it, through the Skia painter that keeps the subplots which did
    /// not change.</summary>
    Control,
}

/// <summary>One mode of the drag run: what draws the frame, and what is on the figure. Named on the command line as the paint
/// (<c>svg</c>, <c>skia</c>, <c>ctl</c>), then <c>1</c> for the surface alone or <c>4</c> for the surface with three line panels,
/// and for the SVG paint a trailing <c>g</c> loads the Skia package first, so text is written as glyph outlines.</summary>
/// <param name="Paint">What draws the frame.</param>
/// <param name="WithPanels">Whether three line panels sit beside the surface.</param>
/// <param name="SkiaLoaded">Whether the Skia package is loaded for an SVG paint; any other paint loads it anyway.</param>
public readonly record struct DragMode(DragPaint Paint, bool WithPanels, bool SkiaLoaded)
{
    /// <summary>Reads a mode from its command-line name.</summary>
    /// <param name="text">The name, for example <c>skia4</c> or <c>svg4g</c>.</param>
    /// <param name="mode">The mode, when the name is one.</param>
    /// <returns>Whether <paramref name="text"/> names a mode.</returns>
    public static bool TryParse(string text, out DragMode mode)
    {
        mode = default;
        var loaded = text.EndsWith('g');
        var name = loaded ? text[..^1] : text;
        if (name.Length == 0 || name[^1] is not ('1' or '4')) return false;

        var paint = name[..^1] switch
        {
            "svg" => DragPaint.Svg,
            "skia" => DragPaint.Skia,
            "ctl" => DragPaint.Control,
            _ => (DragPaint?)null,
        };
        if (paint is null) return false;

        mode = new DragMode(paint.Value, name[^1] == '4', loaded || paint != DragPaint.Svg);
        return true;
    }
}
