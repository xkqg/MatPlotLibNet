// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using System.Collections;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Mcp;

/// <summary>The one door between a model-authored document and the library's serializer. <c>ChartSerializer.FromJson</c>
/// is a round-trip reader for its own writer: it drops an unknown series type leniently, skips an unknown property,
/// leaves out a misspelled enum name of a text field (it only announces it) and overwrites the figure defaults with zero when a size is absent. Handed to a
/// model every one of those turns a typo into a blank picture reported as success. So the reader refuses first, by
/// field and path, and only a document it has normalised reaches <see cref="IChartSerializer.FromJson"/>.</summary>
/// <remarks>Order: parse → normalise (defaults, colours, enum spelling, bounds, text lengths) → strict structural pass
/// over the SAME DTO records the serializer reads (an unknown field is a refusal with its path) → semantic pass
/// (types against the catalog, array lengths) → the serializer → the figure it built must show something (a series, an overlay of
/// a subplot, or a text or an image on the figure itself). A subplot keeps its axes and series at many seats (the
/// primary pair, the top and right axes, every Y axis slot of either side): the normalising and the semantic pass both walk
/// them through <see cref="AxesSeatShape"/>, so a check reaches every seat and a refusal names the seat's path.</remarks>
internal sealed partial class ChartSpecReader
{
    private const double DefaultWidth = Figure.RestingWidth;
    private const double DefaultHeight = Figure.RestingHeight;
    private const double DefaultDpi = Figure.ReferenceDpi;

    /// <summary>The field a described value (a tick locator, a normaliser, a patch, ...) names its kind in.</summary>
    private const string KindField = "type";

    /// <summary>A copy of the serializer's own read options (naming, converters, the number rule that reads "NaN" as
    /// the missing value), plus the one thing it deliberately lacks: a refusal of unmapped members. Its lenience is wire
    /// compatibility for its other consumers; here a document is authored by hand and an unknown field is a mistake to
    /// report.</summary>
    private static readonly JsonSerializerOptions Strict = new(ChartSerializer.ReadOptions)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    private readonly ChartTypeCatalog _catalog;
    private readonly RenderLimits _limits;
    private readonly SpecVocabulary _vocabulary = new();

    /// <summary>Creates a reader that accepts the types <paramref name="catalog"/> lists within <paramref name="limits"/>.</summary>
    public ChartSpecReader(ChartTypeCatalog catalog, RenderLimits limits)
    {
        _catalog = catalog;
        _limits = limits;
    }

    /// <summary>The <see cref="Figure"/> the spec describes. A spec is refused with a <see cref="ToolRefusalException"/> that names what
    /// is wrong; one is refused as "Nothing to draw" when it holds no series and carries nothing that shows of its own (an overlay of a
    /// subplot, or a text or an image on the figure), because that picture would be blank.</summary>
    /// <param name="spec">The chart spec the tool was called with.</param>
    /// <returns>The figure the spec describes.</returns>
    /// <exception cref="ToolRefusalException">The spec is not a JSON object, holds a value or a chart type the tool does not
    /// accept, goes past the render limits, or draws nothing.</exception>
    public Figure Read(ChartSpec spec)
    {
        JsonObject root = ParseObject(spec.Json);
        Normalise(root);
        string normalised = root.ToJsonString();
        FigureDto dto = DeserializeStrictly(normalised);
        int series = Validate(dto);
        Figure figure = Deserialize(normalised);
        if (series == 0 && !ShowsSomethingOfItsOwn(figure))
        {
            throw new ToolRefusalException(
                "Nothing to draw: the spec has no series, no patch, no overlay on a Cartesian subplot, and no text or image on the figure that shows. "
                + "Put at least one series with a 'type' in subPlots[].series[], or an overlay in the subplot (patches, spans, referenceLines, "
                + "annotations, trendlines, horizontalLevels, fibonacciRetracements or signals), or a text in texts[] or an image in images[]. "
                + "A legend, a colour bar or axis breaks alone draw nothing.");
        }

        return figure;
    }

