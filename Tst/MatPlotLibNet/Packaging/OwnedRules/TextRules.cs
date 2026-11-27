// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging.OwnedRules;

/// <summary>Whether a text is math, how it is drawn and how it is measured is decided in one place: a text is a <c>TextLabel</c>,
/// drawn by <c>ctx.Draw(label, font, placement)</c> and measured by <c>ctx.ExtentOf(label, font)</c>. Where it stands across its
/// line is its placement's (<c>TextPlacement.Vertical</c>, the box's ascent), its size and font are made from the theme's one base
/// font by a role, and a number becomes text in the invariant culture only in <c>InvariantTextExtensions</c>.</summary>
/// <remarks>The Verso, DataFrame and Interactive packages have no access to the core's internals (granting it would pin them to the
/// exact core version, <see cref="InternalsDependencyContractTests"/>): Verso writes its one number and reads its one number
/// itself, DataFrame converts a cell of a column to a number and to a flag itself, Interactive writes the port of its server
/// itself; they are the second owners, named in the rows.</remarks>
internal static class TextRules
{
/// <summary>The size a series' own text is drawn at is one rule over the size its author gave and the size the kind rests at.</summary>
    private static readonly OwnedRule SeriesTextSize = new(
        "the size a series' own text is drawn at (a table, a tree grid, a 3-D text, a contour label) is IGivenTextSize's TextSizeUnder over Theme.SeriesTextSizeOver: no kind writes that method again",
        @"\bSeriesTextSizeOver\(",
        "Src/MatPlotLibNet/Styling/Theme.cs", "Src/MatPlotLibNet/Styling/IGivenTextSize.cs") { Under = "Src/", Finds = [
        "    internal double SeriesTextSizeOver(double? given, double resting) => given ?? (Looks.Text.SeriesTextAtFontSize ? DefaultFont.Size : resting);"] };

/// <summary>The surfaces that implement the two public text members and the one verb that asks them.</summary>
    private static readonly string[] TheSurfacesAndTheVerb =
    [
        "Src/MatPlotLibNet/Rendering/IRenderContext.cs",
        "Src/MatPlotLibNet/Rendering/MarkCollectingContext.cs",
        "Src/MatPlotLibNet/Rendering/Svg/SvgRenderContext.cs",
        "Src/MatPlotLibNet/Rendering/MathText/MathTextRenderContextExtensions.cs",
        "Src/MatPlotLibNet.Skia/SkiaRenderContext.cs",
        "Src/MatPlotLibNet.Maui/MauiGraphicsRenderContext.cs",
    ];

    private static readonly OwnedRule TextDrawnByTheVerb = new(
        "a text is drawn through ctx.Draw(TextLabel, font, placement), which decides once whether it is math: no renderer, layout or part asks a surface for DrawText, DrawRichText or DrawTextWithLevel by itself (a surface implements them, the verb asks them; the level of a tree row is the placement's)",
        @"\b(?:DrawText|DrawRichText|DrawTextWithLevel)\(",
        TheSurfacesAndTheVerb) { Under = "Src/", Finds = [
        "        ctx.DrawText(label, at, font, TextAlignment.Left);",
        "            Ctx.DrawText(series.Label, box.Baseline, font, label.Alignment);",
        "        ctx.DrawRichText(rich, at, font, alignment);",
        "        Ctx.DrawTextWithLevel(row.Label, at, font, level);"], Ignores = [
        "        ctx.Draw(TextLabel.Of(label), font, new TextPlacement(at, TextAlignment.Left) { Level = row.Depth + 1 });"] };

