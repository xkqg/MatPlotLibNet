// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging.OwnedRules;

/// <summary>Rules of the test code itself, read over every file under <c>Tst</c>.</summary>
internal static class TestCodeRules
{
    /// <summary>The text of a call up to the point where a term is looked for, at any depth of brackets: any run of characters, where a
    /// bracket that closes a group opened inside the text is allowed and the bracket that closes the call itself is not, so a scan never
    /// leaves the call it began in.</summary>
    private const string InsideTheCall = @"(?:[^()]|(?<open>\()|(?<-open>\)))*?";

    /// <summary>A test of a number that is true of a missing value or an infinity, as a call or as a method group.</summary>
    private const string NotFinite = @"double\.(?:IsNaN|IsInfinity)\b";

    /// <summary>A test that is true of a number that is finite, as a call or as a method group.</summary>
    private const string Finite = @"double\.IsFinite\b";

    /// <summary>A test that is true of a number that is not finite, said either way.</summary>
    private const string SaidNotFinite = "(?:" + NotFinite + @"|!\s*" + Finite + ")";

    /// <summary>A predicate over a sequence: a lambda that names the item, or a method group.</summary>
    private const string OfItems = @"\(\s*(?:\w+\s*=>\s*)?";

    /// <summary>The rules of the area, in the order the scan reads them.</summary>
    internal static OwnedRule[] All { get; } =
    [
        new("the repository root is found once, by walking up from the test assembly: every suite reads it from RepositoryRoot",
            @"File\.Exists\(.*""(?:CHANGELOG\.md|MatPlotLibNet\.slnx?|icon\.png)""",
            "Tst/MatPlotLibNet/Packaging/RepositoryRoot.cs") { Under = "Tst/", Finds = [
            "    while (!File.Exists(Path.Combine(dir.FullName, \"CHANGELOG.md\")))"] },
        new("a test assembly's own folder is walked up from in RepositoryRoot only",
            @"new DirectoryInfo\(AppContext\.BaseDirectory\)",
            "Tst/MatPlotLibNet/Packaging/RepositoryRoot.cs") { Under = "Tst/", Finds = [
            "    var dir = new DirectoryInfo(" + "AppContext.BaseDirectory);"] },
        new("the repository tree is walked by TreeWalk only, which never enters a folder of build output, and read through RepositoryTree, which lists each set of files once: no test goes below a folder by itself (the two readers of the built tool's own output folder are beside the owner)",
            @"SearchOption\.AllDirectories|\bRecurseSubdirectories\b|\b(?:Get|Enumerate)(?:Directories|FileSystemEntries|FileSystemInfos)\(|\bFileSystemEnumerable\b",
            "Tst/MatPlotLibNet/Packaging/TreeWalk.cs", "Tst/MatPlotLibNet.Mcp/McpStdioTests.cs", "Tst/MatPlotLibNet.Mcp/ToolPackagingTests.cs") { Under = "Tst/", Finds = [
            "        Directory.EnumerateFiles(Path.Combine(Root, \"Src\"), \"*.cs\", SearchOption." + "AllDirectories)",
            "        var files = Directory.GetFiles(folder, \"*.csproj\", SearchOption." + "AllDirectories);",
            "        var options = new EnumerationOptions { Recurse" + "Subdirectories = true };",
            "        foreach (var child in Directory.Enumerate" + "Directories(folder))",
            "        var entries = new FileSystem" + "Enumerable<string>(root, (ref FileSystemEntry e) => e.ToFullPath());"], Ignores = [
            "        var files = RepositoryTree.Files(\"Src\", \"*.cs\");",
            "        Directory.EnumerateFiles(Path.Combine(Root, \"docs\", \"cookbook\"), \"*.md\")",
            "        var names = Directory.GetFiles(WikiImages).Select(Path.GetFileName);"] },
        new("the folders that hold build output are named by RepositoryTree only: no test lists them again, and none tests a path for them",
            @"\bnode_modules\b|\bis\s+""(?:obj|bin)""|""(?:obj|bin)""\s+or\b|(?:\{Path\.DirectorySeparatorChar\}|[\\/])(?:obj|bin)(?:\{Path\.DirectorySeparatorChar\}|[\\/])",
            "Tst/MatPlotLibNet/Packaging/RepositoryTree.cs", "Tst/MatPlotLibNet/Packaging/RepositoryTreeTests.cs") { Under = "Tst/", Finds = [
            "    private static readonly string[] SkippedFolders = [\"node_" + "modules\", \"bin\", \"obj\", \"dist\", \".git\"];",
            "            .Where(file => !file.Contains($\"{Path.DirectorySeparatorChar}" + "obj{Path.DirectorySeparatorChar}\", StringComparison.Ordinal))",
            "            .Any(part => part is \"" + "obj\" or \"bin\" or \"other\");",
            "            .Where(f => !Regex.IsMatch(f, @\"[\\\\/](bin|obj|node_" + "modules)[\\\\/]\"));",
            "        Assert.DoesNotContain(files, f => f.Path.Contains(\"/" + "obj/\"));"], Ignores = [
            "        using var assets = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, \"Src\", project, \"obj\", \"project.assets.json\")));",
            "        var tool = new DirectoryInfo(Path.Combine(RepositoryRoot.Folder, \"Src\", \"MatPlotLibNet.Mcp\", \"bin\"));",
            "    [InlineData(\"histogram\", \"rWidth:\", \"bin\")]"] },
        new("a figure is sent through its JSON and read back by RoundTripped, through the one serializer it holds",
            @"\.FromJson\([^;]*\.ToJson\(|\b(?:string|var)\s+(\w+)\s*=\s*[^;]*\.ToJson\([^()\n;]*\)\s*;(?=(?:(?!\b\1\b)[\s\S])*?\.FromJson\(\s*\1\s*\)(?:(?!\b\1\b)[\s\S])*?\n    \})",
            "Tst/MatPlotLibNet/TestFixtures/FigureRoundTripExtensions.cs") { Under = "Tst/", AcrossLines = true, Finds = [
            "        var back = serializer.FromJson(serializer." + "ToJson(figure));",
            "        var again = ChartSerializerDefault.Current.FromJson(figure." + "ToJson());",
            "        string json = figure." + "ToJson();\n        var back = ChartSerializerDefault.Current.FromJson(json);\n        Assert.Equal(figure.ToSvg(), back.ToSvg());\n    }",
            "        var json = new ChartSerializer()." + "ToJson(figure);\n        var restored = new ChartSerializer().FromJson(json);\n    }",
            "        var json = Plt.Create()\n            .AddSubPlot(1, 1, 1, ax => ax.Plot(x, y))\n            ." + "ToJson();\n\n        var axes = new ChartSerializer().FromJson(json).SubPlots[0];\n    }"], Ignores = [
            "        string json = serializer.ToJson(figure);",
            "        var read = Reader.Read(new ChartSpec(figure.ToJson()));",
            "        string json = figure." + "ToJson();\n        var back = serializer.FromJson(json);\n        Assert.Contains(\"\\\"NaN\\\"\", json);\n    }",
            "        string json = figure." + "ToJson();\n        Assert.DoesNotContain(\"markerSize\", json);\n        var back = serializer.FromJson(json);\n    }",
            "        string json = figure." + "ToJson().Replace(\"a\", \"b\", StringComparison.Ordinal);\n        Assert.Throws<ArgumentException>(() => new ChartSerializer().FromJson(json));\n    }"] },
        new("the series types under the drawn-property contract are theory rows of DrawnSeriesFixtures.TypeNames, never a copy of the list",
            @"TheoryData<string>\s+\w+\s*\{\s*get;\s*\}\s*=\s*new\((?:DrawnSeriesFixtures\.)?DrawnTypes\.Select\(",
            "Tst/MatPlotLibNet/Serialization/DrawnSeriesFixtures.cs") { Under = "Tst/", Finds = [
            "    public static TheoryData<string> SeriesTypes { get; } = new(DrawnSeriesFixtures." + "DrawnTypes.Select(t => t.Name));"], Ignores = [
            "    public static TheoryData<string, CoordinateSystem> Kinds { get; } = new(DrawnSeriesFixtures.DrawnTypes.Select(t => (t.Name, t.Name switch"] },
        OneDouble("an axes context a test hands a series is a TestAxesContext: no test class implements IAxesContext",
            "IAxesContext", "Tst/MatPlotLibNet/TestFixtures/TestAxesContext.cs") with { Finds = [
            "    private sealed class NoLimits : " + "IAxesContext",
            "    private sealed class StackedContext(IReadOnlyList<ISeries> series) : " + "IAxesContext",
            "    private sealed record ContextWithXLimits(double? XAxisMin, double? XAxisMax) : " + "IAxesContext",
            "file sealed class NullCtx : " + "IAxesContext"], Ignores = [
            "    private sealed class Probe : ChartSeries",
            "    private static double Range(IAxesContext context) => context.XAxisMin ?? 0;",
            "internal sealed record TestAxesContext(double? XAxisMin = null) : IDisposable"] },
        OneDouble("a surface a test draws on derives from SilentRenderContext (or RecordingRenderContext): no test class implements IRenderContext afresh",
            @"I\w*RenderContext", "Tst/MatPlotLibNet/TestFixtures/SilentRenderContext.cs", "SilentRenderContext", "RecordingRenderContext") with { Finds = [
            "    private sealed class QuietContext : " + "IRenderContext",
            "    private sealed class GridFillingContext(bool fills) : " + "IPixelGridRenderContext",
            "    private sealed class MarkupRecordingContext : " + "IMarkupRenderContext"], Ignores = [
            "    private sealed class OwnRenderer(Axes axes, IRenderContext ctx) : AxesRenderer(axes, Plot, ctx, Theme.MatplotlibV2)",
            "internal sealed class RecordingRenderContext : SilentRenderContext, IOutlineClipRenderContext",
            "    private sealed class Surface : SilentRenderContext, IRenderContext"] },
        OneDouble("a series visitor a test hands a series is the TestSeriesVisitor: no test class implements ISeriesVisitor",
            "ISeriesVisitor", "Tst/MatPlotLibNet/TestFixtures/TestSeriesVisitor.cs") with { Finds = [
            "    private sealed class RecordingVisitor : " + "ISeriesVisitor",
            "    private sealed class VisitorSpy : " + "ISeriesVisitor",
            "    private sealed class Spy : MatPlotLibNet.Rendering." + "ISeriesVisitor"], Ignores = [
            "        new PackageSeries().Accept(new TestSeriesVisitor(), _ => made++, () => { });",
            "    public void Accept(ISeriesVisitor visitor, RenderArea area) { }"] },
        OneDouble("a series written outside the library, as a test hands one to the axes, is the ForeignSeries: no test class implements ISeries",
            "ISeries", "Tst/MatPlotLibNet/TestFixtures/ForeignSeries.cs") with { Finds = [
            "    private sealed class BareSeries : " + "ISeries",
            "    private sealed class ForeignSeries(DataRangeContribution reported) : " + "ISeries"], Ignores = [
            "    private sealed class Probe : ChartSeries",
            "    public void Add(ISeries series) { }",
            "    private sealed class Rows : IReadOnlyList<ISeries>"] },
        OneDouble("a layout a test hands an interaction modifier is a TestChartLayout: no core test class implements IChartLayout afresh",
            "IChartLayout", "Tst/MatPlotLibNet/TestFixtures/TestChartLayout.cs", "TestChartLayout") with { Under = "Tst/MatPlotLibNet/", Finds = [
            "    private sealed class PlainLayout : " + "IChartLayout",
            "    private sealed class StatedLayout(Rect area, DataRange range) : " + "IChartLayout"], Ignores = [
            "    private sealed class FakeLayout : TestChartLayout, IChartLayout",
            "    private sealed class BareModifier(string chartId, IChartLayout layout, Action<FigureInteractionEvent> sink)"] },
        OneDouble("a handler a test hands a host, a session or a control is a RecordingChartEventHandler: no test class implements IChartEventHandler",
            "IChartEventHandler", "Tst/MatPlotLibNet/TestFixtures/RecordingChartEventHandler.cs") with { Finds = [
            "    private sealed class RecordingHandler : " + "IChartEventHandler",
            "    private sealed class Collect(List<FigureInteractionEvent> into) : " + "IChartEventHandler",
            "    private sealed class ThrowingHandler : MatPlotLibNet.Interaction." + "IChartEventHandler"], Ignores = [
            "        var host = new InteractionHost { Handler = new RecordingChartEventHandler(events) };",
            "    public IChartEventHandler? Handler { get; set; }"] },
        OneDouble("a publisher a test hands a session or a registry is a SilentChartPublisher or a RecordingChartPublisher: no test class implements IChartPublisher",
            "IChartPublisher", "Tst/MatPlotLibNet.AspNetCore/TestFixtures/SilentChartPublisher.cs") with { Finds = [
            "    private sealed class RecordingPublisher : " + "IChartPublisher",
            "    private sealed class GateWatchingPublisher(object gate) : " + "IChartPublisher"], Ignores = [
            "    internal sealed class RecordingChartPublisher : SilentChartPublisher",
            "        private readonly IChartPublisher _inner;"] },
        OneDouble("a caller publisher a test hands a session is a RecordingCallerPublisher, or the library's NullCallerPublisher for one that listens to nothing: no test class implements ICallerPublisher",
            "ICallerPublisher", "Tst/MatPlotLibNet.AspNetCore/TestFixtures/RecordingCallerPublisher.cs") with { Finds = [
            "    private sealed class Callers : " + "ICallerPublisher",
            "    private sealed class NullCallerPublisher : " + "ICallerPublisher"], Ignores = [
            "        var session = new ChartSession(\"c\", figure, publisher, NullCallerPublisher.Instance, options);"] },
        OneDouble("a publisher that keeps and forgets per chart is a KeepingChartPublisher: no test class implements IKeepingChartPublisher",
            "IKeepingChartPublisher", "Tst/MatPlotLibNet.AspNetCore/TestFixtures/KeepingChartPublisher.cs") with { Finds = [
            "    private sealed class CountingPublisher : " + "IKeepingChartPublisher",
            "    private sealed class Counting : SilentChartPublisher, " + "IKeepingChartPublisher"], Ignores = [
            "        var publisher = new KeepingChartPublisher();",
            "    internal sealed class KeepingChartPublisher : SilentChartPublisher"] },
        OneDouble("an animation source a test hands a control or a follower is a FakeAnimationSource: no test class implements IAnimationSource",
            "IAnimationSource", "Tst/MatPlotLibNet/TestFixtures/FakeAnimationSource.cs") with { Finds = [
            "    private sealed class FakeSource : " + "IAnimationSource",
            "    private sealed class FakeAnimationSource : " + "IAnimationSource"], Ignores = [
            "        var source = new FakeAnimationSource();",
            "    public IAnimationSource? AnimationSource { get; set; }"] },
        OneDouble("a hub client a server test hands a publisher that writes only what it must is a BareHubClient: no test class implements IChartHubClient",
            "IChartHubClient", "Tst/MatPlotLibNet.AspNetCore/TestFixtures/BareHubClient.cs") with { Under = "Tst/MatPlotLibNet.AspNetCore/", Finds = [
            "    private sealed class BareClient : " + "IChartHubClient"], Ignores = [
            "        IChartHubClient client = new BareHubClient();",
            "        var group = Substitute.For<IChartHubClient>();"] },
        OneDouble("an animation a test hands a controller or a play is a TestAnimation: no test class implements IAnimation",
            "IAnimation", "Tst/MatPlotLibNet/TestFixtures/TestAnimation.cs") with { Finds = [
            "    private sealed class EndlessAnimation : " + "IAnimation<int>",
            "    private sealed class StopsBeforeItsPush(Action stop) : " + "IAnimation<int>"], Ignores = [
            "        var animation = new TestAnimation(3) { Interval = TimeSpan.FromMilliseconds(10) };",
            "    private sealed class FakeSource : " + "IAnimationSource"] },
        OneDouble("a transform a test registers or saves through is a RecordingFigureTransform: no test class implements IFigureTransform afresh",
            "IFigureTransform", "Tst/MatPlotLibNet/TestFixtures/RecordingFigureTransform.cs", "RecordingFigureTransform") with { Finds = [
            "    private sealed class FailingTransform : " + "IFigureTransform",
            "    private sealed class WritesMarker(string marker) : " + "IFigureTransform"], Ignores = [
            "    private sealed class OptionsTransform() : RecordingFigureTransform(\"no-options\"), IFigureTransform",
            "        FigureExtensions.RegisterTransform(\".mark\", new RecordingFigureTransform(\"marker\"));"] },
        OneDouble("a renderer of the caller's a test hands an exporter is a RecordingChartRenderer: no test class implements IChartRenderer",
            "IChartRenderer", "Tst/MatPlotLibNet/TestFixtures/RecordingChartRenderer.cs") with { Finds = [
            "    private sealed class CallersRenderer : " + "IChartRenderer",
            "    private sealed class ThrowingRenderer : MatPlotLibNet.Rendering." + "IChartRenderer"], Ignores = [
            "    private sealed class Probe : IFrameChartRenderer",
            "        var renderer = new RecordingChartRenderer((_, ctx) => ctx.DrawRectangle(new Rect(1, 2, 3, 4), Fill));"] },
        OneDouble("a subscription client a test hands a live chart is a RecordingSubscriptionClient: no test class implements IChartSubscriptionClient afresh",
            "IChartSubscriptionClient", "Tst/MatPlotLibNet.Blazor/TestFixtures/RecordingSubscriptionClient.cs", "RecordingSubscriptionClient") with { Finds = [
            "    private sealed class FeedClient : " + "IChartSubscriptionClient",
            "    private sealed class LiveClient : " + "IChartSubscriptionClient, IChartGestureSender, IChartMessageSource"], Ignores = [
            "    private sealed class LiveClient : RecordingSubscriptionClient, IChartSubscriptionClient, IChartGestureSender",
            "        var client = new RecordingSubscriptionClient();"] },
        OneDouble("the text measure a test hands a renderer or a layout is a TestFontMetrics: no test class implements IFontMetrics",
            "IFontMetrics", "Tst/MatPlotLibNet/TestFixtures/TestFontMetrics.cs") with { Finds = [
            "    private sealed class WideMetrics : " + "IFontMetrics",
            "    private sealed class CountingMetrics(IFontMetrics inner) : " + "IFontMetrics",
            "    private sealed class TallDigitMetrics : " + "IFontMetrics, IGlyphInkMetrics"], Ignores = [
            "    internal sealed class TestGlyphInk(Func<string, Font, double, GlyphInk> ink) : TestFontMetrics(Box), IGlyph" + "InkMetrics",
            "        var metrics = new TestFontMetrics((text, font) => new Size(text.Length * 10.0, 14.0));"] },
        OneDouble("a tick formatter an application writes, as a test hands one to an axis, is a TestTickFormatter: no test class implements ITickFormatter",
            "ITickFormatter", "Tst/MatPlotLibNet/TestFixtures/TestTickFormatter.cs") with { Finds = [
            "    private sealed class UserFormatter : " + "ITickFormatter",
            "    private sealed class WideAboveThree : global::MatPlotLibNet.Rendering.TickFormatters." + "ITickFormatter"], Ignores = [
            "        ax.SetXTickFormatter(new TestTickFormatter(v => $\"<{v}>\"));",
            "    private sealed class Probe : ITickSetFormatter"] },
        OneDouble("a tick locator an application writes, as a test hands one to an axis, is a TestTickLocator: no test class implements ITickLocator",
            "ITickLocator", "Tst/MatPlotLibNet/TestFixtures/TestTickLocator.cs") with { Finds = [
            "    private sealed class EveryThirdLocator : " + "ITickLocator",
            "    private sealed class TheseValuesLocator(params double[] values) : " + "ITickLocator"], Ignores = [
            "        var locator = new TestTickLocator(1.0, 2, 3, 4);",
            "    private sealed class Probe : IMinorTickLocator"] },
        OneDouble("a projection a map test hands a map is a TestGeoProjection: no test class implements IGeoProjection",
            "IGeoProjection", "Tst/MatPlotLibNet.Geo/TestFixtures/TestGeoProjection.cs") with { Finds = [
            "    private sealed class Square : " + "IGeoProjection",
            "    private sealed record SquareWorld(bool Clockwise) : " + "IGeoProjection, IProjectionHorizon"], Ignores = [
            "    private sealed record SquareWorld(bool Clockwise) : TestGeoProjection(Square), IProjectionHorizon",
            "        var map = new TestGeoProjection(new GeoBounds(-180, 180, -90, 90));"] },
        OneDouble("a map a test hands the axes is a TestMapSeries: no test class implements IMapSeries",
            "IMapSeries", "Tst/MatPlotLibNet/TestFixtures/TestMapSeries.cs") with { Finds = [
            "    private sealed class BoxMap : ChartSeries, " + "IMapSeries"], Ignores = [
            "    private static TestMapSeries BoxMap => new(new(0, 10, 0, 10), _ => []);"] },
        OneDouble("a stackable series that is not a bar, as a test hands one to the axes, is a TestStackable: no test class implements IStackable",
            "IStackable", "Tst/MatPlotLibNet/TestFixtures/TestStackable.cs") with { Finds = [
            "    internal sealed class CountingStackable(double[] values) : ChartSeries, " + "IStackable"], Ignores = [
            "        var counting = new TestStackable([1.0, 2.0]);"] },
        OneDouble("a colour map an application writes, as a test hands one to a series, is a TestColorMap: no test class implements IColorMap afresh",
            "IColorMap", "Tst/MatPlotLibNet/TestFixtures/TestColorMap.cs", "TestColorMap") with { Finds = [
            "    private sealed class StripedColorMap(string name) : " + "IColorMap"], Ignores = [
            "    private sealed class OverUnderColorMap(Color? under, Color? over) : TestColorMap(\"OverUnder\", _ => Gray), IColorMap"] },
        OneDouble("a normalizer an application writes, as a test hands one to a colour scale, is a TestNormalizer: no test class implements INormalizer",
            "INormalizer", "Tst/MatPlotLibNet/TestFixtures/TestNormalizer.cs") with { Finds = [
            "    private sealed class Foreign : " + "INormalizer"], Ignores = [
            "    private sealed class Probe : INormalizerRule"] },
        OneDouble("a light an application writes, as a test hands one to a 3-D series, is a TestLightSource: no test class implements ILightSource",
            "ILightSource", "Tst/MatPlotLibNet/TestFixtures/TestLightSource.cs") with { Finds = [
            "    private sealed class HalfLight : " + "ILightSource"], Ignores = [
            "        var light = new TestLightSource(0.5);"] },
        OneDouble("an interpolation engine an application writes, as a test hands one to an image, is a TestInterpolationEngine: no test class implements IInterpolationEngine",
            "IInterpolationEngine", "Tst/MatPlotLibNet/TestFixtures/TestInterpolationEngine.cs") with { Finds = [
            "    private sealed class OwnBilinear : " + "IInterpolationEngine"], Ignores = [
            "        var engine = new TestInterpolationEngine(\"own\");"] },
        OneDouble("the glyph outlines a test hands the SVG writer are a TestGlyphPaths: no test class implements IGlyphPathProvider",
            "IGlyphPathProvider", "Tst/MatPlotLibNet/TestFixtures/TestGlyphPaths.cs") with { Finds = [
            "    private sealed class OutlineStub(string pathData) : " + "IGlyphPathProvider"], Ignores = [
            "    private sealed class Shaped : " + "IShapedGlyphPathProvider"] },
        OneDouble("the glyph outlines that shape, as a test hands them the SVG writer, are a TestShapedGlyphPaths: no test class implements IShapedGlyphPathProvider",
            "IShapedGlyphPathProvider", "Tst/MatPlotLibNet/TestFixtures/TestGlyphPaths.cs") with { Finds = [
            "    private sealed class ShapedStub : " + "IShapedGlyphPathProvider"], Ignores = [
            "        var shaped = new TestShapedGlyphPaths(text => \"M\" + text, (_, _) => null);"] },
        OneDouble("the glyph ink a test hands a math layout is a TestGlyphInk: no test class implements IGlyphInkMetrics",
            "IGlyphInkMetrics", "Tst/MatPlotLibNet/TestFixtures/TestGlyphInk.cs") with { Finds = [
            "    private sealed class NoInk : " + "IGlyphInkMetrics",
            "    private sealed class InkMetrics : " + "IFontMetrics, " + "IGlyphInkMetrics",
            "    private sealed class InkMetrics(double rise = 0.7)\n        : TestFontMetrics((text, font) => Box(text.Length, font)), " + "IGlyphInkMetrics"], Ignores = [
            "        var style = StyleOf(MathFontSet.DejaVuSans, TestGlyphInk.Boxes);"] },
        OneDouble("the connector a test hands a subscription client is a RecordingHubConnector: no test class implements IHubConnector",
            "IHubConnector", "Tst/MatPlotLibNet.Blazor/Infrastructure/RecordingHubConnector.cs") with { Finds = [
            "    private sealed class TestServerConnector(TestServer server) : " + "IHubConnector"], Ignores = [
            "        var connector = new RecordingHubConnector(connect: _ => hub);"] },
        new("a wait for another thread is Eventually.HoldsAsync: no test spells its own deadline loop",
            @"\bDateTime\.UtcNow\s*\+",
            "Tst/MatPlotLibNet/TestFixtures/Eventually.cs") { Under = "Tst/", Finds = [
            "            var deadline = DateTime." + "UtcNow + TimeSpan.FromSeconds(2);"], Ignores = [
            "        await Eventually.HoldsAsync(() => Calls.Count >= n);"] },
        new("the hub a chart publisher sends through is a ChartHubDouble: no server test builds IHubClients afresh",
            @"Substitute\.For<IHubClients<",
            "Tst/MatPlotLibNet.AspNetCore/TestFixtures/ChartHubDouble.cs") { Under = "Tst/MatPlotLibNet.AspNetCore/", Finds = [
            "        var clients = Substitute." + "For<IHubClients<IChartHubClient>>();"], Ignores = [
            "        var hub = new ChartHubDouble(\"c\");"] },
        new("a session a test drains is a SessionRig: no test spells the marker event the rig waits for",
            @"new PinnedAnnotation\(""marker""",
            "Tst/MatPlotLibNet.AspNetCore/TestFixtures/SessionRig.cs") { Under = "Tst/", Finds = [
            "        Send(new DataCursorEvent(\"c1\", 0, " + "new PinnedAnnotation(\"marker\", 0, 0, 0, 0, 0)));"], Ignores = [
            "        var rig = new SessionRig(handler);"] },
        new("the thread a control posts its work to is a PostedWork in a test: no test class keeps its own queue of posted work",
            @"Queue<Action>\s+Waiting",
            "Tst/MatPlotLibNet/TestFixtures/PostedWork.cs") { Under = "Tst/", Finds = [
            "        public readonly Queue<" + "Action> Waiting = new();"], Ignores = [
            "        var thread = new PostedWork();"] },
        new("the numbers of a random series a test draws come from SeededNormals: no test class writes its own generator",
            @"636413622384679\d{4}UL",
            "Tst/MatPlotLibNet/TestFixtures/SeededNormals.cs") { Under = "Tst/", Finds = [
            "            _state = unchecked(_state * 636413622384679" + "3005UL + 1442695040888963407UL);"], Ignores = [
            "        var rng = new SeededNormals(seed);"] },
        OwnedRule.Nowhere("a number or a sequence of numbers said to be finite is said by FiniteAssertions (AssertFinite, AssertAllFinite): no test spells the check by hand, and a missing value expected on purpose says double.IsNaN with its reason; a pattern of missing values (a half-missing pair, a gap beside a gap) is named in a bool or said by GapAssertions, never asserted bare",
            @"Assert\.False\(" + InsideTheCall + @"(?:(?<!!\s*)" + NotFinite + @"|!\s*" + Finite + ")"
            + @"|Assert\.True\(" + InsideTheCall + @"(?:!\s*(?:\(\s*)?" + NotFinite + @"|!\s*" + InsideTheCall + @"\.Any" + OfItems + NotFinite + @"|(?<!!\s*)" + Finite + ")"
            + @"|Assert\.DoesNotContain\(" + InsideTheCall + SaidNotFinite
            + @"|Assert\.Empty\(" + InsideTheCall + @"\.Where" + OfItems + SaidNotFinite
            + @"|Assert\.Equal\(\s*0\s*," + InsideTheCall + @"\.Count" + OfItems + SaidNotFinite
            + @"|(?m:^\s*\w+\s*=>\s*" + Finite + @"\()"
            + @"|\.Where\(" + InsideTheCall + @"!\s*" + Finite + @"\(",
            "Tst/") with { AcrossLines = true, Finds = [
            "            Assert." + "False(double.IsNaN(v));",
            "        Assert." + "False(double.IsInfinity(inv[0, 0]));",
            "        Assert.All(r, v => Assert." + "False(double.IsNaN(v)));",
            "        Assert." + "True(double.IsFinite(p.X));",
            "        Assert." + "True(double.IsFinite(Values(ind.OutputSeries[0])[3]), \"after the warm-up\");",
            "        Assert." + "True(double.IsFinite(upper[0]) && upper[0] > lower[0]);",
            "        Assert." + "True(!double.IsNaN(point.D));",
            "        Assert.All(grid, l => Assert." + "True(\n            double.IsFinite(l.P1.X) && double.IsFinite(l.P1.Y)));",
            "        Assert." + "DoesNotContain(areas[0].YData, double.IsNaN);",
            "        Assert." + "DoesNotContain(Ends(expected), v => !double.IsFinite(v));",
            "        Assert." + "True(values.All(double.IsFinite));",
            "        Assert." + "False(double.IsNaN(plot.Width) || double.IsNaN(plot.X));",
            "        Assert." + "False(double.IsInfinity(v) || double.IsNaN(v));",
            "        Assert." + "True(!double.IsNaN(a) && !double.IsNaN(b));",
            "        Assert." + "True(x > 0 && double.IsFinite(x));",
            "        Assert." + "True(!(double.IsNaN(a) || double.IsNaN(b)));",
            "        Assert." + "False(values.Any(double.IsNaN));",
            "        Assert." + "True(!values.Any(double.IsNaN));",
            "        Assert." + "True(values.All(v => !double.IsNaN(v)));",
            "        Assert." + "False(values.Any(v => !double.IsFinite(v)));",
            "        Assert." + "Empty(values.Where(double.IsNaN));",
            "        Assert." + "Equal(0, values.Count(double.IsNaN));",
            "        Assert." + "False(\n            double.IsNaN(a)\n            || double.IsNaN(b));",
            "        Func<DrawLineCall, bool> IsFinite,\n        " + "line => double.IsFinite(line.P1.X) && double.IsFinite(line.P2.X),",
            "        }." + "Where(member => member.Value is { } value && !double.IsFinite(value)).Select(member => member.Name);"], Ignores = [
            "        Assert.True(double.IsNaN(y[3]));   // a gap",
            "        Assert.False(double.IsFinite(pt.X));   // the origin has no pixel",
            "        Assert.False(xMissingButNotY, \"inconsistent\");",
            "        bool neighbourIsSeparator = double.IsNaN(a[i - 1]) || double.IsNaN(a[i + 1]);",
            "        Assert.True(double.IsNaN(a) && double.IsNaN(b), \"a missing pair\");",
            "        Assert.True(double.IsNaN(py) || (py >= 0 && py <= 800), \"inside or missing\");",
            "        Assert.False(double.IsFinite(a) && double.IsFinite(b));",
            "        Assert.True(double.IsPositiveInfinity(v));",
            "        Assert.True(r.Y.Any(double.IsNaN), \"NaN should propagate\");",
            "        Assert.Equal(3, vwap.Count(double.IsNaN));",
            "        Assert.True(i + 2 == n || (double.IsNaN(x[i]) && double.IsNaN(y[i])), \"followed by a gap\");",
            "        Assert.Contains(momentum[12..], double.IsFinite);",
            "        Assert.Equal(9, line.YData.Count(double.IsFinite));",
            "        sma.GetLatest().AssertFinite();",
            "        if (!xs.All(double.IsFinite)) continue;",
            "        int drawn = svg.PolylinePoints().Count(p => double.IsFinite(p.X) && double.IsFinite(p.Y));",
            "        Assert.DoesNotContain(double.NegativeInfinity, line.YData);   // a gap may stand, an infinity may not",
            "        double[] there = values.Where(double.IsFinite).ToArray();",
            "        var valid = rsRatio.Where(v => !double.IsNaN(v)).ToArray();"] },
        OwnedRule.Nowhere("a drawing, or any text a drawing wrote, said to carry no missing value and no infinity is said by FiniteAssertions (AssertNoNonFiniteNumber): no test looks for the tokens NaN and Infinity by hand (DoesNotContain, DoesNotMatch, a Contains read as false, a regular expression over the markup); the scripts of a figure, which test isNaN, are left out in that one place. A DrawnToken row that lists a token it must not carry is data of DrawnToken, not a scan",
            @"Assert\.DoesNotContain\(\s*""-?(?:NaN|Infinity)"""
            + @"|Assert\.DoesNotMatch\(\s*""[^""]*(?:NaN|Infinity)"
            + @"|Assert\.DoesNotContain\(" + InsideTheCall + @"\.Contains\(\s*""-?(?:NaN|Infinity)"""
            + @"|Assert\.False\(" + InsideTheCall + @"\.Contains\(\s*""-?(?:NaN|Infinity)"""
            + @"|Assert\.True\(" + InsideTheCall + @"!\s*\w[\w.\[\]]*\.Contains\(\s*""-?(?:NaN|Infinity)"""
            + @"|Regex\.(?:Matches|IsMatch|Match|Count)\([^;]*?(?:NaN|Infinity)"
            + @"|\bAbsent\s*=\s*\[[^\]]*""-?(?:NaN|Infinity)""",
            "Tst/") with { AcrossLines = true, Finds = [
            "        Assert." + "DoesNotContain(\"NaN\", svg);",
            "        Assert." + "DoesNotContain(\"Infinity\", figure.ToSvg(), StringComparison.Ordinal);",
            "        Assert." + "DoesNotContain(\"-Infinity\", text);",
            "        Assert." + "DoesNotMatch(\"NaN|Infinity\", body);",
            "        Assert.All(lines, l => Assert." + "DoesNotContain(\"NaN\", l));",
            "        Assert." + "DoesNotContain(labels, l => l.Contains(\"NaN\", StringComparison.Ordinal));",
            "        Assert." + "False(text.Contains(\"NaN\") || text.Contains(\"Infinity\"), $\"says {text}\");",
            "        Assert." + "True(!svg.Contains(\"Infinity\"));",
            "        int tokens = Regex." + "Matches(svg, \"NaN\").Count;",
            "        return [.. Regex." + "Matches(body, \"[^\\\"<>]*(?:NaN|Infinity)[^\\\"<>]*\").Select(m => m.Value)];",
            "        Assert." + "DoesNotContain(\n            \"NaN\",\n            svg);"], Ignores = [
            "        new DrawnToken(\"AMissingValue_BreaksTheLine\", () => Svg(), \"<polyline\") { Absent = [\"<circle\"], Finite = true },",
            "        Assert.Contains(\"NaN,NaN\", table.ToCsv());",
            "        Assert.Equal(\"NaN\", formatter.Format(double.NaN));",
            "        Assert.True(svg.Contains(\"NaN\"));",
            "        Assert.DoesNotContain(\"firstCellDate\", json);",
            "        Assert.DoesNotContain(\"<script\", svg);",
            "        var parts = Reg" + "ex.Matches(svg, \"<polyline points=\\\"([^\\\"]*)\\\"\");"] },
        OwnedRule.Nowhere("every theory over the shipped series kinds reads the one roster, DrawnSeriesFixtures: no test writes a list of series instances of its own",
            @"TheoryData<\s*(?:I|Chart)Series\b|\{\s*new \w+Series\(.*\),\s*nameof\(\w+Series\)\s*\}",
            "Tst/") with { Finds = [
            "    public static TheoryData<" + "ISeries, string> AllSeries" + "Instances => new()",
            "        { new LineSeries([1.0], [2.0]), name" + "of(LineSeries) },"], Ignores = [
            "    public static TheoryData<string> Kinds => [\"contour\", \"contourf\"];",
            "        yield return (\"Kde(filled)\", new KdeSeries([1.0, 2, 3]) { Fill = true });"] },
        OwnedRule.Nowhere("the roster's instance of a kind at rest is DrawnSeriesFixtures.Instance: no test takes the series out of an at-rest Build itself",
            @"TheSeries\(DrawnSeriesFixtures\.Build\([^;]*everyPropertySet: false",
            "Tst/") with { Finds = [
            "        var series = DrawnSeriesFixtures.TheSeries(DrawnSeriesFixtures." + "Build(kind, everyPropertySet: false));"], Ignores = [
            "            var series = DrawnSeriesFixtures.TheSeries(DrawnSeriesFixtures.Build(type.Name, everyPropertySet: true));",
            "    public static ISeries Instance(string typeName) => TheSeries(Build(typeName, everyPropertySet: false));"] },
        OwnedRule.Nowhere("the sample series of a registered kind is a row of SampleSeries.AddSampleSeries, the golden corpus' data: the second switch and the kind list of the round trip stay deleted",
            @"\bAddSeries(?:ByKind)\b|\bSeries(?:Kinds)\b(?!\w)|\bAllSeries(?:Instances)\b",
            "Tst/") with { Finds = [
            "    private static void AddSeries" + "ByKind(AxesBuilder ax, string kind)",
            "    private static readonly string[] Series" + "Kinds =",
            "        foreach (var row in AllSeriesTests.AllSeries" + "Instances)"], Ignores = [
            "        .AddSubPlot(1, 1, 1, ax => ax.AddSampleSeries(discriminator))",
            "    public static TheoryData<string, AxisSide, int> SeriesKindsOnEveryYAxisSeat { get; }"] },
        OwnedRule.Nowhere("a kind's type tag is held by the golden corpus and its registry row by the roster theory: no test of a kind states either",
            @"Assert\.Equal\((?:""[a-z0-9_-]+""|kind), [^;]*(?:ToSeriesDto\(\)|\bdto)\.Type\)|Assert\.Contains\(""[a-z0-9_-]+"", SeriesRegistry\.Discriminators\)",
            "Tst/MatPlotLibNet/Models/") with { Finds = [
            "        Assert.Equal(\"eventplot\", series." + "ToSeriesDto().Type);",
            "        Assert.Equal(kind, dto." + "Type);",
            "    public void TheChordKind_IsInTheRegistry() => Assert.Contains(\"chord\", SeriesRegistry." + "Discriminators);"], Ignores = [
            "        Assert.Contains(dto.Type, SeriesRegistry.Discriminators);",
            "        Assert.Equal(kind, dto.Kind);"] },
        OwnedRule.Nowhere("a kind's base class and interfaces are its declaration, which the compiler holds: no test of a series states them",
            @"Assert\.IsAssignableFrom<\w+>\(new \w+Series\(|void \w+Series_Inherits\w+\(",
            "Tst/MatPlotLibNet/Models/") with { Finds = [
            "        Assert.IsAssignableFrom<IColormappable>(new Clustermap" + "Series(SampleData));",
            "    public void BoxSeries_Inherits" + "DatasetSeries()"], Ignores = [
            "        var chart = Assert.IsAssignableFrom<ChartSeries>(series);"] },
        new("the members of a data range are listed once, where the roster theory holds each one finite under axes with and without limits",
            @"nameof\(\w+\.StickyZMax\)",
            "Tst/MatPlotLibNet/Models/Series/AllSeriesTests.cs") { Under = "Tst/", Finds = [
            "        AssertFinite(range.StickyZMax, name" + "of(range.StickyZMax));"], Ignores = [
            "        Assert.Null(range.StickyZMax);"] },
        OwnedRule.Nowhere("what a series, a part or a settings record starts from is pinned by RestingValueCorpusTests, a setting it is given by the drawn-property round trips, and the data a constructor stores by the GivenSeries and GivenAxes pins: no test of the models states one of them",
            @"\bvoid (?:Default\w*_(?:Is|Has)\w*|\w+_Defaults\w*|\w+_DefaultIs\w*|\w*_CanBeSet\w*|Constructor_Stores\w*|Ctor_Stores\w*)\(",
            "Tst/MatPlotLibNet/Models/") with { Finds = [
            "    public void DefaultWhis" + "_Is1Point5()",
            "    public void Default_Width" + "_Is20()",
            "    public void DefaultFigure_Has" + "ReasonableDefaults()",
            "    public void Legend_LegendValues_" + "DefaultsFalse()",
            "    public void ColorMap_" + "DefaultsToNull()",
            "    public void MinRenderInterval_" + "DefaultIs33ms()",
            "    public void Extend_Min_" + "CanBeSet()",
            "    public void Constructor_" + "StoresData()",
            "    [Fact] public void FontSize_" + "DefaultsTo12() => Assert.Equal(12, new QuiverKeySeries(0.5, 0.9, 1.0, \"1 m/s\").FontSize);"], Ignores = [
            "    public void ANewFigure_WearsTheMatplotlibV2Theme()",
            "    public void ComputeDataRange_WithoutPositions_KeepsTheRoomAroundTheDefaultPositions()",
            "    public void AppendBar_WithAVolume_StoresThePricesOnly()"] },
        OwnedRule.Nowhere("a setting given to a series and read straight back is held by the drawn-property round trips: no fact is only a series made, one setting given and asserts that read that setting",
            @"\{\s*(?:var \w+ = new\s*\w*\[\][^;]*;\s*)*(?:var|\w+) (\w+) = new \w+Series\([^;]*?\)(?:\s*\{\s*(\w+) = [^;]*?\})?;\s*(?:\1\.(\w+) = [^;]*;\s*)?(?:Assert\.[^;]*;\s*)*?Assert\.\w+(?:<[\w.]+>)?\([^;]*\b\1\.(?:\2|\3)\b[^;]*\);\s*(?:Assert\.[^;]*;\s*)*\}",
            "Tst/") with { AcrossLines = true, Finds = [
            "    {\n        var labs = new[] { \"A\", \"B\" };\n        var s = new PairGrid" + "Series(SampleVars) { Labels = labs };\n        Assert.Same(labs, s.Labels);\n    }",
            "    {\n        var series = new Scatter" + "Series([1.0, 2.0], [3.0, 4.0]) { C = [0.0, 1.0] };\n        Assert.Equal(2, series.C!.Length);\n    }",
            "    {\n        INormalizable n = new Image" + "Series(new double[2, 2]);\n        n.Normalizer = new LogNormalizer();\n        Assert.IsType<LogNormalizer>(n.Normalizer);\n    }"], Ignores = [
            "    {\n        var series = new Line" + "Series(X, Y) { Color = Colors.Red };\n        Assert.Contains(\"#FF0000\", Svg(series));\n    }",
            "    {\n        var scatter = new Scatter" + "Series([1.0], [2.0]);\n        IHasMarkerStyle styled = scatter;\n        styled.MarkerStyle = MarkerStyle.Square;\n        Assert.Equal(MarkerStyle.Square, scatter.Marker);\n    }",
            "    {\n        var series = new Histogram2D" + "Series(X, Y) { BinsX = 3 };\n        Assert.Equal(3, series.ComputeDataRange(null!).XMax);\n    }"] },
        OwnedRule.Nowhere("the fields of a positional record and a value a constructor or an initializer stores as given are what the compiler holds: no test of the core restates them",
            @"\bvoid (?:\w+_SetVia\w*|\w+_CanBeAssigned\w*|\w+_StoresAllFields\w*|\w+_Exposes\w*Properties\w*|\w+_IsStored|\w+_Stores(?:Values|Fractions|PixelCoords|PixelRange|WindowSize)|Constructor_Stores\w*|Ctor_Stores\w*)\(",
            "Tst/MatPlotLibNet/") with { Finds = [
            "    public void Labels_SetVia" + "Init_Stored()",
            "    public void ColorMap_SetVia" + "Initializer_Stored()",
            "    public void ScatterSeries_C_" + "CanBeAssigned()",
            "    public void StateSegment_Struct_" + "ExposesAllProperties()",
            "    public void MatplotlibFontStack_" + "StoresAllFields()",
            "    public void InsetBounds_" + "StoresValues()",
            "    public void Elevation_" + "IsStored()",
            "    public void Constructor_" + "StoresNameAndParameters()"], Ignores = [
            "    public void AppendBar_WithAVolume_StoresThePricesOnly()",
            "    public void ChangingASlotToAnotherValue_StoresItAndStampsALaterMark()",
            "    public void AddInset_InsetBounds_StoresBoundsAndAddsInset()"] },
        OwnedRule.Nowhere("a series' trip through JSON is held by the drawn-property contract and the GivenSeries pins, and a series read from JSON with no data by the one no-data theory of MissingValueGeometryTests: no test restates either by hand",
            @"\bvoid (?:RoundTrip_Preserves\w*Series\w*|\w*FullyPopulated\w*|\w*_Flips\w*Arms?|\w+_UsesNullFallback|ToSeriesDto_Includes\w*)\(",
            "Tst/") with { Finds = [
            "    public void RoundTrip_Preserves" + "RadarSeries()",
            "    public void RoundTrip_Hexbin" + "FullyPopulated_FlipsAllOptionalArms()",
            "    public void RoundTrip_HeatmapWithColorMap_" + "FlipsColorMapArm()",
            "    public void FromJson_HexbinWithNoXYData_" + "UsesNullFallback()",
            "    public void ToSeriesDto_" + "IncludesBandwidthWhenSet()"], Ignores = [
            "    public void RoundTrip_PreservesTitle()",
            "    public void ASeriesWithNoData_RendersAnEmptyPlot(string type)",
            "    public void ANamedColourMap_IsWrittenByItsName()"] },
        OwnedRule.Nowhere("what a verb owes the callback it takes (it runs, after the verb, on what the verb added; no callback draws what an empty one does; the verb adds what it names) is held for every overload by VerbContractTests over VerbCatalogue: no test of the core suite states it for one verb",
            @"\bvoid (?!Apply_)[A-Z][A-Za-z0-9]*_(?:Adds[A-Z]\w*Series|Shortcut_Adds\w*|WithConfigure\w*|NullConfigure\w*|OverloadNullConfigure\w*|Configure_IsInvoked|InBarSlotContext_SetsIndicatorOffset)\(",
            "Tst/MatPlotLibNet/") with { Finds = [
            "    public void Pie_" + "AddsPieSeries()",
            "    public void GarmanKlass_" + "Shortcut_AddsSeries()",
            "    public void AxHLine_" + "WithConfigure_AppliesCustomisation()",
            "    public void Plot_" + "NullConfigure_AddsLineSeriesWithoutCustomization()",
            "    public void WithTitle_" + "OverloadNullConfigure_SetsTitleOnly()",
            "    public void Vpin_" + "Configure_IsInvoked()",
            "    public void Ema_" + "InBarSlotContext_SetsIndicatorOffset()"], Ignores = [
            "    public void Apply_AddsLineSeries()",
            "    public void AddLines_NoHue_AddsSingleLineSeries()",
            "    public void AddIndicator_InBarSlotContext_NudgesOffsetOntoTheCategorySlotCentre()"] },
        new("what every indicator keeps (what it draws and under which labels, where it draws it, the range of its pane, its first value, what it says over bars that never move, its name by default, the arguments it refuses in both forms) is held once by IndicatorContractTests over IndicatorCases, and the shape of every streaming form by StreamingFormsShapeTests over the map: no indicator's test file states one for itself (the multivariate base keeps its own refusal of rows of two lengths)",
            @"\bvoid (?:Inherits\w*Indicator|Compute_(?:EmptyInput\w*|LengthBelow\w*_ReturnsEmpty|LengthEquals\w*|SingleBar_ReturnsEmpty|TooFew\w*_ReturnsEmpty|BarCount\w*_ReturnsEmpty|ReturnsCorrectLength|ValuesInRange|FlatPrices_ReturnsZero)|Apply_(?:Adds\w*Series\w*|SetsExpectedLabels?|SetsLabel|SetsY\w*|SetsAxisLimits|DrawsEachValueAtItsOwnBar|TooFewBars\w*|GivenTooFewBars\w*)|Default(?:Period|Label|Alpha|Hazard|Window|SmoothingPeriod|Order|HpAndLp)\w*|Constructor_\w+_Throws|Construction_(?:Defaults|Custom)\w*|WarmupPeriod_\w*|BeforeWarmup_ReturnsNaN|OutputSeries_Has\w*|Label_DefaultsTo\w*|TheShortcut_RunsItsCallback\w*)\(",
            "Tst/MatPlotLibNet/Indicators/MultivariateIndicatorTests.cs") { Under = "Tst/MatPlotLibNet/Indicators/", Finds = [
            "    public void Compute_" + "EmptyInput_ReturnsEmpty()",
            "    public void Compute_" + "LengthBelowPeriod_ReturnsEmpty()",
            "    public void Compute_" + "FlatPrices_ReturnsZero()",
            "    public void Apply_" + "AddsLineSeries()",
            "    public void Apply_" + "SetsExpectedLabel()",
            "    public void Apply_" + "SetsYAxisZeroToOne()",
            "    public void Inherits" + "CandleIndicator()",
            "    public void DefaultPeriod_" + "Is20()",
            "    public void Constructor_" + "PeriodBelowTwo_Throws(int period)",
            "    public void Construction_" + "DefaultsAreCorrect()",
            "    public void BeforeWarmup_" + "ReturnsNaN()",
            "    public void TheShortcut_" + "RunsItsCallbackOnTheIndicator_Once()"], Ignores = [
            "    public void Apply_ShiftsTheHistogramByTheOffset_AsItShiftsTheLines()",
            "    public void Apply_WithExplicitLabels_UsesThemForCategories()",
            "    public void Compute_KnownVector_MatchesPythonReference()",
            "    public void Compute_TwoBars_ReturnsSingleValue()"] },
        new("the verb of a composed chart draws exactly what the hand chain of public calls it replaces draws: one theory over the shapes' ByHand (the calendar of events keeps its own, as its chain needs a flag only the core can name)",
            @"\bvoid \w*HandRecipe\w*\(",
            "Tst/MatPlotLibNet/Rendering/ComposedChartContractTests.cs", "Tst/MatPlotLibNet/Builders/CalendarEventsTests.cs") { Under = "Tst/", Finds = [
            "    public void TheVerb_DrawsExactlyWhat" + "TheHandRecipeDraws()"], Ignores = [
            "    public void TheVerb_DrawsTheBricks()"] },
        OwnedRule.Nowhere("the promises of a composed chart (a refused call leaves the axes as it was, the limits are the caller's, it survives the JSON, a line takes one colour slot and one legend entry, the parts are a read-only record struct) and of a verb with a callback (the callback wins and runs once, the verb chains, a missing builder is refused) are theories over ComposedChartShapes and VerbCatalogue: no verb's test file states one for itself",
            @"\bvoid (?:ANullBuilder_IsRefused|WhatTheCallbackSets_Wins\w*|TheVerb_ChainsWithTheRestOfTheBuilder|TheCallback_RunsOnce\w*|\w*_IsOneLegendEntry\w*|\w*_(?:AreOne|IsOne)ColourSlot\w*|TheParts_AreAReadOnlyRecordStruct\w*|ARefusedCall_\w*|TheVerb_SetsNoAxisLimits\w*|TheChart_Survives(?:The)?Json\w*)\(",
            "Tst/MatPlotLibNet/Builders/") with { Finds = [
            "    public void ANullBuilder_" + "IsRefused()",
            "    public void WhatTheCallbackSets_" + "WinsOverWhatTheVerbDid()",
            "    public void TheVerb_" + "ChainsWithTheRestOfTheBuilder()",
            "    public void TheCallback_" + "RunsOncePerGroup_InOrder_AfterTheVerbStyledIt()",
            "    public void TheLine_" + "IsOneLegendEntry()",
            "    public void TheSegments_" + "AreOneColourSlot_TheNextSeriesTakesTheNextColour()",
            "    public void TheParts_" + "AreAReadOnlyRecordStruct_WithNoPublicConstructor()",
            "    public void ARefusedCall_" + "LeavesTheAxesAsItWas()",
            "    public void TheVerb_" + "SetsNoAxisLimits_AndKeepsTheCallersOwn()",
            "    public void TheChart_" + "SurvivesTheJson_AndDrawsTheSamePicture()"], Ignores = [
            "    public void AReferenceWithoutAFiniteQuantile_IsRefused_AndLeavesTheAxesAsItWas(double quantile, string written)",
            "    public void TheParts_AreTheTwoSeriesOnTheAxes_TheRisingBricksAndTheFallingOnes()"] },
        OwnedRule.Nowhere("a class of tests is not the twin of another: no class of its own for the linked top axis, the Y slot, an edge of the axis part, an inverted or a reversed-limit axis, or a second look at the SignalR script (a Baseline or a release-numbered class is refused by the rule of names below)",
            @"\bclass (?:LinkedTopAxisTests|LinkedYAxisTests|CartesianAxisPartOn\w+EdgeTests|InvertedAxisTests|ReversedAxisLimitTests|SvgSignalRDataCursorScriptTests|SignalRBehaviouralTests)\b",
            "Tst/MatPlotLibNet/") with { Finds = [
            "public class Linked" + "TopAxisTests",
            "public class CartesianAxisPartOn" + "AYEdgeTests",
            "public class Inverted" + "AxisTests",
            "public class SignalR" + "BehaviouralTests"], Ignores = [
            "public class LinkedAxisTests",
            "public class LinkedBottomAxisTests",
            "public class CartesianAxisPartTests",
            "public class ReversedAxisTests",
            "public class SvgTransformTests"] },
        OwnedRule.Nowhere("a part with nothing to draw draws nothing is held for every part by CartesianAxesPartTests over the parts: no part's unit tests state it",
            @"\bvoid (?:Render_No\w+_(?:DoesNothing|DrawsNothing)|AnAxesWithoutPatches_DrawsNothing)\(",
            "Tst/MatPlotLibNet/Rendering/CartesianParts/") with { Finds = [
            "    public void Render_NoSpans_" + "DoesNothing()",
            "    public void Render_NoBreaks_" + "DrawsNothing()",
            "    public void AnAxesWithoutPatches_" + "DrawsNothing() => Assert.Empty(Render(new Axes()).Calls);"], Ignores = [
            "    public void APartWithNothingToDraw_DrawsNothing(string name)",
            "    public void Render_StyleNone_DrawsNothing()"] },
        OwnedRule.Nowhere("a part's unit test draws through PartFactory.Drawn: no test makes a recording context and renders the part it builds on the next line",
            @"var ctx = new RecordingRenderContext\(\);\s*\n\s*Build\([^\n]*\)\.Render\(\);",
            "Tst/MatPlotLibNet/Rendering/CartesianParts/") with { AcrossLines = true, Finds = [
            "        var ctx = new Recording" + "RenderContext();\n        Build(new Axes(), ctx).Render();\n"], Ignores = [
            "        var ctx = Drawn(new Axes());\n",
            "        var ctx = new RecordingRenderContext();\n        var part = Build(new Axes(), ctx);\n        part.Render();\n"] },
        OwnedRule.Nowhere("a direct-invocation test of a series renderer draws through Drawn: no test makes the context and the renderer by hand",
            @"var ctx = NewContext\(out var svg",
            "Tst/MatPlotLibNet/Rendering/SeriesRenderers/SeriesRenderersTests.cs") with { Finds = [
            "        var ctx = New" + "Context(out var svg);"], Ignores = [
            "        make(NewContext(out var svg, dataXMin, dataXMax, dataYMin, dataYMax)).Render(series);"] },
        new("the Fahrenheit ticks of a linked axis, 0..100 degrees Celsius laid out in steps of 25, are written once, in LinkedAxisTests",
            @"static readonly double\[\] FahrenheitTicks\b",
            "Tst/MatPlotLibNet/Rendering/LinkedAxisTests.cs") { Under = "Tst/", Finds = [
            "    private static readonly double[] Fahrenheit" + "Ticks = [50, 75, 100, 125, 150, 175, 200];"], Ignores = [
            "        Assert.Equal(FahrenheitTicks, placed.Ticks);"] },
        OwnedRule.Nowhere("what a colour bar draws for each orientation and extension is held by the theories of ColorBarRendererTests: no fact of the bar's matrix is a Baseline or a RenderColorBar_ fact of the axes renderer",
            @"\bvoid (?:Baseline_\w+|RenderColorBar_(?:Vertical|Horizontal)\w*(?:Extend|DrawEdges)\w*)\(",
            "Tst/MatPlotLibNet/Rendering/") with { Finds = [
            "    public void Baseline_" + "Vertical_ExtendNeither_NoLabel()",
            "    public void RenderColorBar_" + "VerticalExtendMin_DrawsUnderRect()",
            "    public void RenderColorBar_" + "HorizontalDrawEdges_DrawsPerStepEdges()"], Ignores = [
            "    public void RenderColorBar_WithLabel_RendersLabelText()",
            "    public void RenderColorBar_DegenerateRange_FallsBackToUnitRange()"] },
        OwnedRule.Nowhere("the number of every member of a public enum is pinned once, in EnumOrdinalSnapshot: no other test restates a member's number",
            @"Assert\.Equal\(\s*-?\d+\s*,\s*\(int\)\s*(?:\w+\.)*[A-Z]\w*\.[A-Z]\w*\s*\)|Assert\.Equal\(\s*\[[\d,\s]+\]\s*,\s*\w+\.Select\(\s*(\w+)\s*=>\s*\(int\)\s*\1\s*\)\s*\)",
            "Tst/") with { Finds = [
            "        Assert.Equal(4, (int)" + "InteractionToolbar.ToolMode.SpanSelect);",
            "        Assert.Equal(0, (int)" + "SpectralSides.Default);",
            "        Assert.Equal([1, 2, 4, 8], each." + "Select(r => (int)r));"], Ignores = [
            "        Assert.Equal(2, end.Cells[(int)DayOfWeek.Friday, end.ColumnOf(DateTime.MaxValue)]);",
            "        Assert.Equal(3, (int)Math.Round(value));",
            "        Assert.Equal(AnimationPlaybackState.Stopped, controller.State);"] },
        new("a process-wide slot of the library (the serializer, the chart renderer, the SVG renderer, the font metrics, the glyph outlines) is set for a test through its Holding, which writes back what the slot held however the test ends: no test writes a slot's Current by hand. A write the setter refuses, asserted by Assert.Throws in the slot's own test, changes no slot",
            @"(?<!Assert\.Throws<ArgumentNullException>\(\(\) => (?:[\w:]+\.)*)\b\w+Default\s*\.\s*Current\s*(?:\?\?)?=(?![=>])|\(\s*(?:[\w:]+\.)*\w+Default\s*\.\s*Current\s*,[^;]*\)\s*=(?![=>])",
            "Tst/MatPlotLibNet/TestFixtures/ChartServicesExtensions.cs") { Under = "Tst/", Finds = [
            "            FontMetricsDefault" + ".Current = metrics;",
            "        finally { GlyphPathProviderDefault" + ".Current = prev; }",
            "        global::MatPlotLibNet.ChartRendererDefault" + ".Current=CallersRenderer();",
            "        MatPlotLibNet.SvgRendererDefault" + ".Current ??= renderer;",
            "        (FontMetricsDefault" + ".Current, GlyphPathProviderDefault.Current) = (metrics, outlines);",
            "        Installed(metrics, () => ChartSerializerDefault" + ".Current = serializer);"], Ignores = [
            "            Assert.Throws<ArgumentNullException>(() => FontMetricsDefault" + ".Current = null!);",
            "        using var held = FontMetricsDefault.Holding(metrics);",
            "        Assert.Same(FontMetricsDefault.Current, ctx.BoxMetricsTold);",
            "        var before = ChartSerializerDefault.Current;",
            "        if (GlyphPathProviderDefault.Current == null) return;"] },
        OwnedRule.Nowhere("a test class holds a test: no test class, and no class nested in a test file, is left empty or holding only fields",
            @"(?m)^(?: {4,}(?:(?:public|internal|private|protected) )?(?:sealed )?class \w+|(?:(?:public|internal) )?(?:sealed )?class \w+Tests)[ \t]*\n[ \t]*\{(?:\s*private (?:static )?readonly [^;{}]+;)*\s*\}",
            "Tst/") with { AcrossLines = true, Finds = [
            "public class BollingerBandsTests\n{\n}",
            "public class WilliamsRTests\n{\n    private static readonly double[] High =\n        [18, 17];\n    private static readonly double[] Low = [10, 11];\n}",
            "    public class PlateCarree\n    {\n        private static readonly IGeoProjection P = GeoProjection.PlateCarree;\n\n    }"], Ignores = [
            "[CollectionDefinition(\"ChartServicesGlobalState\", DisableParallelization = true)]\npublic sealed class ChartServicesGlobalStateCollection\n{\n}",
            "public sealed class HistTypeOutputContractTests : EnumOutputContractTests<HistType>\n{\n    protected override Func<HistType, string> RenderDelegate => type => \"\";\n}",
            "public class ObvTests\n{\n    private static readonly double[] Close = [100, 102];\n\n    [Fact]\n    public void Compute_ReturnsFullLength() => Assert.Single(Close);\n}"] },
        OwnedRule.Nowhere("a test class is named for the type or the behaviour it tests, never for the round of work that wrote it or the reach it was written to lift: no Coverage, Branch, EdgeCase, Baseline, NearMiss, Misc, Smoke, Helper or release-numbered class",
            @"\bclass (?:\w*?(?:Coverage|Branch|EdgeCase|Baseline(?=Tests\b)|NearMiss|Misc|Smoke|Surgical|Pinpoint|DirectInvocation|Helper|Util|Phase[A-Z](?=[A-Z])|V\d{2,}|X\d|Series\d)\w*Tests|\w+TestsV\d+)\b",
            "Tst/") with { Finds = [
            "public class BranchCoverage_" + "SeriesTests",
            "public class AxesRendererCoverage" + "Tests",
            "public class GeoJsonReaderEdgeCase" + "Tests",
            "public class NearMissBranch" + "Tests",
            "public class X9dMisc" + "CoverageTests",
            "public class BaselineHelper" + "Tests",
            "public class CartesianSignalsPart" + "BaselineTests",
            "public class PhaseFIndicator" + "Tests",
            "public class V170AxisFeaturesFidelity" + "Tests : FidelityTest",
            "public class SignalRInteractionTests" + "V122 : IAsyncDisposable"], Ignores = [
            "public class StackedBaselineExtensionsTests",
            "public class PhaseSpectrumTests",
            "public class PatchEdgeTests",
            "public static class EdgeCaseData",
            "public class ChartSerializerTests"] },
        OwnedRule.Nowhere("a test asserts what the code it calls decides: no test's only assertion is that the markup holds an svg element, or that the markup or the figure it built is not null or empty, which every figure that draws without throwing passes",
            @"\bvoid \w+\([^)]*\)\s*(?:=>\s*Assert\.(?:Contains|StartsWith)\(\s*""<svg""[^;]*;|\{" + WithoutAnotherAssert + @"*?(?:Assert\.(?:Contains|StartsWith)\(\s*""<svg""[^;]*;|\b(?:var|string|Figure)\s+(\w+)\s*=\s*[^;]*\.(?:ToSvg|Build)\(\)\s*;" + WithoutAnotherAssert + @"*?Assert\.(?:NotNull|NotEmpty)\(\s*\1\s*\)\s*;)" + WithoutAnotherAssert + @"*\n {4}\})",
            "Tst/") with { AcrossLines = true, Finds = [
            "    [Fact]\n    public void AMathTitle_Renders()\n    {\n        var svg = Plt.Create().Plot([1.0], [2.0]).ToSvg();\n        Assert.Not" + "Null(svg);\n        Assert.Contains(\"<" + "svg\", svg);\n    }\n",
            "    [Fact]\n    public void TheBuilder_BuildsWithoutException()\n    {\n        var figure = Plt.Create().Plot([1.0], [2.0]).Build();\n        Assert.Not" + "Null(figure);\n    }\n",
            "    [Theory]\n    [MemberData(nameof(EdgeCases))]\n    public void ASeriesAtTheEdge_StillDrawsAWholeDocument(Action<AxesBuilder> configure)\n    {\n        var svg = Render(configure);\n        Assert.Contains(\"<" + "svg\", svg);\n        Assert.Contains(\"</" + "svg>\", svg);\n    }\n",
            "    [Fact]\n    public void TwoNodes_RenderWithoutError()\n    {\n        var svg = Plt.Create().Plot([1.0], [2.0]).ToSvg();\n        Assert.Contains(\"<" + "svg\", svg);\n    }\n",
            "    [Fact]\n    public void APoint_Renders() => Assert.StartsWith(\"<" + "svg\", Plt.Create().Plot([1.0], [2.0]).ToSvg());\n",
            "    [Fact]\n    public void AnEmptyGrid_HitsTheEarlyReturn()\n    {\n        var fig = Plt.Create().Build();\n        Assert.StartsWith(\"<" + "svg\", fig.ToSvg());\n    }\n"], Ignores = [
            "    [Fact]\n    public void TwoNodes_DrawTwoBoxes()\n    {\n        var svg = Plt.Create().Plot([1.0], [2.0]).ToSvg();\n        Assert.Contains(\"<svg\", svg);\n        Assert.Equal(2, Reg" + "ex.Count(svg, \"<rect\"));\n    }\n",
            "    [Fact]\n    public void AToken_IsDrawn()\n    {\n        var svg = Plt.Create().Plot([1.0], [2.0]).ToSvg();\n        Assert.Contains(\"<svg\", svg);\n        svg.AssertHoldsToken(\"<polyline\");\n    }\n",
            "    [Fact]\n    public void TheFile_IsAnSvgDocument() => Assert.StartsWith(\"<?xml\", File.ReadAllText(path));\n",
            "    [Fact]\n    public void ALookup_FindsTheMap()\n    {\n        var map = ColorMapRegistry.Get(\"viridis\");\n        Assert.NotNull(map);\n    }\n"] },
        OwnedRule.Nowhere("a row of the markup theory carries a token the drawing decides: no row's only token is the svg element, which every figure that draws without throwing writes",
            @"[)}]\s*,\s*""<svg""(?:\s*,\s*""</svg>"")?\s*\)\s*,?\s*$",
            "Tst/") with { Finds = [
            "        new DrawnToken(\"NoTrees_RendersValidSvg\", () => RenderSvg(), \"<" + "svg\", \"</svg>\"),",
            "            .ToSvg(), \"<" + "svg\"),"], Ignores = [
            "            .ToSvg(), \"Chart legend\", \"<svg\"),",
            "        }, \"<svg\") { Absent = [\"NaN\"] },"] },
        OwnedRule.InComments("a comment of a test says what the test pins, never where the code it reached stood: no line number of a file of the repository, said as line NN, LNN or File.cs:NN (a matplotlib citation names its .py file on the same line)",
            @"^(?:(?!\.py\b).)*?(?:(?<![\w/])(?:[Ll]ines?\s\d{2,}(?![\d.]|\s*(?:px|pt|pixels?|points?|%))|L\d{2,}\b)(?!\s+of\s+\S*\.(?:csv|json|txt|dat|tsv|geojson)\b)|\.(?:cs|md|csproj|razor|js|ts|yml|ps1)[:#]L?\d{2,})",
            "Tst/") with { Finds = [
            "    /" + "// <summary>FromJson line 45 — `?? throw` true arm: empty/whitespace input.</summary>",
            "    /" + "/ ── L71 (true) ────────────────────────────────────────────",
            "    /" + "// (<c>else if (axes.YAxis.Min.HasValue) return Format(Min)</c>, line 309-310).",
            "        packer.Flush();        /" + "/ _bitsInPending == 0 → L203 FALSE arm: if-body skipped",
            "    /" + "/ Lines 277-281 in ChartSerializer.cs — switch on dto.Type for secondary",
            "    /" + "// <summary>Round-trips an Inset axes with its own series (lines 298-309).</summary>",
            "    /" + "// ColorJsonConverter.cs L1060: `hex is not null ? Color.FromHex(hex) : default` null arm.",
            "    /" + "// <c>SvgInteractivityScript.cs</c> (L209-221) is in place and correct; this",
            "    /" + "// FigureTemplates.FinancialDashboard lines 70/81/103 — the three",
            "    /" + "/ ── Coverage: sub-pixel cell skip (PairGridSeriesRenderer.cs:36-37) ───────",
            "    /" + "// empty-series default (<c>new(0, 1, -1, 1)</c>, see <c>ResidualSeries.cs:38</c>), and reports why",
            "    /" + "// <summary>The owner's exact spec (ROADMAP.md:101): \"4 axes\" — the primary Y",
            "    /" + "/ the guard of ChartSerializer.cs#L45 is taken when the input is empty"], Ignores = [
            "    /" + "// the header is on cities.csv:12 and the test reads it",
            "    /" + "// axes3d.py:_on_move (lines 1746-1843) is the formula",
            "    /" + "// (<c>axis3d.py</c> lines 288-297): elevation ±90 collapses the X/Y pair",
            "    /" + "// axes3d.py:format_deltas L4492",
            "    /" + "// <c>mpl_toolkits/mplot3d/axes3d.py</c> L458-470) draws in FIXED",
            "    /" + "// the header ends on line 12 of cities.csv",
            "    /" + "/ Rule L1: whitespace at the end of the line takes the paragraph level",
            "    /" + "// the 2.5 px wide lines 1 pixel apart",
            "    /" + "// 11 vertical lines 37.5 px apart, on numeric ticks",
            "    /" + "// all lines 12 pixels wide in the colour of the theme",
            "        var message = \"line 45 of the file\";",
            "        var url = \"http://host/line 45\";"] },
        new("the comment lines of a file are told apart by SourceFile only: no test tests a line for a leading comment mark, lists the marks or looks for one in a line by itself",
            @"\bStartsWith\(\s*""/{2,3}""|[\[,{]\s*""/{2,3}""\s*[,\]}]|\bIndexOf\(\s*""/{2,3}""|\(\?:/{2,3}[|)]|\bTrim(?:Start)?\(\)\s*\.StartsWith\(\s*['""]#",
            "Tst/MatPlotLibNet/Packaging/SourceFile.cs") { Under = "Tst/", Finds = [
            "        string[] lines = [.. text.Split('\\n').Where(line => !line.TrimStart()." + "StartsWith('#'))];",
            "        var code = lines.Where(line => !line.Trim()." + "StartsWith(\"#\", StringComparison.Ordinal));",
            "        var code = lines.Where(line => !line.TrimStart().StartsWith(\"/" + "/\", StringComparison.Ordinal));",
            "    private static readonly string[] NoCodeStarts = [\"/" + "/\", \"/" + "//\", \"using \", \"namespace \"];",
            "    private static bool IsDocumentation(string line) => line.TrimStart().StartsWith(\"/" + "//\", StringComparison.Ordinal);",
            "                int comment = line.IndexOf(\"/" + "/\", StringComparison.Ordinal);",
            "                var own = OwnOf(root.FindToken(start + line.IndexOf(\"/" + "//\", StringComparison.Ordinal)).Parent ?? root);",
            "    private const string CodeInsideTheType = @\"^(?!\\s*(?:/" + "/|\\{|\\}|$))(?!.*\\b(?:class|namespace)\\b).+\";"], Ignores = [
            "    private const string Compared = @\"(?:,\\s*StringComparison\\.\\w+)?\\);[ \\t]*(?:/" + "/[^\\n]*)?\";",
            "            text => Assert.StartsWith(\"#\", text));",
            "        var url = \"http://host/line 45\";",
            "        string path = prefix + \"/\" + name;",
            "        if (line.TrimStart().StartsWith(\"using \", StringComparison.Ordinal)) continue;"] },
        new("the lines of a text are cut by SourceFile only: no test cuts a text, or the text of a file, at a line end by hand, whatever text it is (a document or a section of one, a wiki page, an svg or a markdown table the library wrote, the schema an agent is shown, an exception message), none reads a file by its lines, none turns its line ends into one kind by hand, and none cuts at a literal that holds a line end (a fenced block, the front matter, a section or a block of a yaml file are asked of SourceFile: FencedBlocksOf, FrontMatterOf, SectionsOf, YamlChildrenOf); a csv is cut at its own line end by CrlfLinesOf, the place of an offset on its line is told by LineAt, LineEndAt and LineIndexAt (SourceFile.LinesOf, ElementLinesOf, CrlfLinesOf, Lines and LineFeedTextOf say the rest); the one reader that is beside the owner reads the lines of a pipe as they arrive (McpStdioTests), which is no text to cut",
            @"\.(?:Split|(?:Last)?IndexOf(?:Any)?)\((?:[$@]*""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)+'|[^()""'$@]|[$@](?![""$@])|(?<d>\()|(?<-d>\)))*?(?:[$@]*""(?:[^""\\]|\\[^nru])*\\(?:[nr]|u000[AaDd])|'\\(?:[nr]|u000[AaDd]|x0?[AaDd])'|Environment\.NewLine|\(char\)\s*1[03]\b|(?:""""""|@"")\s*$)|\.ReplaceLineEndings\(|\bnew StringReader\(|\.EnumerateLines\(|\.Replace\(\s*""\\r(?:\\n)?""|\bText\.Split\(|\bTextOf\([^()]*\)\.Split\(|\.Count\(\s*'\\[nr]'\s*\)|[=!]=\s*'\\[nr]'|'\\[nr]'\s*[=!]=|\bReadLine(?:Async)?\(|\bFile\.Read(?:AllLines|Lines)\(|\b(?:ReadAllText|Read)\((?:[^()]|\((?:[^()]|\([^()]*\))*\))*\)\s*\.Split\(",
            "Tst/MatPlotLibNet/Packaging/SourceFile.cs", "Tst/MatPlotLibNet.Mcp/McpStdioTests.cs") { Under = "Tst/", Finds = [
            "        string[] lines = [.. Script()." + "ReplaceLineEndings(\"\\n\").Split('\\n')];",
            "        var lines = section." + "Split('\\n');",
            "        var page = text." + "Split(\"\\n\\n\", 2)[0];",
            "        var blocks = page." + "Split(\"```json\\n\").Skip(1);",
            "        int end = markdown." + "IndexOf(\"\\n---\", 3, StringComparison.Ordinal);",
            "        var parts = section." + "Split(\"\\n## \");",
            "        var parts = text." + "Split(separator: \"\\r\\n\\r\\n\");",
            "        var parts = text." + "Split([\"a\", \"b\\n\"], StringSplitOptions.None);",
            "        int at = text." + "LastIndexOf($\"\\n## {heading}\");",
            "        var parts = text." + "Split(sep + \"\\n\");",
            "        var parts = Regex." + "Split(text, \"\\n\\n\");",
            "        var parts = text." + "Split(string.Concat(a, \"\\n\"));",
            "        int at = text." + "IndexOf(Pick(a), \"\\n\");",
            "        var parts = text." + "Split(\"\"\"",
            "        int at = text." + "IndexOf(@\"",
            "        var parts = text." + "Split((char)10);",
            "        var parts = text." + "Split('\\u000A');",
            "        return node.ToString()." + "Split(\"\\r\\n\")[0].Trim();",
            "        var lines = svg." + "Split(\"\\n\");",
            "        var lines = svg." + "Split(new[] { '\\n' });",
            "        var lines = svg." + "Split(['\\r', '\\n']);",
            "        string first = ex.Message." + "Split(Environment.NewLine)[0];",
            "        var records = table.ToCsv()." + "Split(\"\\r\\n\", StringSplitOptions.RemoveEmptyEntries);",
            "        Assert.DoesNotContain(\"\\n\", csv." + "Replace(\"\\r\\n\", \"\"));",
            "        string svg = text." + "Replace(\"\\r\", \"\");",
            "        using var reader = new " + "StringReader(svg);",
            "        foreach (var line in svg." + "EnumerateLines())",
            "        var cells = file.Text." + "Split(',');",
            "        var cells = TextOf(path)." + "Split(',');",
            "        string firstLine = Read(\"Src\", \"ARCHITECTURE.md\")." + "Split(' ')[0];",
            "        foreach (var raw in File." + "ReadLines(Path.Combine(Root, \"ucd.txt\")))",
            "        var rules = File." + "ReadAllLines(Path.Combine(Root, \".gitignore\"));",
            "        int start = text." + "LastIndexOf('\\n', index) + 1;",
            "        int end = text." + "IndexOf('\\n', index);",
            "                int end = text." + "IndexOf(\"\\n\", i, StringComparison.Ordinal);",
            "        int cut = text." + "IndexOfAny(['\\r', '\\n']);",
            "        int line = file.Code.AsSpan(0, match.Index)." + "Count('\\n');",
            "        int lines = text.Count(c => c " + "== '\\n');",
            "        while (reader.Read" + "Line() is { } line)"], Ignores = [
            "        Assert.Contains('\\n', text);",
            "        string joined = string.Join('\\n', lines);",
            "        int close = svg.IndexOf(\"</script>\", start, StringComparison.Ordinal);",
            "        int at = line.IndexOf(':');",
            "        var words = description.Text.Trim().Split(' ');",
            "        var parts = page." + "Split(\"chart types are\", 2);",
            "        var parts = text." + "Split(\"\\\\n\");",
            "        var parts = Regex." + "Split(text, @\"\\s+\");",
            "        var parts = text." + "Split(Pick(a), \"b\");",
            "        var lines = SourceFile.LinesOf(text);",
            "        var lines = file.Lines;"] },
        new("the sections of a markdown document are cut by SourceFile only (SectionOf, SectionsOf, IntroductionOf): no test finds a heading with IndexOf, Split or StartsWith, or lists the headings of a document with a regular expression over its lines",
            @"\b(?:IndexOf|LastIndexOf|Split|StartsWith)\(\s*\$?""(?:\\n)?#{2,4} |@?""\^#{2,4} |@?""\^#\{1,6\}",
            "Tst/MatPlotLibNet/Packaging/SourceFile.cs") { Under = "Tst/", Finds = [
            "        int start = markdown.IndexOf($\"\\n## {heading}\", StringComparison.Ordinal);",
            "        int next = markdown.IndexOf(\"\\n## \", start + 1, StringComparison.Ordinal);",
            "            .. Page().Split(\"\\n## \").Skip(1)",
            "        .TakeWhile((line, index) => index == 0 || !line.StartsWith(\"## \", StringComparison.Ordinal))",
            "        var releases = Regex.Matches(changelog, @\"^" + "## \\[(\\d+\\.\\d+\\.\\d+)\\]\", RegexOptions.Multiline)",
            "            else if (!inside && Regex.Match(line, @\"^" + "#{1,6}\\s+(?<heading>.*)$\") is { Success: true } heading)"], Ignores = [
            "        Assert.Contains($\"## [{version}]\", changelog, StringComparison.Ordinal);",
            "                .Contains(\"### \" + Name(row)))",
            "        int heading = readme.IndexOf(\"Where this is going\", StringComparison.Ordinal);",
            "        var parts = text." + "Split(\"\\n\\n\");"] },
        new("the fences of a markdown document are read by SourceFile only (FenceBlocksOf, MarkdownLinesOf, FencedBlocksOf, MarkdownFence.Of): no test decides which line is a fence or which lines lie inside one, by a toggle on a line that starts with three backticks, a trim of backticks, a regular expression over the fence lines, a split at a fence, or a tilde fence",
            @"\.(?:StartsWith|EndsWith|Contains|IndexOf|LastIndexOf|Split|Count|IsMatch|Matches?|Replace)\([^\n]*?(?<!\\)""[^""\n]*(?:`{3}|~{3}|(?:\\x60|\\u0060){3})|\b(?:new Regex|GeneratedRegex)\([^\n]*?(?<!\\)""[^""\n]*(?:`{3}|~{3})|\.(?:Trim|TrimStart|TrimEnd|Contains|StartsWith|EndsWith)\(\s*'`'|\bnew string\(\s*'`'",
            "Tst/MatPlotLibNet/Packaging/SourceFile.cs") { Under = "Tst/", Finds = [
            "            if (line.TrimStart()." + "StartsWith(\"```\", StringComparison.Ordinal))",
            "        var ticks = trimmed.Length - trimmed." + "TrimStart('`').Length;",
            "        var fences = Regex." + "Count(text, @\"^\\s*```csharp\\s*$\", RegexOptions.Multiline);",
            "        var blocks = page." + "Split(\"```json\\n\").Skip(1);",
            "        var tilde = line." + "StartsWith(\"~~~\", StringComparison.Ordinal);",
            "    [GeneratedRegex" + "(@\"^ {0,3}```(?<info>.*)$\")]",
            "        var open = new Regex" + "(\"^```\", RegexOptions.Multiline);",
            "        var words = line." + "Contains('`', StringComparison.Ordinal);",
            "        var rule = new string" + "('`', 3);",
            "        var fence = line." + "IndexOf(\"\\u0060\\u0060\\u0060\", StringComparison.Ordinal);"], Ignores = [
            "        var arity = type.Name." + "Split('`')[0];",
            "        var at = name." + "IndexOf('`');",
            "        var page = WikiPage.Read(\"Page.md\", \"```csharp\\nvar a = 1;\\n```\\n\");",
            "    [InlineData(\"```\", \"Rendering/\", \"    Categorical/\", \"```\")]",
            "        var bare = line." + "Replace(\"``\", string.Empty);",
            "        Assert." + "Contains(\"`x`\", page, StringComparison.Ordinal);"] },
        new("the pictures a wiki page shows are read by Wiki.PictureLink only: no wiki test reads the links to the images folder with a pattern of its own, in a regular expression or in a search of the text",
            @"\b(?:new Regex|GeneratedRegex|Regex\.(?:Matches?|IsMatch|Replace|Split|Count))\([^\n]*?""[^""\n]*(?:\(images/|!\\\[|src=""""images/)|\.(?:IndexOf|Split|Contains|StartsWith)\([^\n]*?""[^""\n]*\]\(images/",
            "Tst/MatPlotLibNet.Wiki/Wiki.cs") { Under = "Tst/", Finds = [
            "    [GeneratedRegex" + "(@\"\\(images/(?<name>[^)\\s#]+)\\)\")]",
            "        var pictures = Regex." + "Matches(text, @\"!\\[[^\\]]*\\]\\(images/(?<name>[^)]+)\\)\");",
            "        var at = text." + "IndexOf(\"](images/\", StringComparison.Ordinal);",
            "        var shown = new Regex" + "(@\"<img src=\"\"images/(?<name>[^\"\"]+)\"\"\");"], Ignores = [
            "            \"![x](images/x.png)\",",
            "        var link = new Regex" + "(@\"\\[\\[(?<page>[^\\]]+)\\]\\]\");",
            "        var path = Path.Join(folder, \"images\", name);"] },
        OwnedRule.Nowhere("the cells of a markdown table row are cut by SourceFile only (TableCellsOf): no test splits a row of a table at its bars, with Split or with a regular expression over the row",
            @"\b(?:row|rows|cells?|line|header)\b[^\n;]*?\.Split\(\s*(?:'\|'|""\|"")|@?""\^\\\|\(\?<cells>",
            "Tst/") with { Finds = [
            "            var cells = row.Groups[\"cells\"].Value." + "Split('|');",
            "    private static string Name(string row) => row." + "Split('|')[2].Trim();",
            "        var rows = Regex.Matches(page, @\"" + "^\\|(?<cells>.+)\\|\\s*$\", RegexOptions.Multiline);"], Ignores = [
            "            var parts = expected.Split('|');",
            "        string[] expected = secondSlot.Split('|');",
            "        var cells = SourceFile.TableCellsOf(row);"] },
        new("the inline code spans of a markdown line are read by SourceFile only (CodeSpansOf, WithoutCodeSpans, SoleCodeOf): no test finds the text between backticks with a regular expression of its own",
            @"(?:\bnew Regex\(|\bGeneratedRegex\(|\bRegex\.[A-Za-z]+\((?:[^\n""]*?,\s*)?|\bnew\()[@$]*""`",
            "Tst/MatPlotLibNet/Packaging/SourceFile.cs") { Under = "Tst/", Finds = [
            "    [GeneratedRegex(\"`[^`]*`\")]",
            "            var span = Regex.Match(cells[1], @\"`(?<code>[^`]+)`\");",
            "        return [.. Regex.Matches(text[(at + label.Length)..], \"`([A-Za-z0-9_]+)`\").Select(match => match.Groups[1].Value)];",
            "    private static readonly Regex Spans = new(\"`([^`\\n]+)`\");"], Ignores = [
            "        Assert.Contains(\"`x`\", page, StringComparison.Ordinal);",
            "        var arity = type.Name.Split('`')[0];",
            "    [GeneratedRegex(@\"^[A-Z]\\w*\")]",
            "    [GeneratedRegex(@\"^\\s*(?:\\([^()`]*\\)\\s*)?(?:[,/]\\s*)?$\")]"] },
        new("a test compiles C# in three places: the packages' sources (SourceCompilation), the wiki's blocks as a reader runs them (WikiProgramExtensions) and every call a document writes, a placeholder standing for any value (CallCompilation); no other test compiles a document's code",
            @"\bCSharpCompilation\.Create\(",
            "Tst/MatPlotLibNet/Packaging/SourceCompilation.cs", "Tst/MatPlotLibNet.Wiki/WikiProgramExtensions.cs", "Tst/MatPlotLibNet.Wiki/CallCompilation.cs") { Under = "Tst/", Finds = [
            "        var compilation = CSharpCompilation." + "Create(\"Doc\", [tree], references);",
            "            var check = Microsoft.CodeAnalysis.CSharp.CSharpCompilation." + "Create(name, trees, refs, options);"], Ignores = [
            "        var tree = CSharpSyntaxTree.ParseText(code);",
            "        var compilation = SourceCompilation.Create(package, files, references);"] },
        new("which assembly the readers of the documents count as one of the packages (the library's own, no suite, no sample program) is told by PackageSurface.IsPackageAssembly only: the types loaded, the names of .NET and the receiver a member is missing on read the one rule",
            @"(?:\b[Aa]ssembly\w*(?:\.Name)?|GetFileName(?:WithoutExtension)?\([^()]*\))\s*\.StartsWith\(\s*""MatPlotLibNet""",
            "Tst/MatPlotLibNet.Wiki/PackageSurface.cs") { Under = "Tst/MatPlotLibNet.Wiki/", Finds = [
            "                     .Where(assembly => !assembly.Name." + "StartsWith(\"MatPlotLibNet\", StringComparison.Ordinal)))",
            "            .Where(path => Path.GetFileName(path)." + "StartsWith(\"MatPlotLibNet\", StringComparison.Ordinal)"], Ignores = [
            "        PackageSurface.Loaded.Namespaces.Where(name => name.StartsWith(\"MatPlotLibNet\", StringComparison.Ordinal))",
            "               && PackageSurface.IsPackageAssembly(assembly.Name)"] },
        OwnedRule.Nowhere("a call a document writes in a code span is read by CallClaim only (compiled by CallCompilation): no test reads a receiver or a member out of a span with a regular expression of its own",
            @"\(\?<(?:receiver|member|call|method)>", "Tst/") with { Finds = [
            "            var call = Regex.Match(span.Code.Trim(), @\"^(?<" + "receiver>[A-Za-z_]\\w*)\\.(?<" + "member>[A-Za-z_]\\w*)\");",
            "    [GeneratedRegex(@\"\\.(?<" + "call>[A-Z]\\w*)\\(\")]"], Ignores = [
            "    [GeneratedRegex(@\"^(?<key>[a-z_][a-z0-9_]*(?:\\.[a-z0-9_]+)+)\")]",
            "        var name = Regex.Match(text, @\"(?<name>\\w+)\");"] },
    ];

