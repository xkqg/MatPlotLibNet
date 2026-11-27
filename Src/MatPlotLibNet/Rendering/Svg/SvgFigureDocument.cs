// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>An SVG document of a figure in the parts it is written from: the root element's opening tag, the title and the
/// description, the background (with the figure's title and what it draws under its subplots), one part per subplot, what the
/// figure draws above them, and the scripts. <see cref="ToSvg"/> writes the document <c>ToSvg</c> has always written;
/// <see cref="ToWrappedSvg"/> writes it with every drawn part in a group of its own, named by its <see cref="SvgPartMarker"/>
/// (<c>background</c>, <c>axes-0</c>, <c>axes-1</c>, …, <c>overlay</c>), so a receiver can replace one part without the
/// rest.</summary>
internal sealed class SvgFigureDocument
{
    internal SvgFigureDocument(string rootTag, string headBody, SvgRenderContext background, IReadOnlyList<SvgRenderContext> axes,
        SvgRenderContext overlay, IReadOnlyList<string> scripts)
    {
        RootTag = rootTag;
        HeadBody = headBody;
        Background = background;
        Axes = axes;
        Overlay = overlay;
        Scripts = scripts;
    }

    /// <summary>The opening tag of the root element, with its line break: its attributes carry the size, the role and, for a
    /// figure with server interaction, the view of the first subplot, which a zoom or a stream changes.</summary>
    internal string RootTag { get; }

    /// <summary>The title and the description, as they follow the root element.</summary>
    internal string HeadBody { get; }

    /// <summary>The background, the figure's title and what the figure draws under its subplots.</summary>
    internal SvgRenderContext Background { get; }

    /// <summary>One context per subplot, in order. A subplot nothing changed in since the figure's last draw is the same
    /// context as in the document of that draw.</summary>
    internal IReadOnlyList<SvgRenderContext> Axes { get; }

    /// <summary>What the figure draws above its subplots: its colour bar, its texts, its legend.</summary>
    internal SvgRenderContext Overlay { get; }

    /// <summary>The scripts, in the order they are written, each without its line break.</summary>
    internal IReadOnlyList<string> Scripts { get; }

    /// <summary>The document as <c>ToSvg</c> writes it.</summary>
    internal string ToSvg() => Write(wrapped: false);

    /// <summary>The document with every drawn part in a named group of its own.</summary>
    internal string ToWrappedSvg() => Write(wrapped: true);

    /// <summary>The background in its named group, as <see cref="ToWrappedSvg"/> writes it.</summary>
    internal string WrappedBackground() => Wrapped("background", Background);

    /// <summary>The subplot at <paramref name="index"/> in its named group, as <see cref="ToWrappedSvg"/> writes it.</summary>
    internal string WrappedAxes(int index) => Wrapped(AxesPartName(index), Axes[index]);

    /// <summary>What the figure draws above its subplots in its named group, as <see cref="ToWrappedSvg"/> writes it.</summary>
    internal string WrappedOverlay() => Wrapped("overlay", Overlay);

    /// <summary>The name of the part of the subplot at <paramref name="index"/>.</summary>
    internal static string AxesPartName(int index) => "axes-" + index.InvariantText();

    private static string Wrapped(string name, SvgRenderContext part)
    {
        var parts = new SvgDocumentParts();
        AddWrapped(parts, name, part);
        return parts.ToString();
    }

    private static void AddWrapped(SvgDocumentParts parts, string name, SvgRenderContext part)
    {
        parts.AppendLineLf(SvgPartMarker.Start(name));
        parts.Add(part);
        parts.Add(SvgPartMarker.Closing);
    }

    private string Write(bool wrapped)
    {
        var parts = new SvgDocumentParts();
        parts.Add(RootTag);
        parts.Add(HeadBody);
        if (wrapped) AddWrapped(parts, "background", Background);
        else parts.Add(Background);
        for (int i = 0; i < Axes.Count; i++)
        {
            if (wrapped) AddWrapped(parts, AxesPartName(i), Axes[i]);
            else parts.Add(Axes[i]);
        }

        if (wrapped) AddWrapped(parts, "overlay", Overlay);
        else parts.Add(Overlay);
        foreach (var script in Scripts) parts.AppendLineLf(script);
        parts.AppendLineLf("</svg>");
        return parts.ToString();
    }

    /// <summary>
    /// The pieces of one SVG document in the order they appear — the root element, the markup each render context wrote, the
    /// scripts — written into the finished string in ONE copy. Appending every piece to a builder first and then asking that
    /// builder for its string copied the whole document twice, and on a live chart redrawn several times a second that copy,
    /// and the memory it took, cost more than writing the markup did.
    /// </summary>
    internal sealed class SvgDocumentParts
    {
        private readonly List<object> _parts = [];

        /// <summary>Adds what <paramref name="builder"/> holds; it is read when the document is made, not now.</summary>
        /// <param name="builder">The markup to add.</param>
        public void Add(StringBuilder builder) => _parts.Add(builder);

        /// <summary>Adds the markup <paramref name="context"/> has written.</summary>
        /// <param name="context">The render context whose markup to add.</param>
        public void Add(SvgRenderContext context) => Add(context.Markup);

        /// <summary>Adds <paramref name="text"/> as it is.</summary>
        /// <param name="text">The text to add.</param>
        public void Add(string text) => _parts.Add(text);

        /// <summary>Adds <paramref name="text"/> followed by the library's line end, <see cref="LineEndExtensions.LineEnd"/>.</summary>
        /// <param name="text">The text to add.</param>
        public void AppendLineLf(string text)
        {
            _parts.Add(text);
            _parts.Add(LineEndExtensions.LineEnd);
        }

        /// <summary>The document: every part, in order, copied once into a string of exactly the right length.</summary>
        public override string ToString() => string.Create(Length(), _parts, static (target, parts) =>
        {
            int at = 0;
            foreach (var part in parts)
            {
                if (part is string text)
                {
                    text.AsSpan().CopyTo(target[at..]);
                    at += text.Length;
                    continue;
                }

                foreach (var chunk in ((StringBuilder)part).GetChunks())
                {
                    chunk.Span.CopyTo(target[at..]);
                    at += chunk.Length;
                }
            }
        });

        private int Length()
        {
            int length = 0;
            foreach (var part in _parts)
            {
                length += part is string text ? text.Length : ((StringBuilder)part).Length;
            }

            return length;
        }
    }
}
