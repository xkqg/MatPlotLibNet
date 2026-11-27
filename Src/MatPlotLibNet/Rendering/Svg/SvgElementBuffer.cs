// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>The markup an SVG drawing surface writes, and the one envelope every element of it is written in: what was asked of the next
/// element (its depth in a tree, a class, data for a script, the width a text is set to), the clips pushed on the surface and the paint
/// servers its shapes are filled with. The mark, text and markup writers of <see cref="SvgRenderContext"/> write into it.</summary>
/// <remarks>A clip cuts the marks drawn under it, never a text (<see cref="MarkClips{TClip}"/>): its <c>clipPath</c> is defined once, and the
/// marks are written in groups drawn in it, each closed before a text and before any other group opens or closes
/// (<see cref="OutsideClips"/>), so a group drawn in a clip only ever holds marks, and the texts a series has the surface cut
/// (<see cref="CutsTexts"/>).</remarks>
internal sealed class SvgElementBuffer : IClipSurface<string>
{
    /// <summary>A <c>data-*</c> attribute asked of the next element.</summary>
    private readonly record struct DataAttr(string Key, string Value);

    /// <summary>The scope that names the ids of the clips, hatches and gradients written here.</summary>
    private readonly SvgIdScope _ids;

    /// <summary>The clips pushed on this surface, by the id of their <c>clipPath</c>, and how many are applied.</summary>
    private MarkClips<string> _clips;

    /// <summary>How many clips were defined, for the id of the next one.</summary>
    private int _clipCount;

    /// <summary>The gradient definitions, made on the first gradient: most figures carry none.</summary>
    private SvgGradientRegistry? _gradients;

    /// <summary>The hatch definitions, made on the first hatched shape: most figures carry none.</summary>
    private SvgHatchRegistry? _hatches;

    private List<DataAttr>? _pendingData;
    private string? _pendingClass;
    private int? _pendingTextLevel;
    private double? _pendingTextLength;

    /// <summary>A buffer whose clip, hatch and gradient ids carry the prefix of <paramref name="ids"/>.</summary>
    /// <param name="ids">The scope that names the ids written here.</param>
    internal SvgElementBuffer(SvgIdScope ids) => _ids = ids;

    /// <summary>The markup written so far.</summary>
    internal StringBuilder Markup { get; } = new();

    /// <summary>Asks a <c>data-<paramref name="key"/></c> attribute of the next element.</summary>
    internal void SetNextData(string key, string value) => (_pendingData ??= []).Add(new(key, value));

    /// <summary>Asks a <c>class</c> of the next element.</summary>
    internal void SetNextClass(string className) => _pendingClass = className;

    /// <summary>Asks a depth in a tree (<c>aria-level</c>) of the next element; <see langword="null"/> takes it back.</summary>
    internal void SetNextTextLevel(int? level) => _pendingTextLevel = level;

    /// <summary>Asks that the next text be set <paramref name="width"/> wide; a width that is not a positive number asks none.</summary>
    internal void SetNextTextLength(double width) => _pendingTextLength = width.IsPositiveFinite() ? width : null;

    /// <summary>The width asked of the next text, taken: a text uses it, or an outline (which has no font to fit) drops it.</summary>
    internal double? TakeTextLength()
    {
        double? length = _pendingTextLength;
        _pendingTextLength = null;
        return length;
    }

    /// <summary>Writes a mark: the clips a text lifted are applied first, so the mark is cut by every clip pushed.</summary>
    /// <typeparam name="TMark">The kind of mark.</typeparam>
    /// <param name="mark">The mark, taken by value and written through a reference to it, so no member call copies it again (a call
    /// on a value of a type parameter passed as <see langword="in"/> copies the whole value first).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void WriteMark<TMark>(TMark mark) where TMark : struct, ISvgElement
    {
        _clips.Mark(this);
        Write(ref mark);
    }

    /// <summary>Writes a text: outside every clip, or, while the texts are cut (<see cref="CutsTexts"/>), inside them as a mark is.</summary>
    /// <typeparam name="TElement">The kind of text element: a text element or a glyph outline.</typeparam>
    /// <param name="element">The element, taken by value as a mark is.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void WriteText<TElement>(TElement element) where TElement : struct, ISvgElement
    {
        _clips.Text(this);
        Write(ref element);
    }