    private static readonly OwnedRule PlainTextOfARichText = new(
        "the plain text of a rich text (every span's text one after the other) is RichText.PlainText: no site joins the spans by hand",
        @"\.Select\(\s*\w+\s*=>\s*\w+\.Text\s*\)|foreach \(var span in (?:richText|rich|text)\.Spans\)",
        "Src/MatPlotLibNet/Rendering/MathText/RichText.cs") { Under = "Src/", Finds = [
        "        var plain = string.Concat(richText.Spans.Select(s => s.Text));",
        "        foreach (var span in richText.Spans) sb.Append(span.Text);",
        "        byte[] levels = Resolve(string.Concat(spans.Select(span => span.Text)));"], Ignores = [
        "        var plain = richText.PlainText;",
        "        foreach (var span in Axes.Spans)",
        "            Spans = axes.Spans.Select(SpanRegionDto.From).ToList(),"] };

    private static readonly OwnedRule LineOfAFont = new(
        "the height and the ascent of one line of a font are asked as ctx.LineExtentOf(font): no site names the sample text it measures the line by",
        @"\.ExtentOf\(\s*(?:TextLabel\.\w+\(\s*)?(?:""[^""]{0,2}""|string\.Empty)\s*[,)]",
        "Src/MatPlotLibNet/Rendering/TextMeasurement/TextExtentExtensions.cs") { Under = "Src/", Finds = [
        "        double h = Ctx.ExtentOf(\"0\", font).Height;",
        "        var line = Ctx.ExtentOf(string.Empty, labelFont);",
        "        double h = ctx.ExtentOf(TextLabel.Plain(\"0\"), font).Height;",
        "        double h = ctx.ExtentOf(TextLabel.Of(\"\"), font).Height;"], Ignores = [
        "        double h = ctx.LineExtentOf(font).Height;",
        "            double w = Math.Max(ctx.ExtentOf(\"0.0000\", tickFont).Width, 1);",
        "        var size = ctx.ExtentOf(TextLabel.Of(annotation.Text), font);"] };

    private static readonly OwnedRule WhereAPlainLabelStands = new(
        "a label set as written whatever it holds (TextLabel.Plain) is the one text a surface was handed through its public DrawText, which is raw by contract (the box the mark collector keeps for it); every other text, whoever places it, is TextLabel.Of, so it reads math",
        @"TextLabel\.Plain\(",
        "Src/MatPlotLibNet/Rendering/MarkCollectingContext.cs") { Under = "Src/", Finds = [
        "        Ctx.Draw(TextLabel.Plain(title), font, new TextPlacement(at, TextAlignment.Left));",
        "        double w = ctx.ExtentOf(TextLabel.Plain(text), font).Width;"], Ignores = [
        "        Ctx.Draw(TextLabel.Of(title), font, new TextPlacement(at, TextAlignment.Left));",
        "        var rich = TextLabel.OfRich(spans);"] };

    private static readonly OwnedRule SeriesMeasuredBySeam = OwnedRule.Nowhere(
        "a series renderer measures and places its texts by the surface (ctx.ExtentOf, or the SurfaceFontMetrics the layouts are handed), which reads math as the verb that draws them does: it never names the installed font metrics, which know no math",
        @"\bFontMetricsDefault\b",
        under: "Src/MatPlotLibNet/Rendering/SeriesRenderers/") with { Finds = [
        "        LabelLayoutEngine.Place(candidates, bounds, FontMetricsDefault.Current);",
        "        WordCloudLayout.Compute(spec, bounds, FontMetricsDefault.Current);"] };

    /// <summary>Texts placed by their vertical alignment, and sizes of another rule: no rule of a drop or a hang reads them as one.</summary>
    private static readonly string[] PlacedByTheirVerticalAlignment =
    [
        "        var middle = new TextPlacement(nodeRects[i].Center, TextAlignment.Center) { Vertical = TextVerticalAlignment.Center };",
        "        double belowBase = tickFont.Size + TernaryDrawingExtensions.TickGap + 4;",
        "            Size = labelFont.Size * 0.8,",
        "    internal double HeightFor(double fontSize) => fontSize + 2 * Pad;",
        "        Ctx.Draw(TextLabel.Of(value), font, new TextPlacement(new Point(cx, cy + ValueBelowHub), TextAlignment.Center));",
    ];

