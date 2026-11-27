// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging.OwnedRules;

/// <summary>A paragraph of documentation that says how a rule works is written on the thing that owns the rule, once. A member that
/// follows the rule says what is its own and names the owner with a <c>cref</c>; it never types the contract again. These rows read
/// the XML documentation, so a copy of a contract's sentences on the next member is found by the sentence, wherever the member is.</summary>
internal static class DocumentationRules
{
    /// <summary>What a series' own <c>FromSeriesDto</c> takes and gives is said by <c>SeriesRegistry.Register</c>, which takes it as the
    /// reader of the series: the axes it is added to, the record that carries it, the series it hands back. The same three lines on each
    /// of the ninety-odd readers, and on the extensions that restore a part of the record, said it again on every one.</summary>
    private static readonly OwnedRule FromSeriesDtoContract = OwnedRule.InDocumentation(
        "what a series' FromSeriesDto takes and returns (the axes, the record, the series it hands back) is said once, by SeriesRegistry.Register: no reader of a record repeats the three lines",
        @"<param name=""(?:axes|dto)"">The (?:target axes the reconstructed series is added to|serialization DTO carrying the series' persisted properties)\.</param>|<returns>The reconstructed series instance\.</returns>",
        under: "Src/") with { Finds = [
        "    /// <param name=\"axes\">The target axes the reconstructed series is added to.</param>",
        "    /// <param name=\"dto\">The serialization DTO carrying the series' persisted properties.</param>",
        "    /// <returns>The reconstructed series instance.</returns>"], Ignores = [
        "    /// <param name=\"axes\">The axes the series is added to.</param>",
        "    /// <param name=\"dto\">The record of the series.</param>",
        "    /// <returns>The series made, or <see langword=\"null\"/> when none was.</returns>"] };

    /// <summary>That a series' own <c>FromSeriesDto</c> reads a record and adds the series it makes to the axes is the contract
    /// <c>SeriesRegistry.Register</c> says. A reader's summary names the registry and says only what the reader restores of its own;
    /// it never says again that the series is rebuilt from its record and added to the axes.</summary>
    private static readonly OwnedRule FromSeriesDtoSummary = OwnedRule.InDocumentation(
        "that a series' FromSeriesDto rebuilds the series from its record and adds it to the axes is said once, by SeriesRegistry.Register: no reader's summary says it again, it names the registry",
        @"<summary>Reconstructs an? <see cref=""[A-Za-z0-9_.]+""/> (?:from its serialization DTO|at the capacity)|<summary>Rebuilds a map from its DTO",
        under: "Src/") with { Finds = [
        "    /// <summary>Reconstructs a <see cref=\"LineSeries\"/> from its serialization DTO and adds it to the axes.</summary>",
        "    /// <summary>Reconstructs a <see cref=\"StreamingLineSeries\"/> at the capacity and the snapshot it was written with, and adds it to the axes.</summary>"], Ignores = [
        "    /// <summary>The reader of a <see cref=\"LineSeries\"/> that <see cref=\"SeriesRegistry\"/> takes.</summary>"] };

    /// <summary>What an indicator's <c>Spec</c> factory is for is said by <c>IndicatorSpec</c>, the type it makes: the label, the lines and
    /// the first value both forms of the indicator read. The paragraph on each of the fifty-odd factories said it again.</summary>
    private static readonly OwnedRule SpecParagraph = OwnedRule.InDocumentation(
        "what an indicator's Spec factory is for (one label, its lines and its first value for the batch form and the streaming form) is said once, by IndicatorSpec: no factory repeats the paragraph",
        @"What both forms of the indicator say of these parameters|read it, so neither checks or names on its own",
        under: "Src/") with { Finds = [
        "    /// <summary>What both forms of the indicator say of these parameters, after checking them: its label, its lines and its first value",
        "    /// (<see cref=\"IndicatorSpec\"/>). The batch constructor and the streaming constructor read it, so neither checks or names on its own.</summary>"], Ignores = [
        "    /// <summary>What an indicator's batch form and its streaming form both say from the parameters they are made with, said once."] };