    /// <summary>Whether a text written now is cut by the clips as a mark is (<see cref="MarkClips{TClip}.CutsTexts"/>).</summary>
    internal bool CutsTexts
    {
        get => _clips.CutsTexts;
        set => _clips.CutsTexts = value;
    }

    /// <summary>Writes an element no clip cuts: a group the browser reads.</summary>
    /// <typeparam name="TElement">The kind of element.</typeparam>
    /// <param name="element">The element, taken by value as a mark is.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void WriteOutsideClips<TElement>(TElement element) where TElement : struct, ISvgElement
    {
        OutsideClips();
        Write(ref element);
    }

    /// <summary>The markup, after every applied clip is closed: where a text or a group is written.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal StringBuilder OutsideClips()
    {
        _clips.Lift(this);
        return Markup;
    }

    /// <summary>The one envelope of an element: its opening, the attributes asked of the next element, its close.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Write<TElement>(ref TElement element) where TElement : struct, ISvgElement
    {
        element.WriteOpening(this);
        AppendAskedAttributes();
        element.WriteClosing(Markup);
    }

    /// <summary>Writes what was asked of the next element (its depth in a tree, its class, its data), each forgotten once written.
    /// Not generic, so the kinds of element share its code.</summary>
    private void AppendAskedAttributes()
    {
        if (_pendingTextLevel is { } level)
        {
            Markup.Append(" aria-level=\"").AppendInvariant(level).Append('"');
            _pendingTextLevel = null;
        }
        if (_pendingClass is not null)
        {
            Markup.Append(" class=\"").Append(_pendingClass).Append('"');
            _pendingClass = null;
        }
        if (_pendingData is { Count: > 0 } data)
        {
            foreach (var attr in data)
                Markup.Append(" data-").Append(attr.Key).Append("=\"").Append(attr.Value.EscapeForXml()).Append('"');
            data.Clear();
        }
    }

    /// <summary>Defines the <c>clipPath</c> of <paramref name="outline"/> under a new id and pushes it
    /// (<see cref="MarkClips{TClip}.Push"/>): the group drawn in it opens at once when every clip before it is applied, else before the
    /// next mark; <see cref="PopClip"/> closes it.</summary>
    /// <typeparam name="TOutline">The kind of shape the clip has.</typeparam>
    /// <param name="outline">The shape of the clip.</param>
    internal void PushClip<TOutline>(TOutline outline) where TOutline : struct, ISvgGeometry
    {
        string id = _ids.Clip(_clipCount++);
        Markup.Append("<defs><clipPath id=\"").Append(id).Append("\">");
        outline.WriteGeometry(Markup);
        Markup.AppendLineLf(" /></clipPath></defs>");
        _clips.Push(this, id);
    }

    /// <summary>Pops the innermost clip, closing its group when it is applied.</summary>
    internal void PopClip() => _clips.Pop(this);

    /// <inheritdoc />
    /// <remarks>Opens the group drawn in the clip's <c>clipPath</c>.</remarks>
    void IClipSurface<string>.Apply(string clip) => Markup.Append("<g clip-path=\"url(#").Append(clip).AppendLineLf(")\">");

    /// <inheritdoc />
    /// <remarks>Closes the group drawn in the clip.</remarks>
    void IClipSurface<string>.Lift() => Markup.AppendLineLf("</g>");

    /// <summary>The id of the paint server a shape is filled with, defined on first use: its gradient, its hatch pattern, or none for a
    /// flat fill. Asked BEFORE the shape's opening tag: the definition is written into the same markup. A shape with both a gradient
    /// and a hatch is drawn in two layers (<see cref="ShapeStyleLayers"/>) before it gets here.</summary>
    /// <param name="shape">The fill of the shape.</param>
    /// <returns>The id, without the <c>#</c>, or <see langword="null"/> for a flat fill.</returns>
    internal string? PaintServerOf(in ShapeStyle shape)
    {
        if (shape.HasVisibleGradient) return (_gradients ??= new SvgGradientRegistry(Markup, _ids)).RegisterShared(shape.Gradient!.Value);
        if (!shape.HasVisibleHatch) return null;
        return (_hatches ??= new SvgHatchRegistry(Markup, _ids)).Register(shape.Hatch, shape.Fill!.Value, shape.HatchStroke);
    }

