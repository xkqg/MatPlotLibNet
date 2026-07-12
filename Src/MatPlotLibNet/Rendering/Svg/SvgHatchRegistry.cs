// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>
/// Owns SVG hatch-pattern id generation and <c>&lt;defs&gt;&lt;pattern&gt;</c> emission — the exact
/// shape of <see cref="SvgGradientRegistry"/>, its sibling: an SRP collaborator composed into
/// <see cref="SvgRenderContext"/>, holding the id counter and writing into the context's shared buffer.
/// </summary>
/// <remarks>
/// <para>One deliberate difference from the gradient registry: hatches are <b>de-duplicated</b>. A gradient
/// is allocated per call because each Sankey link paints its own source-to-target blend, so two calls are two
/// gradients. A hatch is a series-wide style repeated across every mark of that series — emitting a fresh
/// <c>&lt;pattern&gt;</c> per bar would bloat the document linearly with the data. Identical
/// (pattern, fill, hatch-colour) triples therefore share one definition.</para>
/// <para>The pattern tile carries the base fill as a background rect and the hatch strokes on top of it, so the
/// element's own <c>fill</c> attribute becomes a single <c>url(#…)</c> reference and the two colours stay
/// bound together — a hatch never drifts off the fill it belongs to.</para>
/// </remarks>
internal sealed class SvgHatchRegistry
{
    /// <summary>Tile size in user units. Six pixels reads as a hatch at a glance without moiré at chart scale.</summary>
    private const double Tile = 6.0;

    private readonly StringBuilder _sb;
    private readonly Dictionary<(HatchPattern Hatch, Color Fill, Color Stroke), string> _ids = [];
    private int _hatchId;

    /// <summary>Constructs a registry that writes pattern defs to the supplied buffer.</summary>
    /// <param name="sb">The shared output buffer (the hosting <see cref="SvgRenderContext"/>'s StringBuilder).</param>
    public SvgHatchRegistry(StringBuilder sb)
    {
        _sb = sb;
    }

    /// <summary>Returns the id of a pattern painting <paramref name="hatch"/> in <paramref name="hatchColor"/> over
    /// <paramref name="fill"/>, emitting the definition on first use. Callers reference it as <c>url(#id)</c>.</summary>
    /// <param name="hatch">The hatch pattern. <see cref="HatchPattern.None"/> is never passed here.</param>
    /// <param name="fill">The base fill the hatch is painted over.</param>
    /// <param name="hatchColor">The stroke colour of the hatch lines.</param>
    /// <returns>The pattern id, without the <c>#</c> prefix.</returns>
    public string Register(HatchPattern hatch, Color fill, Color hatchColor)
    {
        var key = (hatch, fill, hatchColor);
        if (_ids.TryGetValue(key, out string? existing))
        {
            return existing;
        }

        string refId = $"hatch-{_hatchId++}";
        _ids[key] = refId;

        _sb.Append("<defs><pattern id=\"").Append(refId)
           .Append("\" patternUnits=\"userSpaceOnUse\" width=\"").Append(Tile.ToSvgNumber())
           .Append("\" height=\"").Append(Tile.ToSvgNumber()).Append("\">")
           .Append("<rect width=\"").Append(Tile.ToSvgNumber())
           .Append("\" height=\"").Append(Tile.ToSvgNumber())
           .Append("\" fill=\"").Append(fill.ToHex()).Append("\" />");

        AppendStrokes(hatch, hatchColor);

        _sb.AppendLine("</pattern></defs>");
        return refId;
    }

    /// <summary>Emits the line geometry of one tile. Diagonals are drawn twice, offset by a tile, so the
    /// strokes meet seamlessly where adjacent tiles abut.</summary>
    private void AppendStrokes(HatchPattern hatch, Color color)
    {
        switch (hatch)
        {
            case HatchPattern.ForwardDiagonal:
                AppendLine(0, Tile, Tile, 0, color);
                AppendLine(-1, 1, 1, -1, color);
                AppendLine(Tile - 1, Tile + 1, Tile + 1, Tile - 1, color);
                break;

            case HatchPattern.BackDiagonal:
                AppendLine(0, 0, Tile, Tile, color);
                AppendLine(-1, Tile - 1, 1, Tile + 1, color);
                AppendLine(Tile - 1, -1, Tile + 1, 1, color);
                break;

            case HatchPattern.Horizontal:
                AppendLine(0, Tile / 2, Tile, Tile / 2, color);
                break;

            case HatchPattern.Vertical:
                AppendLine(Tile / 2, 0, Tile / 2, Tile, color);
                break;

            case HatchPattern.Cross:
                AppendLine(0, Tile / 2, Tile, Tile / 2, color);
                AppendLine(Tile / 2, 0, Tile / 2, Tile, color);
                break;

            case HatchPattern.DiagonalCross:
                AppendLine(0, Tile, Tile, 0, color);
                AppendLine(0, 0, Tile, Tile, color);
                break;

            case HatchPattern.Dots:
                AppendDot(Tile / 2, Tile / 2, color);
                break;

            case HatchPattern.Stars:
                AppendDot(Tile / 2, Tile / 2, color);
                AppendLine(0, Tile, Tile, 0, color);
                AppendLine(0, 0, Tile, Tile, color);
                break;

            default:
                // An out-of-range cast paints the background only — a hatch nobody defined draws nothing,
                // rather than throwing inside a render pass. HatchPattern.None never reaches here (guarded
                // by ShapeStyle.HasVisibleHatch).
                break;
        }
    }

    private void AppendDot(double cx, double cy, Color color)
    {
        _sb.Append("<circle cx=\"").Append(cx.ToSvgNumber())
           .Append("\" cy=\"").Append(cy.ToSvgNumber())
           .Append("\" r=\"1\" fill=\"").Append(color.ToHex())
           .Append("\" stroke=\"").Append(color.ToHex()).Append("\" />");
    }

    private void AppendLine(double x1, double y1, double x2, double y2, Color color)
    {
        _sb.Append("<line x1=\"").Append(x1.ToSvgNumber())
           .Append("\" y1=\"").Append(y1.ToSvgNumber())
           .Append("\" x2=\"").Append(x2.ToSvgNumber())
           .Append("\" y2=\"").Append(y2.ToSvgNumber())
           .Append("\" stroke=\"").Append(color.ToHex())
           .Append("\" stroke-width=\"1\" />");
    }
}