    private static readonly OwnedRule BaseFontOfTheTheme = new(
        "the theme's base font (its family, size and weight) is read by the theme's font roles (ThemeFontExtensions), by the theme itself and by the rc keys that write it: no renderer, layout or model reads Theme.DefaultFont's size, family or weight by itself, it asks a role (TickFont, LabelFont, DataTextFont, TitleFont)",
        @"DefaultFont\.(?:Size|Family|Weight)\b",
        "Src/MatPlotLibNet/Rendering/ThemeFontExtensions.cs",
        "Src/MatPlotLibNet/Styling/Theme.cs",
        "Src/MatPlotLibNet/Styling/ThemeRcKey.cs") { Under = "Src/", Finds = [
        "        var labelFont = new Font { Family = df.Family, Size = Context.Theme.DefaultFont.Size };",
        "        double size = Context.Theme.DefaultFont.Size;",
        "            Family = theme.DefaultFont.Family,",
        "        var font = theme.DataTextFont(12, null, theme.DefaultFont.Weight);"], Ignores = [
        "        var font = Context.Theme.DataTextFont();",
        "        theme = new(theme.Name, theme) { DefaultFont = configure(theme.DefaultFont) };"] };

    private static readonly OwnedRule CentreDropOfTheBox = new(
        "how far below its middle a centred text's baseline stands (0.35 of its ascent) is TextBox's own: a text centred on a point is placed with TextVerticalAlignment.Center",
        @"\bCentreDrop\b|[Ff]ont\w*\.Size\s*\*\s*0\.35\b",
        "Src/MatPlotLibNet/Rendering/TextBox.cs") { Under = "Src/MatPlotLibNet/Rendering/", Finds = [
        "                new Point(x, centres[k].Start + font.Size * TextBox.CentreDrop), font));",
        "            double lift = font.Size * 0.35;"] };

    private static readonly OwnedRule NoHandDropBelowACentre = OwnedRule.Nowhere(
        "a text centred on a point stands there by TextVerticalAlignment.Center: no site drops its baseline below a centre by a number of its own (a third of the font size, three tenths of a height, a few pixels past a middle)",
        @"(?:[Ff]ont\w*(?:\(\))?\.Size|\w*[Hh]eight)\s*(?:/\s*3\b|\*\s*0\.3[0-9]*\b)|(?:\bcentreY|\bcy|/\s*2)\s*\+\s*\d+(?:\.\d+)?\s*\)",
        under: "Src/MatPlotLibNet/Rendering/") with { Finds = [
        "                Ctx.Draw(TextLabel.Of(cell), cellFont, new TextPlacement(new Point(textX, y + rowH / 2 + cellFont.Size / 3), alignment));",
        "                double cy = nodeRects[i].Y + nodeRects[i].Height / 2 + measured.Height * 0.3;",
        "        var anchor = new Point(labelX, nodeRect.Y + nodeRect.Height / 2 + measuredLabelHeight * 0.3);",
        "                    ? new LabelCandidate(new Point(end + LabelPad, centreY + 4), label, font, TextAlignment.Left)",
        "            yield return new FunnelStage(i, bar, new Point(bounds.X + bounds.Width / 2, y + rowH / 2 + 4));",
        "            Ctx.Draw(TextLabel.Of(series.CenterText), font, new TextPlacement(new Point(cx, cy + 5), TextAlignment.Center));"], Ignores = PlacedByTheirVerticalAlignment };

    private static readonly OwnedRule NoHangByTheFontSize = OwnedRule.Nowhere(
        "a text hung below a point stands there by TextVerticalAlignment.Top, its box's ascent below it: no site puts a baseline a font size below an edge, a pad or a line",
        @"\+\s*[\w.]*(?:[Ff]ont\w*(?:\(\))?\.Size|\bfontSize|\bLabelSize)\b",
        under: "Src/MatPlotLibNet/Rendering/") with { Finds = [
        "        new(new Point(x + Gap, top + fontSize), TextAlignment.Left);",
        "        ctx.Draw(TextLabel.Of(label), font, new TextPlacement(new Point(leafPx, mergeBaselinePx + LabelOffsetPx + font.Size), TextAlignment.Center));",
        "        internal double TickBaselineBelow(Theme theme) => ColorBarMarks.Under(theme).LabelGap(false) + theme.TickFont().Size;",
        "    internal Point Name(Rect cell, double fontSize) => new(cell.X + Pad, cell.Y + fontSize + BaselineGap);",
        "        double belowBase = tickFont.Size + TickGap + 4 + labelFont.Size;"], Ignores = PlacedByTheirVerticalAlignment };