    /// <summary>
    /// Owns SVG gradient-id generation and <c>&lt;defs&gt;&lt;linearGradient&gt;</c> emission. The element buffer of an SVG surface
    /// (<see cref="SvgElementBuffer"/>) makes one on the first gradient and asks it for the id of every gradient fill: a single
    /// responsibility, linear-gradient defs, with real state (the id counter and the shared markup).
    /// </summary>
    /// <remarks>
    /// <para>The registry writes directly to the buffer's <see cref="StringBuilder"/> so the
    /// output ordering is fixed: a gradient's defs stand where the buffer writes them, before the tag of the shape it fills.</para>
    /// </remarks>
    internal sealed class SvgGradientRegistry
    {
        private readonly StringBuilder _sb;
        private readonly SvgIdScope _ids;
        private readonly Dictionary<LinearGradient, string> _shared = [];
        private int _gradientId;

        /// <summary>Constructs a registry that writes gradient defs to the supplied buffer.</summary>
        /// <param name="sb">The shared markup (the hosting <see cref="SvgElementBuffer"/>'s).</param>
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
        /// <c>gradientUnits="userSpaceOnUse"</c> is emitted so the gradient is anchored to the pixel-space points it names (a Sankey
        /// link's source-to-target blend runs along the link's own box). A shape's fill reaches it through <see cref="Rendering.Svg.SvgElementBuffer.SvgGradientRegistry.RegisterShared"/>.
        /// </remarks>
        public string Register(Color from, Color to,
            double x1, double y1, double x2, double y2)
        {
            string refId = _ids.Gradient(_gradientId++);
            _sb.Append("<defs><linearGradient id=\"").Append(refId)
               .Append("\" gradientUnits=\"userSpaceOnUse\" x1=\"").AppendSvgNumber(x1)
               .Append("\" y1=\"").AppendSvgNumber(y1).Append("\" x2=\"").AppendSvgNumber(x2)
               .Append("\" y2=\"").AppendSvgNumber(y2).Append("\">");
            AppendStop(0, from);
            AppendStop(1, to);
            _sb.AppendLineLf("</linearGradient></defs>");
            return refId;
        }

