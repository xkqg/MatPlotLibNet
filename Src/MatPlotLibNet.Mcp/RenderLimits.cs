// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json.Nodes;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Mcp;

/// <summary>The bounds a model-authored spec must stay inside. Nothing in the library clamps a canvas — a
/// 16000×16000 request allocates a bitmap of a gigabyte and a larger one fails on allocation — and the PNG goes
/// into a context window, so the canvas ceiling is the token ceiling too. The text limit keeps a 20,000-character
/// title from becoming a multi-megabyte document. The limits apply themselves to a document the spec reader has parsed: a
/// size, a picture or a text over its ceiling is a <see cref="ToolRefusalException"/> naming the field.</summary>
/// <param name="MaxWidth">Largest accepted figure width, in pixels.</param>
/// <param name="MaxHeight">Largest accepted figure height, in pixels.</param>
/// <param name="MaxTextLength">Longest accepted title, label or description.</param>
/// <param name="MaxTableRows">Most rows <c>chart_data_table</c> writes out. A table is text, and text is the
/// scarce thing here: a ten-thousand-row answer costs more context than the chart it describes.</param>
internal readonly record struct RenderLimits(int MaxWidth, int MaxHeight, int MaxTextLength, int MaxTableRows)
{
    /// <summary>The limits the server ships with.</summary>
    public static RenderLimits Default => new(4000, 4000, 512, 500);

    /// <summary>Gives a figure that names no <paramref name="field"/> its <paramref name="fallback"/>, and refuses one that names a number
    /// outside 1 to <paramref name="max"/>. A value that is not a number is left for the strict pass, which names it.</summary>
    /// <param name="figure">The figure object of the document.</param>
    /// <param name="field">The size to check: <c>width</c>, <c>height</c> or <c>dpi</c>.</param>
    /// <param name="fallback">What a figure that names no size gets.</param>
    /// <param name="max">The largest accepted value.</param>
    /// <exception cref="ToolRefusalException">The size is below 1 or above <paramref name="max"/>.</exception>
    internal void EnsureSize(JsonObject figure, string field, double fallback, int max)
    {
        var node = figure[field];
        if (node is null)
        {
            figure[field] = fallback;
            return;
        }

        if (node is JsonValue value && value.TryGetValue<double>(out var size) && !(size >= 1 && size <= max))
        {
            throw new ToolRefusalException(
                string.Invariant($"'{field}' must be between 1 and {max} (got {size.InvariantText()}): the picture goes into a context window."));
        }
    }

    /// <summary>A PNG is drawn with <c>dpi / 96</c> pixels for each unit of the figure, so the canvas ceiling holds for
    /// the picture the spec draws, not for its width and height alone: a figure inside the ceiling at 96 dpi can leave it
    /// at 300, and one at 100000 dpi is a bitmap no machine allocates. The picture goes into a context window, so the
    /// ceiling is the picture's, in pixels. A figure whose width, height or dpi is not a number is left for the strict pass.</summary>
    /// <param name="figure">The figure object of the document, after its sizes are ensured.</param>
    /// <exception cref="ToolRefusalException">The picture the spec draws is wider or taller than the ceiling.</exception>
    internal void EnsurePictureFits(JsonObject figure)
    {
        if (!TryNumber(figure, "width", out double width)
            || !TryNumber(figure, "height", out double height)
            || !TryNumber(figure, "dpi", out double dpi))
        {
            return;
        }

        double pixelsAcross = width * dpi / Figure.ReferenceDpi;
        double pixelsDown = height * dpi / Figure.ReferenceDpi;
        if (pixelsAcross > MaxWidth || pixelsDown > MaxHeight)
        {
            throw new ToolRefusalException(
                $"'dpi' {dpi.InvariantText()} draws the picture {Math.Ceiling(pixelsAcross).InvariantText()} by "
                + string.Invariant($"{Math.Ceiling(pixelsDown).InvariantText()} pixels (width and height times dpi / {Figure.ReferenceDpi}); ")
                + string.Invariant($"the ceiling is {MaxWidth} by {MaxHeight}: the picture goes into a context window. Lower the dpi or the size."));
        }
    }

    /// <summary>Refuses a text longer than <see cref="MaxTextLength"/>; a field that is absent or holds no text passes.</summary>
    /// <param name="node">The object that holds the field.</param>
    /// <param name="path">Where <paramref name="node"/> stands, as a refusal names it.</param>
    /// <param name="field">The text field to check.</param>
    /// <exception cref="ToolRefusalException">The text is longer than the limit.</exception>
    internal void CheckText(JsonObject node, string path, string field)
    {
        if (node[field] is JsonValue value && value.TryGetValue<string>(out var text) && text.Length > MaxTextLength)
        {
            throw new ToolRefusalException(string.Invariant($"'{field}' at {path} is {text.Length} characters long; the limit is {MaxTextLength}."));
        }
    }

    private static bool TryNumber(JsonObject node, string field, out double value)
    {
        value = 0;
        return node[field] is JsonValue json && json.TryGetValue(out value);
    }
}