    /// <summary>What a shelf of colour maps is for is said by <c>ColorMapShelf</c>: each property adds the map it makes, so the list of maps
    /// is the list of properties. The paragraph on the shelf of each of the seven classes said it again.</summary>
    private static readonly OwnedRule ShelfParagraph = OwnedRule.InDocumentation(
        "what a class's shelf of colour maps is for (each property adds its map, so the list of maps is the list of properties) is said once, by ColorMapShelf: no class repeats the paragraph",
        @"The maps this class declares, in the order it declares them|registry reads the shelf and the list of maps",
        under: "Src/") with { Finds = [
        "    /// <summary>The maps this class declares, in the order it declares them: each property adds the map it makes, so the",
        "    /// registry reads the shelf and the list of maps is the list of properties, written once. Declared before the maps.</summary>"] };

    /// <summary>That an internal interface adds nothing public to a public type is the rule <c>PublicInterfaceContractTests</c> holds, and
    /// the keyword says the interface is internal: a remark on each of the twelve that says it again is a second statement of the rule.</summary>
    private static readonly OwnedRule InternalInterfaceRemark = OwnedRule.InDocumentation(
        "that an internal interface adds no public member to a public type is held by PublicInterfaceContractTests: no interface repeats the sentence in a remark",
        @"no public member is added to a public type",
        under: "Src/") with { Finds = [
        "/// <remarks>Internal, implemented explicitly: no public member is added to a public type.</remarks>"], Ignores = [
        "/// <remarks>Implemented explicitly once, by <see cref=\"ChartSeries\"/>.</remarks>"] };

    /// <summary>The width a line is drawn at when it was given none is said by <c>LineSeries.LineWidth</c>; the series that draw a line
    /// the same way inherit its documentation, and one that rests at another width names its rule. Read in each spelling of the theme's
    /// width: under the matplotlib themes, drawn by them, or as the look that holds it.</summary>
    private static readonly OwnedRule LineWidthParagraph = new(
        "the width a line is drawn at when it was given none (the theme's, matplotlib's lines.linewidth under the matplotlib themes) is said once, by LineSeries.LineWidth: a series that draws its line the same way inherits the documentation, and one that rests at another width names its rule",
        @"lines\.linewidth</c> under the matplotlib themes|themes draw matplotlib's <c>lines\.linewidth</c>|<c>SeriesLineLook\.Width</c>",
        "Src/MatPlotLibNet/Models/Series/XY/LineSeries.cs") { ReadsDocumentation = true, Under = "Src/", Finds = [
        "    /// says: matplotlib's <c>lines.linewidth</c> under the matplotlib themes (<c>SeriesLineLook.Width</c>), 1.5",
        "    /// line (the matplotlib themes draw matplotlib's <c>lines.linewidth</c>, which <c>eventplot</c>'s lines take: 1.5 pt under the",
        "    /// <see cref=\"LineSeries\"/> is (<c>SeriesLineLook.Width</c> under the matplotlib themes); a width that is given, 1.5"], Ignores = [
        "    /// <inheritdoc cref=\"LineSeries.LineWidth\"/>",
        "    /// <summary>The width of each event line, in pixels, read and drawn as a line's (<see cref=\"LineSeries.LineWidth\"/>) but resting at",
        "    /// matplotlib's <c>lines.linewidth</c>, 1.5 pt under the default style and 1.0 pt under the classic one; a style's"] };

    /// <summary>The colour of the line a reference line, a level and a trend line are drawn in is said by <c>ReferenceLine.Color</c>; the
    /// other two inherit its documentation.</summary>
    private static readonly OwnedRule LineColorParagraph = new(
        "the colour of the line of a drawn annotation line (the theme's neutral colour when none was given) is said once, by ReferenceLine.Color: the level and the trend line inherit the documentation",
        @"<summary>The colour of the line and its label\. ",
        "Src/MatPlotLibNet/Models/ReferenceLine.cs") { ReadsDocumentation = true, Under = "Src/", Finds = [
        "    /// <summary>The colour of the line and its label. <see langword=\"null\"/> (the default) takes the theme's neutral"], Ignores = [
        "    /// <summary>The colour of the line and the dots."] };

    /// <summary>That a series keeps the copy of its data a draw last read, for its kept drawing and its kept estimate to share, is said by
    /// <c>IKeepsDataCopy</c>: the field that holds the copy is private and carries the type that says what it is.</summary>
    private static readonly OwnedRule DataCopyField = OwnedRule.InDocumentation(
        "that a series keeps the copy of its data a draw last read, shared by its kept drawing and its kept estimate, is said once, by IKeepsDataCopy: no field that holds the copy repeats it",
        @"<summary>The copy of the data a draw last read",
        under: "Src/") with { Finds = [
        "    /// <summary>The copy of the data a draw last read (<see cref=\"DataSnapshot.Of\"/>), shared by the kept drawing of the subplot"], Ignores = [
        "/// <summary>An object that keeps the last copy of its data it was handed (<see cref=\"DataSnapshot.Of\"/>), because a draw reads a"] };