    /// <summary>Whether the figure draws something with no series on it: a subplot with an overlay the renderer draws
    /// (<see cref="DrawsAnOverlay"/>), a text that shows or an image that shows, by the one rule core's data
    /// table reads as well (<see cref="FigureText.IsShownOn"/>, <see cref="FigureImage.IsShownOn"/>). A legend, a colour bar and the
    /// breaks of an axis are not counted: they describe what series draw, so with none they are no picture.</summary>
    private static bool ShowsSomethingOfItsOwn(Figure figure) =>
        figure.SubPlots.Any(axes => DrawsAnOverlay(axes))
        || figure.Texts.Any(text => text.IsShownOn(figure.Width, figure.Height))
        || figure.Images.Any(image => image.IsShownOn(figure.Height));

    private static JsonObject ParseObject(string json)
    {
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new ToolRefusalException($"The spec is not valid JSON: {ex.Message}");
        }

        return node as JsonObject
            ?? throw new ToolRefusalException($"The spec must be a JSON object ({{...}}), not {node?.GetValueKind().ToWireName() ?? "null"}.");
    }

    // ---- normalisation: defaults, bounds, text, colours, enum spelling ------------------------------------------

    private void Normalise(JsonObject figure)
    {
        _limits.EnsureSize(figure, "width", DefaultWidth, _limits.MaxWidth);
        _limits.EnsureSize(figure, "height", DefaultHeight, _limits.MaxHeight);
        _limits.EnsureSize(figure, "dpi", DefaultDpi, int.MaxValue);
        _limits.EnsurePictureFits(figure);
        _limits.CheckText(figure, "$", "title");
        NormaliseChild(figure, "titleStyle", "$", SpecNodeKind.TextStyle);
        _limits.CheckText(figure, "$", "altText");
        _limits.CheckText(figure, "$", "description");
        _limits.CheckText(figure, "$", "chartId");
        NormaliseColours(figure, "$");
        NormaliseColorBar(figure, "figureColorBar", "$");
        NormaliseChild(figure, "legend", "$", SpecNodeKind.Legend);
        NormaliseEach(figure, "legends", "$", SpecNodeKind.Legend);
        ForEachObject(figure, "texts", "$", NormaliseFigureText);
        ForEachObject(figure, "images", "$", NormaliseFigureImage);
        NormaliseTheme(figure, "$");
        ForEachObject(figure, "subPlots", "$", NormaliseAxes);
    }

    /// <summary>A text placed on the figure itself: its words, how it stands against its point and its font. Its colours are
    /// normalised with every other colour of the document.</summary>
    private void NormaliseFigureText(JsonObject text, string path)
    {
        _limits.CheckText(text, path, "text");
        NormaliseNode(text, SpecNodeKind.FigureText, path);
        NormaliseChild(text, "font", path, SpecNodeKind.Font);
    }

    /// <summary>An image placed on the figure itself: the kinds of its normaliser and of its colour map, which are checked and
    /// moved first as a series' are. Its colours are normalised with every other colour of the document. An image is cut at
    /// the figure's edge and never makes the picture bigger, so <see cref="RenderLimits.EnsurePictureFits"/> has nothing to count for it.</summary>
    private void NormaliseFigureImage(JsonObject image, string path)
    {
        NormaliseKind(image, "normalizer", path, SpecNodeKind.Normalizer);
        NormaliseColorMap(image, "colorMap", path);
    }

    /// <summary>The figure's theme: the enum values of its font and its grid, where it puts a legend, how it hands out the colours
    /// of its cycle, how it joins the corners of its strokes, and the line styles and markers its cycler steps through. Its colours are normalised with every other colour of the document.</summary>
    private void NormaliseTheme(JsonObject figure, string path)
    {
        if (figure["theme"] is not JsonObject theme)
        {
            return;
        }

        string at = $"{path}.theme";
        NormaliseNode(theme, SpecNodeKind.Theme, at);
        NormaliseChild(theme, "defaultFont", at, SpecNodeKind.Font);
        NormaliseChild(theme, "defaultGrid", at, SpecNodeKind.Grid);
        NormaliseChild(theme, "box", at, SpecNodeKind.BoxLook);
        NormaliseChild(theme, "propCycler", at, SpecNodeKind.PropCycler);
    }

    /// <summary>One subplot: its own text and enum fields and its overlays, then every axis and every series it keeps, at
    /// every seat (<see cref="AxesSeatShape"/>): the primary pair, the top and right axes and every Y axis slot of either
    /// side. Its insets are subplots in turn.</summary>
    private void NormaliseAxes(JsonObject axes, string path)
    {
        _limits.CheckText(axes, path, "title");
        foreach (var seat in AxesSeatShape.OfSubplot.Seats(axes, path, JsonSeatNodes.Instance))
        {
            if (seat.Kind == SeatKind.Axis)
            {
                NormaliseAxis(seat.Node, seat.Path);
            }
            else
            {
                NormaliseSeries(seat.Node, seat.Path);
            }
        }

        NormaliseNode(axes, SpecNodeKind.Axes, path);
        NormaliseChild(axes, "titleStyle", path, SpecNodeKind.TextStyle);
        NormaliseChild(axes, "legend", path, SpecNodeKind.Legend);
        NormaliseColorBar(axes, "colorBar", path);
        NormaliseChild(axes, "zoomIndicator", path, SpecNodeKind.ZoomIndicator);
        NormaliseChild(axes, "grid", path, SpecNodeKind.Grid);
        NormaliseChild(axes, "gridChange", path, SpecNodeKind.Grid);
        if (axes["spines"] is JsonObject spines)
        {
            foreach (var side in new[] { "top", "bottom", "left", "right" })
            {
                NormaliseChild(spines, side, $"{path}.spines", SpecNodeKind.Spine);
            }
        }

        ForEachObject(axes, "annotations", path, (annotation, p) =>
        {
            _limits.CheckText(annotation, p, "text");
            NormaliseNode(annotation, SpecNodeKind.Annotation, p);
            NormaliseChild(annotation, "font", p, SpecNodeKind.Font);
        });
        ForEachObject(axes, "referenceLines", path, (line, p) =>
        {
            _limits.CheckText(line, p, "label");
            NormaliseNode(line, SpecNodeKind.ReferenceLine, p);
        });
        ForEachObject(axes, "spans", path, (span, p) =>
        {
            _limits.CheckText(span, p, "label");
            NormaliseNode(span, SpecNodeKind.Span, p);
        });
        ForEachObject(axes, "patches", path, NormalisePatch);
        NormaliseEach(axes, "trendlines", path, SpecNodeKind.Trendline);
        NormaliseEach(axes, "horizontalLevels", path, SpecNodeKind.HorizontalLevel);
        ForEachObject(axes, "signals", path, (signal, p) =>
        {
            _limits.CheckText(signal, p, "label");
            NormaliseNode(signal, SpecNodeKind.Signal, p);
        });
        NormaliseEach(axes, "xBreaks", path, SpecNodeKind.AxisBreak);
        NormaliseEach(axes, "yBreaks", path, SpecNodeKind.AxisBreak);
        ForEachObject(axes, "insets", path, NormaliseAxes);
    }

    /// <summary>One patch of a subplot: its text, its kind, which stands first as the serializer reads it only there, and the
    /// enum words of its outline, its hatch and (an arrow's) its shape. The kind is checked here, ahead of the strict pass: a patch
    /// of a kind this build does not know reads as the base record there, and the first field of its geometry would be reported
    /// as the unknown one. Its colours are normalised with every other colour of the document.</summary>
    private void NormalisePatch(JsonObject patch, string path)
    {
        _limits.CheckText(patch, path, "label");
        NormaliseKindOf(patch, "patch", path, SpecNodeKind.Patch);
    }

    private void NormaliseSeries(JsonObject series, string path)
    {
        _limits.CheckText(series, path, "label");
        string? type = series["type"] is JsonValue value && value.TryGetValue<string>(out var typed) ? typed : null;
        NormaliseNode(series, SpecNodeKind.Series, path, type);

        foreach (var field in _vocabulary.TreeFields)
        {
            if (series[field] is JsonObject root)
            {
                NormaliseTreeNode(root, $"{path}.{field}");
            }
        }

        NormaliseKind(series, "normalizer", path, SpecNodeKind.Normalizer);
        NormaliseColorMap(series, "colorMap", path);
        NormaliseColorMap(series, "pairGridOffDiagonalColorMapDescription", path);
    }

    /// <summary>One node of a tree (a treemap's, a sunburst's, a dendrogram's or a clustermap's), and the nodes below it: the
    /// pattern over its rectangle. Its colours are normalised with every other colour of the document.</summary>
    private void NormaliseTreeNode(JsonObject node, string path)
    {
        NormaliseNode(node, SpecNodeKind.TreeNode, path);
        ForEachObject(node, "children", path, NormaliseTreeNode);
    }

    /// <summary>A colour map built in code, described by kind: its kind is checked and moved first as a normaliser's is,
    /// and so is the kind of the map a reversed map reverses. Its colours are normalised with every other colour of the
    /// document.</summary>
    private void NormaliseColorMap(JsonObject parent, string field, string path)
    {
        NormaliseKind(parent, field, path, SpecNodeKind.ColorMap);
        if (parent[field] is JsonObject map)
        {
            NormaliseColorMap(map, "map", $"{path}.{field}");
        }
    }

    /// <summary>One axis: its label, its scale, its ticks, its label style, and the kinds of its tick locator, its tick formatter and
    /// its link (the transform that shows the primary axis' range in another unit).</summary>
    private void NormaliseAxis(JsonObject axis, string path)
    {
        _limits.CheckText(axis, path, "label");
        NormaliseNode(axis, SpecNodeKind.Axis, path);
        NormaliseChild(axis, "majorTicks", path, SpecNodeKind.Ticks);
        NormaliseChild(axis, "minorTicks", path, SpecNodeKind.Ticks);
        NormaliseChild(axis, "labelStyle", path, SpecNodeKind.TextStyle);
        NormaliseKind(axis, "tickLocator", path, SpecNodeKind.TickLocator);
        NormaliseKind(axis, "tickFormatter", path, SpecNodeKind.TickFormatter);
        NormaliseKind(axis, "link", path, SpecNodeKind.AxisLink);
    }

    /// <summary>A colour bar: its enum fields, and the kinds of its colour map and its tick locator.</summary>
    private void NormaliseColorBar(JsonObject parent, string field, string path)
    {
        NormaliseChild(parent, field, path, SpecNodeKind.ColorBar);
        if (parent[field] is JsonObject colorBar)
        {
            NormaliseColorMap(colorBar, "colorMap", $"{path}.{field}");
            NormaliseKind(colorBar, "locator", $"{path}.{field}", SpecNodeKind.TickLocator);
        }
    }

    /// <summary>The enum-valued fields of the object at <paramref name="field"/>, a node of <paramref name="kind"/>, when there
    /// is one (<see cref="NormaliseNode"/>).</summary>
    private void NormaliseChild(JsonObject parent, string field, string path, SpecNodeKind kind)
    {
        if (parent[field] is JsonObject node)
        {
            NormaliseNode(node, kind, $"{path}.{field}");
        }
    }

    /// <summary>The enum-valued fields of every object of the list at <paramref name="field"/>, each a node of <paramref name="kind"/>.</summary>
    private void NormaliseEach(JsonObject parent, string field, string path, SpecNodeKind kind) =>
        ForEachObject(parent, field, path, (node, at) => NormaliseNode(node, kind, at));

    /// <summary>Every enum-valued field a node of <paramref name="kind"/> carries, as the vocabulary lists them
    /// (<see cref="Mcp.ChartSpecReader.SpecVocabulary.FieldsOf"/>): the reader names no field of its own, so a row added to the vocabulary is checked
    /// wherever a node of its kind stands.</summary>
    /// <param name="node">The node.</param>
    /// <param name="kind">Where it sits.</param>
    /// <param name="path">Where it stands in the spec.</param>
    /// <param name="seriesType">The kind of series, for a series node (its <c>orientation</c> depends on it).</param>
    private void NormaliseNode(JsonObject node, SpecNodeKind kind, string path, string? seriesType = null)
    {
        foreach (var field in _vocabulary.FieldsOf(kind))
        {
            NormaliseEnum(node, kind, field, path, seriesType);
        }
    }

    /// <summary>A described value (a tick locator, a tick formatter, a normaliser): its <c>type</c> must name a kind the
    /// serializer knows, spelt as it reads it, and stand first, since the serializer reads the kind only as the first
    /// field of the object. A value without a <c>type</c> is refused: the serializer would skip it.</summary>
    private void NormaliseKind(JsonObject parent, string field, string path, SpecNodeKind kind)
    {
        if (parent[field] is JsonObject node)
        {
            NormaliseKindOf(node, field, $"{path}.{field}", kind);
        }
    }

    /// <summary>The kind <paramref name="node"/> names of itself, checked, spelt as the serializer reads it and moved to stand
    /// first, with the other enum-valued fields of its kind (a patch's outline, hatch, arrow shape and plane): <see cref="NormaliseKind"/>
    /// for a value that is an entry of a list rather than the field of an object.</summary>
    /// <param name="node">The described value.</param>
    /// <param name="what">What the value is, as a refusal names it.</param>
    /// <param name="where">Where <paramref name="node"/> stands.</param>
    /// <param name="kind">The family of kinds the value belongs to.</param>
    /// <exception cref="ToolRefusalException">The value has no <c>type</c>, or one that is not a kind of its family.</exception>
    private void NormaliseKindOf(JsonObject node, string what, string where, SpecNodeKind kind)
    {
        var rule = _vocabulary.RuleFor(kind, KindField, seriesType: null)!;
        if (node[KindField] is null)
        {
            throw new ToolRefusalException($"The {what} at {where} has no '{KindField}'; accepted: {string.Join(", ", rule.Accepted)}.");
        }

        NormaliseNode(node, kind, where);
        var fields = node.ToList();
        node.Clear();
        node[KindField] = fields.Single(pair => pair.Key == KindField).Value;
        foreach (var pair in fields.Where(pair => pair.Key != KindField))
        {
            node[pair.Key] = pair.Value;
        }
    }

    /// <summary>Visits every object element of the array at <paramref name="field"/>; a value of another shape is
    /// left for the strict pass, which names it, except <c>null</c>, which the strict pass cannot name (it would
    /// surface as a bare <see cref="NullReferenceException"/> there or in core's restore): it is refused here with its path.</summary>
    /// <exception cref="ToolRefusalException">An entry of the list is <c>null</c>.</exception>
    private static void ForEachObject(JsonObject parent, string field, string path, Action<JsonObject, string> visit)
    {
        if (parent[field] is not JsonArray items)
        {
            return;
        }

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] is null)
            {
                throw new ToolRefusalException(string.Invariant($"The entry at {path}.{field}[{i}] is null; it must be an object."));
            }

            if (items[i] is JsonObject item)
            {
                visit(item, string.Invariant($"{path}.{field}[{i}]"));
            }
        }
    }

    /// <summary>The enum value at <paramref name="field"/>, or each item of the list of enum values there: checked against
    /// the field's vocabulary and spelt as the serializer reads it. A field that is not enum-valued, or holds another
    /// kind of value, is left for the strict pass.</summary>
    private void NormaliseEnum(JsonObject node, SpecNodeKind kind, string field, string path, string? seriesType)
    {
        var rule = _vocabulary.RuleFor(kind, field, seriesType);
        if (rule is null)
        {
            return;
        }

        switch (node[field])
        {
            case JsonValue value when value.TryGetValue<string>(out var written):
                node[field] = CanonicalOrRefuse(rule, written, $"{path}.{field}");
                break;
            case JsonArray items:
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] is JsonValue item && item.TryGetValue<string>(out var entry))
                    {
                        items[i] = CanonicalOrRefuse(rule, entry, string.Invariant($"{path}.{field}[{i}]"));
                    }
                }
                break;
        }
    }

    /// <summary>The spelling <paramref name="rule"/> accepts for <paramref name="written"/>, or the refusal that names
    /// where it stands and what is accepted there.</summary>
    private static string CanonicalOrRefuse(SpecVocabulary.Rule rule, string written, string where) =>
        rule.Canonical(written)
        ?? throw new ToolRefusalException($"'{written}' at {where} is not an accepted value; accepted: {string.Join(", ", rule.Accepted)}.");

    // ---- the strict structural pass -----------------------------------------------------------------------------

    private static FigureDto DeserializeStrictly(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<FigureDto>(json, Strict)
                ?? throw new ToolRefusalException("The spec is empty.");
        }
        catch (JsonException ex)
        {
            throw new ToolRefusalException(Describe(ex));
        }
    }

    private static string Describe(JsonException ex)
    {
        var unmapped = UnmappedProperty().Match(ex.Message);
        string where = ex.Path ?? "$";
        return unmapped.Success
            ? $"Unknown field '{unmapped.Groups[1].Value}' at {where}. describe_chart_schema lists the fields a spec accepts."
            : $"The field at {where} has the wrong shape: {ex.Message}";
    }

    [GeneratedRegex("property '([^']+)'")]
    private static partial Regex UnmappedProperty();

    // ---- the semantic pass ----------------------------------------------------------------------------------------

    /// <summary>Checks every series of every subplot and returns how many there are, which <see cref="Read"/> weighs against what
    /// the figure draws of its own.</summary>
    private int Validate(FigureDto dto)
    {
        int series = 0;
        var subPlots = dto.SubPlots ?? [];
        for (int i = 0; i < subPlots.Count; i++)
        {
            series += ValidateAxes(subPlots[i], string.Invariant($"$.subPlots[{i}]"));
        }

        return series;
    }

    /// <summary>The series of one subplot at every seat (<see cref="AxesSeatShape"/>), each checked once, and the number the
    /// subplot and its insets draw.</summary>
    private int ValidateAxes(AxesDto axes, string path)
    {
        int drawn = 0;
        foreach (var seat in AxesSeatShape.OfSubplot.Seats<object>(axes, path, DtoSeatNodes.Instance).Where(seat => seat.Kind == SeatKind.Series))
        {
            ValidateSeries((SeriesDto)seat.Node, seat.Path);
            drawn++;
        }

        var insets = axes.Insets ?? [];
        for (int k = 0; k < insets.Count; k++)
        {
            drawn += ValidateAxes(insets[k], string.Invariant($"{path}.insets[{k}]"));
        }

        return drawn;
    }

    private void ValidateSeries(SeriesDto series, string path)
    {
        if (string.IsNullOrEmpty(series.Type))
        {
            throw new ToolRefusalException($"The series at {path} has no 'type'. list_chart_types names the accepted types.");
        }

        // Core's secondary-axis restore now goes through the same SeriesRegistry the primary axis does
        // (ChartSerializer.CreateOnSecondarySeat, ROADMAP.md row 99), so the same catalog check applies to both —
        // no separate, narrower allowlist for a series on the right-hand or top axis.
        if (!_catalog.IsListed(series.Type))
        {
            if (_catalog.Excluded.TryGetValue(series.Type, out var reason))
            {
                throw new ToolRefusalException($"'{series.Type}' at {path}.type cannot be built from JSON in this version: {reason}.");
            }

            string hint = _catalog.Suggest(series.Type) is { } meant ? $" — did you mean '{meant}'?" : "";
            throw new ToolRefusalException($"'{series.Type}' at {path}.type is not a chart type this server lists{hint} list_chart_types names the accepted types.");
        }

        RefuseWhatTheLibraryRefuses(series, path);
    }

    /// <summary>Whether the series fits its own data (arrays that pair, a grid the size of its axes, a count of at least one)
    /// is the library's rule, asked of the very code that builds the series: the verb. This reader holds no copy of it and only
    /// names the place, so a rule the library gains is a rule here, for every seat the series can stand at. A series the library
    /// makes nothing of (a map whose package data names no projection or no features it knows) is refused too: the serializer
    /// would leave it out and the picture would be blank where the spec asked for it.</summary>
    /// <exception cref="ToolRefusalException">The library refuses the series, or makes nothing of it; the message names its place and
    /// the library's reason, or what the package data must hold.</exception>
    private static void RefuseWhatTheLibraryRefuses(SeriesDto series, string path)
    {
        SeriesRead? made;
        try
        {
            made = SeriesRegistry.Read(series.Type!, new Axes(), series);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or FormatException)
        {
            throw new ToolRefusalException($"The '{series.Type}' series at {path} was refused: {ex.Message}");
        }

        if (made is not { Series: not null })
        {
            string why = made?.Why is { } reason ? $": {reason}" : "";
            throw new ToolRefusalException(
                $"The '{series.Type}' series at {path} describes nothing the library can draw{why}. Its packageData is {ChartSchemaDescription.PackageDataNote}.");
        }
    }

    // ---- the serializer -------------------------------------------------------------------------------------------

    private static Figure Deserialize(string json)
    {
        try
        {
            return ChartSerializerDefault.Current.FromJson(json);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or FormatException or JsonException)
        {
            throw new ToolRefusalException($"The library refused the figure: {ex.Message}");
        }
    }

    /// <summary>Whether the axes, or an inset the renderer draws on it, holds an overlay the renderer draws: a patch on any axes
    /// (every renderer draws its patches), and on a Cartesian one a span, a reference line, an annotation, a trendline, a
    /// horizontal level, a Fibonacci retracement or a signal (the polar, ternary and 3-D renderers read no other overlay). An inset
    /// without bounds never gets here: reading the figure leaves it out, as the renderer would not draw it. The breaks of an axis
    /// are not counted, nor is a legend or a colour bar: they describe what series draw, so with none they are no picture.</summary>
    /// <param name="axes">The <see cref="Axes"/> it works on.</param>
    private static bool DrawsAnOverlay(Axes axes) =>
        axes.Patches.Count > 0
        || (axes.CoordinateSystem == CoordinateSystem.Cartesian
            && axes.Annotations.Count + axes.ReferenceLines.Count + axes.Spans.Count + axes.Trendlines.Count
                + axes.HorizontalLevels.Count + axes.FibonacciRetracements.Count + axes.Signals.Count > 0)
        || axes.Insets.Any(inset => DrawsAnOverlay(inset));

    /// <summary>Where a spec field sits. The same wire name means different things in different places:
    /// <c>orientation</c> is a bar orientation on a bar series and a line orientation on a reference line.</summary>
    internal enum SpecNodeKind
    {
        Axes,
        Axis,
        Series,
        Annotation,
        ReferenceLine,
        Span,
        Trendline,
        HorizontalLevel,
        AxisBreak,
        Legend,
        ColorBar,
        Grid,
        Spine,
        Ticks,
        TextStyle,
        Font,
        TickLocator,
        TickFormatter,
        Normalizer,
        ColorMap,
        PropCycler,
        FigureText,
        ZoomIndicator,
        Signal,
        Patch,
        Theme,
        /// <summary>A box plot look of a theme.</summary>
        BoxLook,
        TreeNode,
        AxisLink,
    }
}