        /// <summary>The id of the gradient of a shape's fill, defining it on first use: shapes that carry the same gradient share one
        /// definition, as identical hatches do, instead of the one-per-call of <see cref="Rendering.Svg.SvgElementBuffer.SvgGradientRegistry.Register"/>.</summary>
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
            _sb.Append("<stop offset=\"").AppendExactSvgNumber(offset)
               .Append("\" stop-color=\"").AppendHex(color).Append('"')
               .AppendAlpha("stop-opacity", color)
               .Append(" />");
        }
    }

    /// <summary>
    /// Owns SVG hatch-pattern id generation and <c>&lt;defs&gt;&lt;pattern&gt;</c> emission — the exact
    /// shape of <see cref="Rendering.Svg.SvgElementBuffer.SvgGradientRegistry"/>, its sibling: an SRP collaborator made by
    /// <see cref="SvgElementBuffer"/> on the first hatched shape, holding the id counter and writing into the buffer's shared markup.
    /// </summary>
    /// <remarks>
    /// <para>Hatches are <b>de-duplicated</b>, as the gradients of shapes are: a hatch is a series-wide style repeated across every
    /// mark of that series, and emitting a fresh <c>&lt;pattern&gt;</c> per bar would bloat the document linearly with the data.
    /// Identical (pattern, fill, hatch-colour) triples therefore share one definition.</para>
    /// <para>The pattern tile carries the base fill as a background rect and the hatch strokes on top of it, so the
    /// element's own <c>fill</c> attribute becomes a single <c>url(#…)</c> reference and the two colours stay
    /// bound together — a hatch never drifts off the fill it belongs to.</para>
    /// </remarks>
    private sealed class SvgHatchRegistry
    {
        /// <summary>Tile size in user units. Six pixels reads as a hatch at a glance without moiré at chart scale.</summary>
        private const double Tile = HatchTile.Size;

        /// <summary>What makes two patterns the same one: the hatch and the two colours it is painted in.</summary>
        private readonly record struct HatchKey(HatchPattern Hatch, Color Fill, Color Stroke);

        private readonly StringBuilder _sb;
        private readonly SvgIdScope _scope;
        private readonly Dictionary<HatchKey, string> _ids = [];
        private int _hatchId;

        /// <summary>Constructs a registry that writes pattern defs to the supplied buffer.</summary>
        /// <param name="sb">The shared markup (the hosting <see cref="SvgElementBuffer"/>'s).</param>
        /// <param name="ids">The scope that names the patterns; none for the first subplot of a figure.</param>
        public SvgHatchRegistry(StringBuilder sb, SvgIdScope ids = default)
        {
            _sb = sb;
            _scope = ids;
        }

        /// <summary>Returns the id of a pattern painting <paramref name="hatch"/> in <paramref name="hatchColor"/> over
        /// <paramref name="fill"/>, emitting the definition on first use. Callers reference it as <c>url(#id)</c>.</summary>
        /// <param name="hatch">The hatch pattern. <see cref="HatchPattern.None"/> is never passed here.</param>
        /// <param name="fill">The base fill the hatch is painted over.</param>
        /// <param name="hatchColor">The stroke colour of the hatch lines.</param>
        /// <returns>The pattern id, without the <c>#</c> prefix.</returns>
        public string Register(HatchPattern hatch, Color fill, Color hatchColor)
        {
            var key = new HatchKey(hatch, fill, hatchColor);
            if (_ids.TryGetValue(key, out string? existing))
            {
                return existing;
            }

            string refId = _scope.Hatch(_hatchId++);
            _ids[key] = refId;

            _sb.Append("<defs><pattern id=\"").Append(refId)
               .Append("\" patternUnits=\"userSpaceOnUse\" width=\"").AppendSvgNumber(Tile)
               .Append("\" height=\"").AppendSvgNumber(Tile).Append("\">");
            if (!fill.IsNone)
            {
                // A fill that is not there (a hatch laid over a gradient) leaves the tile to the strokes alone.
                _sb.Append("<rect width=\"").AppendSvgNumber(Tile)
                   .Append("\" height=\"").AppendSvgNumber(Tile)
                   .Append("\" fill=\"").AppendHex(fill).Append('"')
                   .AppendAlpha("fill-opacity", fill)
                   .Append(" />");
            }

            AppendStrokes(hatch, hatchColor);

            _sb.AppendLineLf("</pattern></defs>");
            return refId;
        }

        /// <summary>Emits the marks of one tile (<see cref="HatchTile"/>, the one place they are written; the raster and MAUI
        /// backends draw the same ones). The diagonals are drawn twice, offset by a tile, so the strokes meet seamlessly where
        /// adjacent tiles abut.</summary>
        private void AppendStrokes(HatchPattern hatch, Color color)
        {
            foreach (var mark in HatchTile.MarksOf(hatch))
            {
                if (mark.Kind == HatchMarkKind.Dot)
                {
                    AppendDot(mark.X1, mark.Y1, color);
                }
                else
                {
                    AppendLine(mark.X1, mark.Y1, mark.X2, mark.Y2, color);
                }
            }
        }

        /// <summary>A dot of the hatch, filled and stroked in its one colour: a translucent colour writes its alpha as
        /// <c>opacity</c> (<see cref="SvgWriterExtensions.AppendAlpha"/>), which fades the dot as one, so its fill and its stroke
        /// do not add up where they overlap.</summary>
        private void AppendDot(double cx, double cy, Color color)
        {
            _sb.Append("<circle cx=\"").AppendSvgNumber(cx)
               .Append("\" cy=\"").AppendSvgNumber(cy)
               .Append("\" r=\"1\" fill=\"").AppendHex(color)
               .Append("\" stroke=\"").AppendHex(color).Append('"')
               .AppendAlpha("opacity", color)
               .Append(" />");
        }

        /// <summary>A line of the hatch, a stroke drawn from a colour, written by <see cref="SvgWriterExtensions.AppendStroke"/> as
        /// every line of the chart is: a translucent colour writes <c>stroke-opacity</c> after the width.</summary>
        private void AppendLine(double x1, double y1, double x2, double y2, Color color)
        {
            _sb.Append("<line x1=\"").AppendSvgNumber(x1)
               .Append("\" y1=\"").AppendSvgNumber(y1)
               .Append("\" x2=\"").AppendSvgNumber(x2)
               .Append("\" y2=\"").AppendSvgNumber(y2).Append('"')
               .AppendStroke(StrokeStyle.Solid(color, LineWidthRule.OwnPixel.Resting), DashLook.Library)
               .Append(" />");
        }
    }
}