    private static readonly OwnedRule NoShareOfALineForItsAscent = OwnedRule.Nowhere(
        "where a baseline stands in a line is the text's ascent (TextExtent): no site takes a share of a line's height for it",
        @"[Hh]eight\s*\*\s*0\.75\b",
        under: "Src/MatPlotLibNet/Rendering/") with { Finds = [
        "        var titlePoint = new Point(figure.Width / 2, SupTitleBand.TopPad + frame.TitleHeight * 0.75);",
        "            double top = position.Y - size.Height * 0.75;"], Ignores = PlacedByTheirVerticalAlignment };

    private static readonly OwnedRule NoDropOfAnEdge = OwnedRule.Nowhere(
        "a tick label stands on its anchor by the vertical alignment of its edge (EdgeTerms.LabelVertical): no edge or bar keeps a drop or a nudge of its own",
        @"\b(?:LabelDrop|LabelNudge|YLabelNudge|TickLabelDrop)\b",
        under: "Src/") with { Finds = [
        "        Point.Along(AlongX, along + LabelNudge, edge + Outward * reach + LabelDrop * fontSize);",
        "    private const double TickLabelDrop = 4;"], Ignores = PlacedByTheirVerticalAlignment };

    private static readonly OwnedRule SeriesTextSizeByTheTheme = OwnedRule.Nowhere(
        "the size of a series text that names none is the theme's one rule (Theme.SeriesTextSizeOver, through IGivenTextSize.TextSizeUnder), its resting size named by the series beside its FontSize: no renderer draws a series text at a size of its own",
        @"DataTextFont(?:On)?\((?:[^()]*,\s*)?\d+(?:\.\d+)?\s*[,)]|const double (?:fontSize|\w*FontSize|LabelSize|\w*LabelSize)\s*=\s*\d|DataTextFont\(Math\.M(?:in|ax)\(\s*\d",
        under: "Src/MatPlotLibNet/Rendering/SeriesRenderers/") with { Finds = [
        "        Ctx.Draw(label, Context.Theme.DataTextFontOn(ground, 11, FontWeight.Bold), at);",
        "        var captionFont = Context.Theme.DataTextFont(11, color);",
        "            Ctx.Draw(weight, Context.Theme.DataTextFont(9), new TextPlacement(mid, TextAlignment.Center));",
        "        var labelFont = Context.Theme.DataTextFont(Math.Min(10, Context.Theme.DataTextFont().Size));",
        "    private const double LabelSize = 12;",
        "        const double fontSize = 12.0;",
        "    private const double LabelFontSize      = 9.0;"], Ignores = [
        "        var font = Context.Theme.DataTextFontOn(ground, series.TextSizeUnder(Context.Theme), FontWeight.Bold);",
        "        var font = Context.Theme.DataTextFont(HeadlineSize * k * scale, color, FontWeight.Bold);",
        "        var font = Context.Theme.DataTextFont();"] };

    private static readonly OwnedRule SeriesTextAtTheFontSizeReadOnce = new(
        "whether a series text that names no size takes the theme's font size is read by the one rule (Theme.SeriesTextSizeOver), beside the look that holds it and the keys that carry it",
        @"\.SeriesTextAtFontSize\b",
        "Src/MatPlotLibNet/Styling/Theme.cs",
        "Src/MatPlotLibNet/Styling/Looks/TextLook.cs") { Under = "Src/", Finds = [
        "    internal double SeriesTextSizeOver(double? given, double resting) => given ?? (Looks.Text.SeriesTextAtFontSize ? DefaultFont.Size : resting);"] };