    /// <summary>One character of a test's body that is neither its closing brace nor the start of an assertion, other than one that a
    /// value is not null or not empty or one more that the markup opens or closes an svg element: the render rule reads a body with it,
    /// so a body that asserts anything more is not matched.</summary>
    private const string WithoutAnotherAssert = @"(?:(?!\n {4}\})(?!Assert(?!\.(?:NotNull|NotEmpty)\(|\.(?:Contains|StartsWith|EndsWith)\(\s*""</?svg))(?![\w.]Assert)[\s\S])";

    /// <summary>The rule that a library interface a test stands in for has one double, written once in <paramref name="owner"/>: no test
    /// class implements it afresh, in any suite. A class that derives from one of the owner's doubles (<paramref name="leaves"/>) may
    /// list the interface again, to implement a member the interface gives a default body.</summary>
    /// <param name="rule">What the rule says, in a sentence.</param>
    /// <param name="type">The interface, as a pattern over its name.</param>
    /// <param name="owner">The file that holds the double.</param>
    /// <param name="leaves">The owner's doubles a leaf may derive from and list the interface again.</param>
    /// <returns>The rule, read across lines under <c>Tst</c>; the row adds its probes.</returns>
    private static OwnedRule OneDouble(string rule, string type, string owner, params string[] leaves) =>
        new(rule, $@"\b(?:class|record|struct)\s+\w+(?:{Arguments})?\s*:(?>\s*)"
            + (leaves.Length == 0 ? "" : $@"(?!(?:{string.Join('|', leaves)})\b)")
            + $@"(?:[\w.<>]+(?:{Arguments})?\s*,\s*)*(?:global::)?(?:\w+\.)*{type}\b", owner) { Under = "Tst/", AcrossLines = true };

    /// <summary>A parenthesised list, its nested parentheses balanced: a primary constructor's parameters or a base's arguments, a
    /// lambda among them.</summary>
    private const string Arguments = @"\((?>[^()]+|\((?<open>)|\)(?<-open>))*(?(open)(?!))\)";
}
