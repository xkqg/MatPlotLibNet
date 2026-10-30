// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>
/// Owns SVG gradient-id generation and <c>&lt;defs&gt;&lt;linearGradient&gt;</c> emission.
/// Extracted from <see cref="SvgRenderContext"/> via composition; the render context
/// holds an instance of this collaborator and delegates both id allocation and output
/// writing. This is an SRP class — single responsibility = linear-gradient defs — with
/// real state (the id counter + the shared output buffer).
/// </summary>
/// <remarks>
/// <para>The registry writes directly to the context's <see cref="StringBuilder"/> so the
/// output ordering is fixed: a gradient's defs stand where the context writes them (matters for
/// the matplotlib fidelity suite, which compares rendered pixels but is sensitive to defs
/// placement for browsers that process them lazily).</para>
/// </remarks>
internal sealed class SvgGradientRegistry
{
    private readonly StringBuilder _sb;
    private readonly SvgIdScope _ids;
    private readonly Dictionary<LinearGradient, string> _shared = [];
    private int _gradientId;

    /// <summary>Constructs a registry that writes gradient defs to the supplied buffer.</summary>
    /// <param name="sb">The shared output buffer (typically the hosting
    /// <see cref="SvgRenderContext"/>'s internal StringBuilder).</param>
    /// <param name="ids">The scope that names the gradients; none for the first subplot of a figure.</param>
    public SvgGradientRegistry(StringBuilder sb, SvgIdScope ids = default)
    {
        _sb = sb;
        _ids = ids;
    }

    /// <summary>
    /// Allocates a new gradient id, appends a
    /// <c>&lt;defs&gt;&lt;linearGradient id="grad-N" …&gt;…&lt;/linearGradient&gt;&lt;/defs&gt;</c>
    /// block to the buffer, and returns the allocated id (without the <c>#</c> prefix).
    /// Callers reference it as <c>url(#grad-N)</c> in subsequent <c>fill=</c> attributes.
    /// </summary>
    /// <remarks>
    /// <c>gradientUnits="userSpaceOnUse"</c> is emitted so callers can anchor the
    /// gradient to an absolute pixel-space box (used by the Sankey link renderer to
    /// paint source→target colour blends along each link's bounding box).
    /// </remarks>
    public string Register(Color from, Color to,
        double x1, double y1, double x2, double y2)
    {
        string refId = _ids.Gradient(_gradientId++);
        _sb.Append("<defs><linearGradient id=\"").Append(refId)
           .Append("\" gradientUnits=\"userSpaceOnUse\" x1=\"").Append(x1.ToSvgNumber())
           .Append("\" y1=\"").Append(y1.ToSvgNumber()).Append("\" x2=\"").Append(x2.ToSvgNumber())
           .Append("\" y2=\"").Append(y2.ToSvgNumber()).Append("\">");
        AppendStop(0, from);
        AppendStop(1, to);
        _sb.AppendLine("</linearGradient></defs>");
        return refId;
    }

    /// <summary>The id of the gradient of a shape's fill, defining it on first use: shapes that carry the same gradient share one
    /// definition, as identical hatches do, instead of the one-per-call of <see cref="Register"/>.</summary>
    /// <param name="gradient">The gradient the shape is filled with.</param>
    /// <returns>The gradient id, without the <c>#</c> prefix.</returns>
    public string RegisterShared(LinearGradient gradient)
    {
        if (_shared.TryGetValue(gradient, out string? existing))
        {
            return existing;
        }

        string id = Register(gradient.StartColor, gradient.EndColor, gradient.Start.X, gradient.Start.Y, gradient.End.X, gradient.End.Y);
        _shared[gradient] = id;
        return id;
    }

    private void AppendStop(double offset, Color color)
    {
        _sb.Append("<stop offset=\"").Append(offset.ToSvgNumber())
           .Append("\" stop-color=\"").Append(color.ToHex()).Append('"');
        if (color.A < 255)
            _sb.Append(" stop-opacity=\"").Append((color.A / 255.0).ToSvgNumber()).Append('"');
        _sb.Append(" />");
    }
}