    private static readonly OwnedRule FontMadeFromAFont = new(
        "a font made from another font is a with of it, which keeps every field it does not change: no site copies a font's family into a new Font (the weight and the slant it does not copy are lost); only the theme's one base font is assembled from the theme's",
        @"\bFamily\s*=\s*[\w.]+\.Family\s*,",
        "Src/MatPlotLibNet/Rendering/ThemeFontExtensions.cs") { Under = "Src/", Finds = [
        "            Family = labelFont.Family,",
        "            Family = subLabelFontBase.Family,"], Ignores = [
        "        var subFont = subLabelFontBase with { Color = color };",
        "        Family = font.Family.OrNullAtRest(Rest.Family),"] };

    private static readonly OwnedRule SizeNamedForARoleByTheRole = new(
        "a size or a colour a legend or the ticks name for their text (Legend.FontSize and TitleFontSize, TickConfig.LabelSize and LabelColor) goes into the font by its role in ThemeFontExtensions (LegendFontOf, EndLabelFontOf, LegendTitleFontOf, LabelFontOver, LabelSizeOver): no layout or drawing reads it to build or size a font itself, so what is measured and what is drawn take one size",
        @"\.(?:FontSize|TitleFontSize|LabelSize|LabelColor)\s*(?:\?\?|\.HasValue|is\s*\{)",
        "Src/MatPlotLibNet/Rendering/ThemeFontExtensions.cs") { Under = "Src/MatPlotLibNet/Rendering/", Finds = [
        "            return legend.FontSize.HasValue ? tickFont with { Size = legend.FontSize.Value } : tickFont;",
        "        var titleFont = baseFont with { Size = legend.TitleFontSize ?? baseFont.Size + look.TitleSizeStep, Weight = look.TitleWeight };",
        "        if (config.LabelSize.HasValue) font = font with { Size = config.LabelSize.Value };",
        "        internal double TickLabelSizeUnder(Theme theme) => axis.MajorTicks.LabelSize ?? theme.TickFont().Size;"] };

    private const string BidiFolder = "Src/MatPlotLibNet/Rendering/Text/";