    /// <summary>What the outline of a projection is, the two outer meridians from pole to pole, is said by
    /// <c>ProjectionOutlineExtensions.MeridianOutline</c>, which the five projections that draw it call.</summary>
    private static readonly OwnedRule MeridianOutlineRemark = OwnedRule.InDocumentation(
        "what the outline of a projection is (the two outer meridians, from pole to pole) is said once, by ProjectionOutlineExtensions.MeridianOutline: no projection repeats it in a remark",
        @"<remarks>The two outer meridians, from pole to pole\.</remarks>",
        under: "Src/") with { Finds = [
        "    /// <remarks>The two outer meridians, from pole to pole.</remarks>"] };

    /// <summary>How a legend handle is edged is said by <c>LegendSwatch.EdgedPatch</c>, which each series that wears it names; the remark
    /// on each of those series said it again.</summary>
    private static readonly OwnedRule EdgedSwatchRemark = OwnedRule.InDocumentation(
        "how the legend handle of a series that draws bars is edged is said once, by LegendSwatch.EdgedPatch: no series repeats it in a remark",
        @"<remarks>Edged as its bars are under the matplotlib themes",
        under: "Src/") with { Finds = [
        "    /// <remarks>Edged as its bars are under the matplotlib themes: the theme's edge, black under classic.</remarks>"] };

    /// <summary>Which artist a sampled signal is drawn as is said by <c>MatplotlibArtist.Line</c>, which each signal series names.</summary>
    private static readonly OwnedRule SignalArtistRemark = OwnedRule.InDocumentation(
        "which artist a sampled signal is drawn as is said once, by MatplotlibArtist.Line: no signal series repeats it in a remark",
        @"<remarks>2: a sampled signal is drawn as",
        under: "Src/") with { Finds = [
        "    /// <remarks>2: a sampled signal is drawn as <c>plot</c> draws a line, a <c>Line2D</c>.</remarks>"] };

    /// <summary>What a native control's handler, its failures and its clicked point are is said by the <c>InteractionHost</c> that has
    /// them; the surface of each package and the MAUI view inherit the documentation. Seven controls said the same paragraphs, and four
    /// said what an animation source does to them (that is the <c>IAnimationSource</c>'s own documentation).</summary>
    private static readonly OwnedRule NativeControlParagraphs = new(
        "what a native control's handler, its failures and its clicked point are is said once, by InteractionHost, and what an animation source does to a control by IAnimationSource: no control repeats the paragraphs, it inherits the documentation",
        @"Told of every gesture of the reader|Raised when <see cref=""ChartEventHandler""/> fails|Raised when the reader (?:clicks|taps) a data point|Gets or sets the animation source whose|Gets or sets whether the reader can pan, zoom and rotate",
        "Src/MatPlotLibNet/Interaction/InteractionHost.cs") { ReadsDocumentation = true, Under = "Src/", Finds = [
        "    /// <summary>Told of every gesture of the reader once it has reached the figure (or the server sink), whichever kind it",
        "    /// <summary>Raised when <see cref=\"ChartEventHandler\"/> fails. The gesture has been applied and the next one is taken as",
        "    /// <summary>Raised when the reader taps a data point, with the point they tapped: the series it belongs to, its place in",
        "    /// <summary>Gets or sets the animation source whose <see cref=\"IAnimationSource.FrameReady\"/>",
        "    /// <summary>Gets or sets whether the reader can pan, zoom and rotate the chart. A chart the reader has moved stays"], Ignores = [
        "    /// <inheritdoc cref=\"InteractionHost.Handler\"/>"] };

    /// <summary>The rules of the area, in the order the scan reads them.</summary>
    internal static OwnedRule[] All { get; } =
    [
        FromSeriesDtoContract,
        FromSeriesDtoSummary,
        SpecParagraph,
        ShelfParagraph,
        InternalInterfaceRemark,
        LineWidthParagraph,
        LineColorParagraph,
        DataCopyField,
        MeridianOutlineRemark,
        EdgedSwatchRemark,
        SignalArtistRemark,
        NativeControlParagraphs,
    ];
}
