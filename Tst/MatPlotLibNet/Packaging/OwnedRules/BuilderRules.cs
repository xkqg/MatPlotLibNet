// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging.OwnedRules;

/// <summary>The rules of the builders' verbs, which stand in the classes of their concerns: the axes a verb works on, the tail an
/// indicator verb ends with, the forward of a figure verb to its single axes, the spacing a layout verb edits and the export.</summary>
internal static class BuilderRules
{
    /// <summary>The primary axes a verb of the builder works on (the bottom X axis, slot 0 of the left Y axes, the depth axis) are
    /// the builder's <c>X</c>, <c>Y</c> and <c>Z</c>; a class of verbs reads them, it never builds the view of an axis itself.</summary>
    private static readonly OwnedRule PrimaryAxes = new(
        "the primary axes a builder verb works on are AxesBuilder.X, Y and Z: no class of verbs builds an AxisVerbs view of the bottom, the left slot 0 or the depth axis itself",
        @"\bAxisVerbs\.(?:BottomOf|DepthOf)\(|\bAxisVerbs\.Of\([^,]+,\s*[\w.]*YAxes\.Left\[0\]\)",
        "Src/MatPlotLibNet/Builders/AxesBuilder.cs") { Under = "Src/MatPlotLibNet/", Finds = [
        "    internal AxisVerbs X => AxisVerbs.BottomOf(_axes);",
        "            AxisVerbs.Of(axes.Axes, axes.Axes.YAxes.Left[0]).Label(label);",
        "        AxisVerbs.DepthOf(axes.Axes).Limits(min, max);"], Ignores = [
        "        _verbs = AxisVerbs.Of(axes, slot);",
        "        _top = AxisVerbs.TopOf(axes);"] };

    /// <summary>Every indicator verb of the builder ends in <c>ApplyIndicator</c>, which places the indicator on the axes (on the
    /// centres of the bar slots when the axes slots bars) before the callback sees it.</summary>
    private static readonly OwnedRule IndicatorTail = new(
        "an indicator verb of the builder places its indicator through ApplyIndicator: no verb calls Place(indicator, configure) on the axes itself",
        @"\.Place\(\s*indicator\b",
        "Src/MatPlotLibNet/Indicators/AxesBuilderIndicatorVerbs.cs") { Under = "Src/MatPlotLibNet/", Finds = [
        "        _axes.Place(indicator, configure);",
        "            axes.Axes.Place(indicator, configure);"], Ignores = [
        "            => axes.ApplyIndicator(new Indicators.Sma(axes.Axes.PriceData, period), configure);",
        "        return AxesLegend().Place(entries, PlotArea, obstacles, drawnPast);"] };

    /// <summary>A figure verb that draws on the figure's single axes is the axes builder's verb of the same name, recorded through
    /// <c>OnDefaultAxes</c>; the forwards stand in one class.</summary>
    private static readonly OwnedRule DefaultAxesForward = new(
        "a figure verb on the figure's single axes forwards to the axes builder's verb through OnDefaultAxes, in FigureBuilderDefaultAxesVerbs only",
        @"\bOnDefaultAxes\(\s*\w+\s*=>",
        "Src/MatPlotLibNet/Builders/FigureBuilderDefaultAxesVerbs.cs") { Under = "Src/MatPlotLibNet/", Finds = [
        "        OnDefaultAxes(ax => ax.Plot(x, y, configure));",
        "            => figure.OnDefaultAxes(ax => ax.Kde(data, configure));"], Ignores = [
        "    internal FigureBuilder OnDefaultAxes(Action<AxesBuilder> step)",
        "            foreach (var step in _defaultAxesSteps) step(defaultAxesBuilder);"] };

    /// <summary>A layout verb of the figure edits the spacing the calls before it left (a new one when none did), in one member of the
    /// layout verbs.</summary>
    private static readonly OwnedRule LayoutSpacing = new(
        "a layout verb of the figure edits the spacing the calls before it left, in FigureBuilderLayoutVerbs only",
        @"\b_?[Ss]pacing\s*\?\?\s*new\b",
        "Src/MatPlotLibNet/Builders/FigureBuilderLayoutVerbs.cs") { Under = "Src/MatPlotLibNet/", Finds = [
        "    public FigureBuilder TightLayout() { _spacing = (_spacing ?? new()) with { TightLayout = true }; return this; }",
        "        private SubPlotSpacing SpacingSoFar => figure.Spacing ?? new();"], Ignores = [
        "        if (Spacing is not null) figure.Spacing = Spacing;",
        "            figure.Spacing = figure.SpacingSoFar with { TightLayout = true, LayoutRect = rect };"] };

    /// <summary>A builder's export builds the figure and hands it to the figure's own export, in the export verbs of the builder.</summary>
    private static readonly OwnedRule BuildThenExport = new(
        "a builder exports by building the figure and calling the figure's export, in the export verbs of each builder only",
        @"\bBuild\(\)\.(?:ToSvg|ToJson|Save)\(",
        "Src/MatPlotLibNet/Builders/FigureBuilderExportVerbs.cs", "Src/MatPlotLibNet/Builders/MosaicFigureBuilder.cs") { Under = "Src/MatPlotLibNet/", Finds = [
        "    public string ToSvg() => Build().ToSvg();",
        "        public void Save(string path) => figure.Build().Save(path);"], Ignores = [
        "        public string ToSvg() => SvgRendererDefault.Current.Render(figure);",
        "    public Figure Build()"] };

    /// <summary>The rules of the area, in the order the scan reads them.</summary>
    internal static OwnedRule[] All { get; } = [PrimaryAxes, IndicatorTail, DefaultAxesForward, LayoutSpacing, BuildThenExport];
}