    /// <summary>The rules the bidi algorithm reads in more than one clause of UAX #9, each written by the type that owns it.</summary>
    private static readonly OwnedRule[] Bidi =
    [
        new("a code point of a UTF-16 text (two units for a well-formed surrogate pair, one for any other unit, a lone surrogate as its own value) is read by CodePointExtensions: no reader of a text tests for a pair itself",
            @"char\.IsHighSurrogate\(",
            BidiFolder + "CodePointExtensions.cs") { Under = "Src/", Finds = [
            "            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))",
            "        int end = i + (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]) ? 2 : 1);"], Ignores = [
            "        while (start > 0 && (char.IsLowSurrogate(text[start]) || CharUnicodeInfo.GetUnicodeCategory(text[start]) is"] },
        new("the isolate controls of the bidi classes (an initiator LRI, RLI or FSI, and the PDI that closes one) are named by BidiAlgorithm.IsIsolateInitiator and IsIsolateControl: no rule lists them itself",
            @"BidiClass\.(?:LRI|RLI|FSI|PDI)\s+or\s+BidiClass\.(?:LRI|RLI|FSI|PDI)|IsIsolateInitiator\b[^;]*\|\|[^;]*BidiClass\.PDI",
            BidiFolder + "BidiAlgorithm.cs") { Under = "Src/", Finds = [
            "            or BidiClass.LRI or BidiClass.RLI or BidiClass.FSI or BidiClass.PDI;",
            "                case BidiClass.RLI or BidiClass.LRI or BidiClass.FSI:",
            "                types[idx[k]] = IsIsolateInitiator(previous) || previous == BidiClass.PDI ? BidiClass.ON : previous;"], Ignores = [
            "                case var c when c.IsIsolateInitiator:"] },
        new("which characters rule X9 keeps is BidiText.Kept: no rule filters or counts the removed characters itself",
            @"\bremoved\[|!\w+\[\w+\]\.IsRemovedByX9\b|!(?:\w+\.)?IsRemovedByX9\(\w+\[\w+\]\)",
            BidiFolder + "BidiAlgorithm.cs") { Under = "Src/", Finds = [
            "            if (!removed[i])",
            "            for (int j = from; j >= 0 && (removed[j] || IsWhitespaceOrIsolateControl(classes[j])); j--)",
            "            if (!classes[i].IsRemovedByX9) kept++;",
            "            if (!IsRemovedByX9(classes[i])) kept++;"], Ignores = [
            "            byte level = Classes[i].IsRemovedByX9 ? BidiParagraph.Removed : levels[i];",
            "            byte level = IsRemovedByX9(Classes[i]) ? BidiParagraph.Removed : levels[i];"] },
        new("the first strong character outside any isolate decides the direction of a paragraph (rules P2 and P3), in BidiAlgorithm.IsRightToLeftParagraph: no rule, the first strong isolate's included, walks to it itself",
            @"\(\w+ is BidiClass\.R or BidiClass\.AL\)",
            BidiFolder + "BidiAlgorithm.cs") { Under = "Src/", Finds = [
            "            if (c is BidiClass.R or BidiClass.AL)"], Ignores = [
            "            if (c is BidiClass.R or BidiClass.AL or BidiClass.AN || c.IsRemovedByX9 || c.IsIsolateControl)"] },
        new("the direction of an embedding level (R for an odd level, L for an even one) is BidiRunSequence.DirectionOf: no rule works it out itself",
            @"&\s*1\)\s*==\s*1\s*\?\s*BidiClass\.R\s*:\s*BidiClass\.L",
            BidiFolder + "BidiAlgorithm.BidiRunSequence.cs") { Under = "Src/", Finds = [
            "        BidiClass sos = (Math.Max(before, level) & 1) == 1 ? BidiClass.R : BidiClass.L;",
            "        BidiClass embedding = (sequence.Level & 1) == 1 ? BidiClass.R : BidiClass.L;"] },
        new("the strong type before a position of an isolating run sequence (rules W2 and W7, the context of a bracket pair in N0) is BidiRunSequence.PrecedingStrong, sos when there is none: no rule walks back to it itself",
            @"for \(int \w+ = (?![\w.]*\.Count\b)[\w.]+ - 1; \w+ >= -?\d; \w+--\)",
            BidiFolder + "BidiAlgorithm.BidiRunSequence.cs") { Under = BidiFolder, Finds = [
            "            for (int j = k - 1; j >= -1; j--)",
            "                for (int k = pair.Open - 1; k >= 0; k--)",
            "        for (int i = start - 1; i >= 0; i--)"], Ignores = [
            "            for (int j = from; j >= 0 && (classes[j].IsRemovedByX9 || classes[j] == BidiClass.WS || classes[j].IsIsolateControl); j--)",
            "        for (int level = highest; level >= lowestOdd; level--)",
            "            for (int depth = openers.Count - 1; depth >= 0; depth--)"] },
        new("the end of a run of like types in an isolating run sequence (the terminators of W5, the neutrals of N1 and N2) is BidiRunSequence.RunEnd: no rule walks to it itself",
            @"while \(\w+ \+ 1 < \w+ && ",
            BidiFolder + "BidiAlgorithm.BidiRunSequence.cs") { Under = BidiFolder, Finds = [
            "            while (runEnd + 1 < n && types[idx[runEnd + 1]] == BidiClass.ET)",
            "            while (runEnd + 1 < n && IsNeutralOrIsolate(types[idx[runEnd + 1]]))"], Ignores = [
            "                while (end + 1 < items.Length && levels[end + 1] >= level)"] },
        OwnedRule.Nowhere("a rule over an isolating run sequence reads and rewrites its types through the sequence (BidiRunSequence holds the paragraph's types): no rule is handed the sequence and the types apart",
            @"RunSequence \w+, BidiClass\[\]", under: BidiFolder) with { Finds = [
            "    private static void W1NonspacingMarks(RunSequence sequence, BidiClass[] types)"] },
    ];

