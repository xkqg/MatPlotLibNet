// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>The scan <see cref="RestingProse"/> runs over the documentation: a start a <c>Resting</c> or <c>Default</c> constant owns,
/// typed again as a number in a sentence, is found in the type that declares the constant and in every type that reads it, whatever
/// file either stands in; a cref, another number and a kept line are not.</summary>
public class RestingProseTests
{
    /// <summary>A file of the type <c>Node</c>: the class around <paramref name="members"/>, one to a line from line 2.</summary>
    private static SourceFile Node(params string[] members) =>
        new("Src/MatPlotLibNet/Models/Node.cs", "public class Node {\n" + string.Join('\n', members) + "\n    public double Width { get; set; }\n}");

    [Theory]
    [InlineData("    /// <summary>Width of each node rectangle, in pixels. Default 20.</summary>")]
    [InlineData("    /// <summary>The width of a node (default 20).</summary>")]
    [InlineData("    /// <summary>The width of a node; 20 unless said otherwise.</summary>")]
    [InlineData("    /// <summary>A node given no width reads 20; a width that is given, 20 included, is drawn as given.</summary>")]
    [InlineData("    /// <summary>The width of a node, 20 under every other theme.</summary>")]
    [InlineData("    /// <summary>The width of a node. Default <c>20</c>.</summary>")]
    [InlineData("    /// <summary>The width of a node; the default, 20, asks for nothing.</summary>")]
    public void AStartTypedAgainBesideItsOwnConstant_IsFound(string line)
    {
        var file = Node("    private const double RestingWidth = 20;", line);

        Assert.Contains(new RestingProse([file], []).Violations(), f => f.Contains("Node.cs:3", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("    private const SankeyOrientation RestingOrient = SankeyOrientation.Horizontal;", "    /// <summary>Diagram orientation. Default <see cref=\"SankeyOrientation.Horizontal\"/>.</summary>")]
    [InlineData("    private const bool RestingShow = true;", "    /// Default <see langword=\"true\"/>.</summary>")]
    [InlineData("    private const double RestingAlpha = 0.67;", "    /// <summary>How opaque the ribbons are; 0.67 unless said otherwise.</summary>")]
    [InlineData("    private const int RestingCount = 10_000;", "    /// <summary>The points kept. Default 10,000.</summary>")]
    [InlineData("    private const SankeyOrientation RestingOrient = SankeyOrientation.Horizontal;", "    /// <summary>Diagram orientation. Default\n    /// <see cref=\"SankeyOrientation.Horizontal\"/>.</summary>")]
    [InlineData("    private const HistType RestingHistType = HistType.Bar;", "    /// <summary>How the bins are drawn: as separate bars (<c>Bar</c>, the default) or as a step.</summary>")]
    public void AStartOfAnyKindTypedAgain_IsFound(string constant, string line)
    {
        Assert.Contains(new RestingProse([Node(constant, line)], []).Violations(), f => f.Contains("Node.cs:3", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("    /// <summary>Width of each node rectangle, in pixels. Default <see cref=\"RestingWidth\"/>.</summary>")]
    [InlineData("    /// <summary>A width that is given, the default included, is drawn as given.</summary>")]
    [InlineData("    /// <summary>The opacity, from 0 (clear) to 20 (solid).</summary>")]
    [InlineData("    /// <summary>The gap between nodes. Default 10.</summary>")]
    [InlineData("    /// <summary>A width of 0 by default draws nothing.</summary>")]
    [InlineData("    /// <summary>The edge, matplotlib's <c>'20'</c> under the default style, a grey given as text.</summary>")]
    [InlineData("    /// <summary>The colour of a node's outline, a 20-point star's included.</summary>")]
    [InlineData("    /// <summary>matplotlib's <c>axes.linewidth</c>, 20 pt under the default\n    /// style.</summary>")]
    [InlineData("    /// <summary>matplotlib's <c>axes.linewidth</c>, 1.0 pt under the classic style and 20 pt under the default one.</summary>")]
    [InlineData("    /// <summary>The share it covers, between 0 and 20: the default <see cref=\"RestingShare\"/>.</summary>")]
    [InlineData("    /// <summary>The margin: <c>Classic</c> → 0, <c>Default</c> → 20.</summary>")]
    [InlineData("    /// <summary>The look matplotlib has had by default since version 20.0.</summary>")]
    [InlineData("    /// <summary>The diagonal is an if/else cascade, only 20 kinds.</summary>")]
    [InlineData("    /// <summary>The scale the volume is divided by. Default 20^6.</summary>")]
    [InlineData("    /// <summary>The value that reads as 20 %.</summary>")]
    [InlineData("    /// <summary>The dots, as matplotlib's <c>size=20</c> by default.</summary>")]
    [InlineData("    /// <summary>A mark at 1, 2 and 5 (1), else every one (20).</summary>")]
    [InlineData("    /// <summary>The share it spans: 1 (the default) spans the side, 20 a part of it.</summary>")]
    [InlineData("    /// <summary>The gap between nodes. Default 20.</summary>\n    public double Gap { get; set; } = 20;")]
    [InlineData("    /// <param name=\"bands\">How many bands fold each side of zero into.\n    /// Default 20.</param>\n    public void Fold(int bands = 20) { }")]
    public void AStartNamedByItsConstant_OrAnotherNumber_IsNoSecondSpelling(string line)
    {
        var file = Node("    private const double RestingWidth = 20;", "    private const double RestingZero = 0;", line);

        Assert.Empty(new RestingProse([file], []).Violations());
    }

    /// <summary>Rules of a line's width that rest at a value: one made by position through the constant, one made <c>with</c> it and
    /// a width of its own, one made <c>with</c> it and nothing more.</summary>
    private const string WidthRules = "internal sealed record LineWidthRule(double Resting, bool Cycled = false) {\n"
        + "    internal static LineWidthRule Line { get; } = new(SeriesLineLook.LibraryWidth, Cycled: true);\n"
        + "    internal static LineWidthRule LineCollection { get; } = Line with { Cycled = false };\n"
        + "    internal static LineWidthRule OnePixelCollection { get; } = LineCollection with { Resting = 1 };\n}\n"
        + "internal sealed record SeriesLineLook {\n    internal const double LibraryWidth = 1.5;\n}";

    /// <summary>A constant belongs to the type that declares it, not to the file it stands in, and a line is read against every type
    /// its own type names, whatever file the line stands in and however the members around it are laid out: a second type in a file
    /// named after another, a partial type over two files, a verb that reads the constant through one that reads another, a type
    /// named by a call or by a <c>cref</c>, and a line beside a member that reads nothing itself, before and after it moved files. A
    /// start is owned under any name: a constant that is not called <c>Resting</c>, a rule that rests at a value (made by position,
    /// <c>with</c> a width or <c>with</c> nothing), a resting record made by position, a resting width in points, and the start of a
    /// property typed in another type.</summary>
    [Theory]
    [InlineData("Src/MatPlotLibNet/Models/Axes.cs", "public class Axes { }\npublic sealed class Legend {\n    internal const double RestingFrameAlpha = 0.8;\n}",
        "Src/MatPlotLibNet/Styling/LegendLook.cs",
        "public sealed record LegendLook {\n    /// <summary>How opaque the frame is, 0.8 unless said otherwise.</summary>\n    public double FrameAlpha { get; init; } = Legend.RestingFrameAlpha;\n}")]
    [InlineData("Src/MatPlotLibNet/Models/Legend.cs", "public sealed partial class Legend {\n    internal const double RestingFrameAlpha = 0.8;\n}",
        "Src/MatPlotLibNet/Models/LegendFrame.cs",
        "public sealed partial class Legend {\n    /// <summary>How opaque the frame is, 0.8 unless said otherwise.</summary>\n    public double FrameAlpha { get; set; }\n}")]
    [InlineData("Src/MatPlotLibNet/Models/Series/Bars.cs", "public class Bars {\n    internal const int RestingBins = RestingOther;\n    private const int RestingOther = 10;\n}",
        "Src/MatPlotLibNet/Builders/Verbs.cs",
        "public static class Verbs {\n    /// <param name=\"bins\">The number of bins (default 10).</param>\n    public static AxesBuilder Hist(double[] data, int bins = Bars.RestingBins) => null!;\n}")]
    [InlineData("Src/MatPlotLibNet/Models/YAxisContainer.cs", "public sealed class YAxisContainer {\n    private const double RestingTitleGap = 4;\n}",
        "Src/MatPlotLibNet/Builders/Verbs.cs",
        "public static class Verbs {\n    /// <summary>Sets the gap between a title and its labels; the default is 4.</summary>\n    public static void Gap(Axes axes, double pixels) => axes.Gap = YAxisContainer.ValidTitleGap(pixels);\n}")]
    [InlineData("Src/MatPlotLibNet/Models/Series/Scatter.cs", "public class Scatter {\n    private const double RestingMarkerSize = 36;\n}",
        "Src/MatPlotLibNet/Styling/ScatterLook.cs",
        "/// <summary>How a <see cref=\"Scatter\"/> looks.</summary>\n/// <param name=\"Area\">The area of a marker given no size, 36 under the library's own themes.</param>\npublic sealed record ScatterLook(double Area);")]
    [InlineData("Src/MatPlotLibNet/Rendering/SymlogTransform.cs", "internal sealed class SymlogTransform {\n    internal const double DefaultLinThresh = 1.0;\n}",
        "Src/MatPlotLibNet/Builders/AxesBuilder.cs",
        "public sealed class AxesBuilder {\n    /// <summary>Symmetric log, linear within 1 under the library's own.</summary>\n    public AxesBuilder WithSymlogYScale()\n    {\n        return this;\n    }\n\n"
        + "    public AxesBuilder WithSymlogYScale(double linthresh = Rendering.SymlogTransform.DefaultLinThresh)\n    {\n        return this;\n    }\n}")]
    [InlineData("Src/MatPlotLibNet/Rendering/SymlogTransform.cs", "internal sealed class SymlogTransform {\n    internal const double DefaultLinThresh = 1.0;\n}",
        "Src/MatPlotLibNet/Builders/AxesBuilderScaleVerbs.cs",
        "public sealed class AxesBuilder {\n    /// <summary>Symmetric log, linear within 1 under the library's own.</summary>\n    public AxesBuilder WithSymlogYScale() { return this; }\n"
        + "    public AxesBuilder WithSymlogYScale(double linthresh = Rendering.SymlogTransform.DefaultLinThresh) { return this; }\n}")]
    [InlineData("Src/MatPlotLibNet/Styling/Looks/SeriesLineLook.cs", "internal sealed record SeriesLineLook {\n    internal const double LibraryWidth = 1.5;\n}",
        "Src/MatPlotLibNet/Models/Series/XY/LineSeries.cs",
        "public class LineSeries {\n    /// <summary>The thickness of the line, in pixels. Default 1.5.</summary>\n    public double LineWidth { get; set; } = SeriesLineLook.LibraryWidth;\n}")]
    [InlineData("Src/MatPlotLibNet/Styling/LineWidthRule.cs", WidthRules,
        "Src/MatPlotLibNet/Models/Series/Categorical/EventplotSeries.cs",
        "public class EventplotSeries {\n    /// <summary>A width that was given, 1 included, is drawn as given.</summary>\n    LineWidthRule IWidthGiven.WidthRule => LineWidthRule.OnePixelCollection;\n}")]
    [InlineData("Src/MatPlotLibNet/Styling/LineWidthRule.cs", WidthRules,
        "Src/MatPlotLibNet/Models/Series/Financial/OhlcBarSeries.cs",
        "public class OhlcBarSeries {\n    /// <summary>A bar given no width reads 1.5.</summary>\n    LineWidthRule IWidthGiven.WidthRule => LineWidthRule.LineCollection;\n}")]
    [InlineData("Src/MatPlotLibNet/Styling/RestingDot.cs", "internal readonly record struct RestingDot(double Diameter, double Alpha = 1, double Jitter = 0);",
        "Src/MatPlotLibNet/Models/Series/Distribution/StripplotSeries.cs",
        "public sealed class StripplotSeries {\n    /// <summary>The opacity of the dots. Default 0.8.</summary>\n    public double Alpha => Rests.Alpha;\n    private static readonly RestingDot Rests = new(5, 0.8, 0.2);\n}")]
    [InlineData("Src/MatPlotLibNet/Rendering/LogLabelThresholds.cs",
        "internal readonly record struct LogLabelThresholds(double Powers, double Decades) {\n    internal static readonly LogLabelThresholds Default = new(1, 0.4);\n}",
        "Src/MatPlotLibNet/Styling/Looks/TickLabelLook.cs",
        "public sealed record TickLabelLook {\n    /// <summary>Which ticks are named. Unset: 0.4 of a decade (<see cref=\"LogLabelThresholds\"/>).</summary>\n    public LogLabelThresholds? Thresholds { get; init; }\n}")]
    [InlineData("Src/MatPlotLibNet/Models/Axis.cs", "public sealed record TickMark {\n    private static readonly double RestingWidth = 0.8.PointsInPixels;\n}",
        "Src/MatPlotLibNet/Styling/Looks/AxisTickLook.cs",
        "public sealed record AxisTickLook {\n    /// <summary>How wide a tick of a <see cref=\"TickMark\"/> is. Unset: 0.8 pt.</summary>\n    public double? Width { get; init; }\n}")]
    [InlineData("Src/MatPlotLibNet/Styling/Looks/TableLook.cs", "public sealed record TableLook {\n    public double PadPixels { get; init; } = 4;\n}",
        "Src/MatPlotLibNet/Models/Series/Special/TableSeries.cs",
        "public sealed class TableSeries {\n    /// <summary>The space beside a text; given none, 4 pixels under the library's own themes.</summary>\n    public double CellPadding => TableLook.Library.PadOver(null, 0);\n}")]
    public void AStartTypedAgainInATypeThatDeclaresOrNamesIt_IsFound_WhateverFileAndLayout(string path, string declaring, string linePath, string line)
    {
        var findings = new RestingProse([new(path, declaring), new(linePath, line)], []).Violations();

        Assert.Contains(findings, f => f.Contains(linePath + ":2", StringComparison.Ordinal));
    }

    /// <summary>A type that neither declares nor names a constant's type types its number as another quantity of its own, even in the
    /// file that declares the constant.</summary>
    [Fact]
    public void ANumberInATypeThatNeitherDeclaresNorNamesTheConstantsType_IsNoSecondSpelling()
    {
        var file = new SourceFile("Src/MatPlotLibNet/Models/Axes.cs",
            "public class Axes {\n    /// <summary>How opaque the face is, 0.8 unless said otherwise.</summary>\n    public double Alpha { get; set; }\n}\n"
            + "public sealed class Legend {\n    internal const double RestingFrameAlpha = 0.8;\n}");

        var prose = new RestingProse([file], []);

        Assert.Empty(prose.Violations());
        Assert.Equal("0.8", prose.ValueOf("Legend", "RestingFrameAlpha"));
        Assert.Null(prose.ValueOf("Axes", "RestingFrameAlpha"));
    }

    [Theory]
    [InlineData("    /// <summary>The size of the triangle in pixels; absent for 12.</summary>")]
    [InlineData("    /// <summary>Whether the connectors are drawn; absent for <see langword=\"true\"/>.</summary>")]
    [InlineData("    /// <summary>The degree of the fit; a regression leaves it out for 1.</summary>")]
    [InlineData("    /// <summary>Cartesian, polar, three-D or ternary; absent for the default <c>Cartesian</c>.</summary>")]
    [InlineData("    /// <summary>The number of rows of a waffle chart's grid; left out at the default of 10.</summary>")]
    [InlineData("    /// <summary>The gap between a title and its labels. Absent at the default of 4, which a JSON without it reads as.</summary>")]
    [InlineData("    /// <summary>The size of the head; absent reads as 12.</summary>")]
    [InlineData("    /// <summary>The number of levels a contour plot divides its values into; absent for\n    /// 10.</summary>")]
    public void AWireNoteThatTypesTheValueItLeavesOut_IsFound(string line)
    {
        var findings = new RestingProse([new("Src/MatPlotLibNet/Serialization/AnyDto.cs", line)], []).Violations();

        Assert.Contains(findings, f => f.Contains("AnyDto.cs:1", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("    /// <summary>The angle the text is turned through; absent for 0.</summary>")]
    [InlineData("    /// <summary>The bins; absent for <see cref=\"Models.Series.Bars.RestingBins\"/>.</summary>")]
    [InlineData("    /// <summary>The colour of the text; absent for the theme's text colour.</summary>")]
    public void AWireNoteThatNamesTheValueItLeavesOut_IsNoSecondSpelling(string line)
    {
        Assert.Empty(new RestingProse([new("Src/MatPlotLibNet/Serialization/AnyDto.cs", line)], []).Violations());
    }

    [Fact]
    public void AKeptLine_IsNoFinding_AndAKeptEntryThatMatchesNothing_IsNamedAsStale()
    {
        var file = Node("    private const double RestingWidth = 20;", "    /// <summary>matplotlib's own: 20 by default.</summary>");
        KeptProse kept = new("Src/MatPlotLibNet/Models/Node.cs", "matplotlib's own: 20", "matplotlib's number");
        KeptProse stale = new("Src/MatPlotLibNet/Models/Node.cs", "no such line", "gone");

        var findings = new RestingProse([file], [kept, stale]).Violations();

        Assert.Equal("Src/MatPlotLibNet/Models/Node.cs: the kept line 'no such line' no longer types an owner's number (or is gone): remove the entry", Assert.Single(findings));
    }

    [Fact]
    public void TheValueOfAConstantThatReadsAnother_IsTheOthersValue()
    {
        var a = new SourceFile("Src/A/First.cs", "class First {\n    internal const double RestingSize = Second.RestingSize;\n}");
        var b = new SourceFile("Src/A/Second.cs", "class Second {\n    internal const double RestingSize = 6.0;\n    internal const string DefaultName = \"x\";\n}");

        var prose = new RestingProse([a, b], []);

        Assert.Equal("6", prose.ValueOf("First", "RestingSize"));
        Assert.Null(prose.ValueOf("Second", "DefaultName"));
        Assert.Null(prose.ValueOf("Second", "RestingMissing"));
    }
}