    private const string Owner = "Src/MatPlotLibNet/Extensions/InvariantTextExtensions.cs";

    private const string Verso = "Src/MatPlotLibNet.Verso/FigureFormatter.cs";

/// <summary>The DataFrame package's two conversions of a cell: to a number (a column read as doubles) and to a flag (a column read
    /// as the direction of an edge).</summary>
    private static readonly string[] DataFrame =
    [
        "Src/MatPlotLibNet.DataFrame/DataFrameColumnReader.cs",
        "Src/MatPlotLibNet.DataFrame/DataFrameFigureExtensions.cs",
    ];

/// <summary>The Interactive package's one number: the port of the local server in a chart's address.</summary>
    private const string Interactive = "Src/MatPlotLibNet.Interactive/ChartServer.cs";

    /// <summary>The rules of the area, in the order the scan reads them.</summary>
    internal static OwnedRule[] All { get; } =
    [
        OwnedRule.Nowhere("the label of a drawn line is drawn by DrawLineLabel in the theme's font for it (ThemeFontExtensions.LineLabelFont, the tick font in its line's colour) and stands where LineLabelPlacement puts it, 2 px off the line: no part chooses the font, restates its colour or the offset",
            @"[fF]ont with \{ Color = |DrawText\([^;]*[-+]\s*2\b|new Point\([^;]*[-+]\s*2\b|\b\w*[lL]abelFont\s*=\s*TickFont\(\)",
            under: "Src/MatPlotLibNet/Rendering/CartesianParts/") with { Finds = [
            "        var refLabelFont = TickFont();",
            "            ctx.Draw(TextLabel.Of(text), tickFont with { Color = color }, at);"] },
        new("the font an axis title is drawn in is Axis.TitleFontUnder(theme), the theme's axis label font with the axis' label style applied: no site rebuilds it",
            @"\bTitleFont\(\s*_?[tT]heme\.LabelFont\(\)\s*\)|\bLabelStyle\?\.ApplyTo\b|\bTitleFont\(\s*(?:baseFont|LabelFont\(\))\s*\)",
            "Src/MatPlotLibNet/Rendering/AxesTitleExtensions.cs") { Under = "Src/MatPlotLibNet/", Finds = [
            "            var font = Axes.XAxis.LabelStyle?.ApplyTo(baseFont) ?? baseFont;"] },
        SeriesTextSize,
        TextDrawnByTheVerb,
        PlainTextOfARichText,
        LineOfAFont,
        WhereAPlainLabelStands,
        SeriesMeasuredBySeam,
        BaseFontOfTheTheme,
        CentreDropOfTheBox,
        NoHandDropBelowACentre,
        NoHangByTheFontSize,
        NoShareOfALineForItsAscent,
        NoDropOfAnEdge,
        SeriesTextSizeByTheTheme,
        SeriesTextAtTheFontSizeReadOnce,
        FontMadeFromAFont,
        SizeNamedForARoleByTheRole,
        .. Bidi,
        new("a number becomes text, or text a number, in the invariant culture only in InvariantTextExtensions: no site names the invariant culture (to format, parse, convert or compose a number, or held in a field or a local), nor reaches for FormattableString.Invariant or an interpolation handler of its own",
            @"\bCultureInfo\.InvariantCulture\b|\b(?:NumberFormatInfo|DateTimeFormatInfo)\.InvariantInfo\b|\bFormattableString\.Invariant\b|\bnew DefaultInterpolatedStringHandler\(",
            [Owner, Verso, .. DataFrame, Interactive]) { Under = "Src/", Finds = [
            "        return value.ToString(\"G\", CultureInfo.InvariantCulture);",
            "            throw Refused($\"has a duration of {durations[i].ToString(CultureInfo.InvariantCulture)} at index {i}\");",
            "            double.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double ldx) &&",
            "            value.TryFormat(digits, out int written, \"G\", CultureInfo.InvariantCulture);",
            "                string label = string.Format(System.Globalization.CultureInfo.InvariantCulture, percentFormat, pct);",
            "        var culture = System.Globalization.CultureInfo.InvariantCulture;",
            "    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;",
            "        [new(\"FigureDto\", \"width\")] = FormattableString.Invariant($\"the width of the figure in pixels; absent for {Figure.RestingWidth}\"),",
            "            var text = new DefaultInterpolatedStringHandler(0, 0, System.Globalization.CultureInfo.InvariantCulture);",
            "        return string.Create(CultureInfo.InvariantCulture, $\"Sample {i + 1} has {count} defectives\");",
            "            text.Append(CultureInfo.InvariantCulture, $\", {count} points\");",
            "                _          => Convert.ToDouble(v, System.Globalization.CultureInfo.InvariantCulture)",
            "    private static readonly DateTimeFormatInfo Invariant = CultureInfo.InvariantCulture.DateTimeFormat;",
            "        var format = NumberFormatInfo.InvariantInfo;"], Ignores = [
            "        string label = $\"BB({period},{stdDevMultiplier.InvariantText()})\";",
            "        return string.Invariant($\"Sample {i + 1} has {count} defectives\");",
            "        var chartId = Guid.NewGuid().ToString(\"N\");"] },
        OwnedRule.Nowhere("a point of the 3-D vertex wire (data-v3d) and a text's pad are written by Normalized3DPoint (FormatWire, in FrameDigits or MarkDigits) and SceneVertexWriter.PadWire: no writer types the digits of a normalised coordinate or of a pad",
            @"\b(?:Nx|Ny|Nz)\b[^;]*(?:""G\d+""|:G\d+)|\b(?:pad\w*|labelOffset)\b[^;]*""G\d+""", under: "Src/") with { Finds = [
            "            vertices.TextAnchor(anchor, row.EdgeWire!, padPx.ToString(\"G6\", CultureInfo.InvariantCulture), row.Axis.OtherAxes());",
            "            sb.Append(CultureInfo.InvariantCulture, $\"{n.Nx:G6},{n.Ny:G6},{n.Nz:G6}\");",
            "                text.AppendFormatted(v.Nx, \"G4\");",
            "        string? pad = vertices.Writes ? labelOffset.InvariantText(\"G6\") : null;"], Ignores = [
            "        string label = $\"BB({period},{stdDevMultiplier.InvariantText()})\";",
            "        return string.Invariant($\"Sample {i + 1} has {count} defectives\");",
            "        var chartId = Guid.NewGuid().ToString(\"N\");"] },
        OwnedRule.Nowhere("a number is never written in a format without a culture: ToString(format) reads the culture of the machine",
            @"(?<!NewGuid\(\))\.ToString\(""[^""]*""\)", under: "Src/") with { Finds = [
            "        string text = value.ToString(\"0.##\");"], Ignores = [
            "        string label = $\"BB({period},{stdDevMultiplier.InvariantText()})\";",
            "        return string.Invariant($\"Sample {i + 1} has {count} defectives\");",
            "        var chartId = Guid.NewGuid().ToString(\"N\");"] },
        OwnedRule.Nowhere("no class keeps a private wrapper of the owner: a site calls InvariantText itself",
            @"\bprivate static string \w+\([^)]*\)\s*=>\s*[\w.()]+\.InvariantText\(\)", under: "Src/") with { Finds = [
            "    private static string Shown(int count) => count.InvariantText();"], Ignores = [
            "        string label = $\"BB({period},{stdDevMultiplier.InvariantText()})\";",
            "        return string.Invariant($\"Sample {i + 1} has {count} defectives\");",
            "        var chartId = Guid.NewGuid().ToString(\"N\");"] },
    ];
}
