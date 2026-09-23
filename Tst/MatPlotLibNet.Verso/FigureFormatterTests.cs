// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Models;
using Verso.Abstractions;

namespace MatPlotLibNet.Verso.Tests;

/// <summary>
/// What a Verso cell does with a chart. The formatter draws nothing itself — it hands the library's own SVG to
/// the notebook — so these tests are about the four decisions it does make: which values it claims, which
/// representation it answers with, how big the picture may be, and what a reader sees when a render fails.
/// </summary>
public class FigureFormatterTests
{
    private static Figure ASimpleFigure() =>
        Plt.Create().Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]).Build();

    // ---- what it claims ------------------------------------------------------------------------------------

    [Fact]
    public void ItAnswersForAFigure_AndForTheBuildersThatMakeOne()
    {
        // Every entry point the library offers can end a cell, and a cell hands over its last expression rather
        // than a type somebody chose. `Plt.Create()…` without `.Build()` is the shape people actually write.
        var formatter = new FigureFormatter();
        var context = new StubFormatterContext("text/html");

        Assert.True(formatter.CanFormat(ASimpleFigure(), context));
        Assert.True(formatter.CanFormat(Plt.Create().Plot([1.0], [2.0]), context));
        Assert.True(formatter.CanFormat(Plt.Mosaic("AB"), context));
    }

    [Fact]
    public void ItLeavesEverythingElseAlone()
    {
        var formatter = new FigureFormatter();
        var context = new StubFormatterContext("text/html");

        Assert.False(formatter.CanFormat("a string", context));
        Assert.False(formatter.CanFormat(42, context));
    }

    [Fact]
    public void SomethingElseFromTheCharting_LibraryIsNotAChart()
    {
        // The assembly name alone is not the test. A cell that ends on a theme, a colormap or an axes hands over
        // a value from the very library this formatter waits for, and none of them is a picture — those belong
        // to the notebook's own formatters, which can print them.
        var formatter = new FigureFormatter();
        var context = new StubFormatterContext("text/html");

        Assert.False(formatter.CanFormat(FakeLibraryAssembly.ValueWithNoRenderer("MatPlotLibNet.Styling.Theme"), context));
    }

    [Fact]
    public void ItClaimsOnlyTheRepresentationsItCanProduce()
    {
        var formatter = new FigureFormatter();

        Assert.True(formatter.CanFormat(ASimpleFigure(), new StubFormatterContext("text/html")));
        Assert.True(formatter.CanFormat(ASimpleFigure(), new StubFormatterContext("image/svg+xml")));
        Assert.False(formatter.CanFormat(ASimpleFigure(), new StubFormatterContext("text/plain")));
        Assert.False(formatter.CanFormat(ASimpleFigure(), new StubFormatterContext("application/json")));
    }

    [Fact]
    public void ItIsAskedBeforeTheBuiltInsAndDoesNotPretendToBeAFallback()
    {
        // The resolver pre-filters on SupportedTypes and then orders by priority, descending. Object is the only
        // type that crosses a load-context boundary, so the real filter is CanFormat — and 40 sits clear of every
        // formatter Verso ships (the highest is 50, for exceptions and data frames).
        var formatter = new FigureFormatter();

        Assert.Equal([typeof(object)], formatter.SupportedTypes);
        Assert.Equal(40, formatter.Priority);
    }

    // ---- what it answers with ------------------------------------------------------------------------------

    [Fact]
    public async Task AFigureComesBackAsTheSvgTheLibraryAlreadyRenders()
    {
        var figure = ASimpleFigure();

        var output = await new FigureFormatter().FormatAsync(figure, new StubFormatterContext("text/html"));

        Assert.Equal("text/html", output.MimeType);
        Assert.False(output.IsError);
        Assert.Contains(figure.ToSvg(), output.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ABuilderIsBuiltFirst_AndDrawsTheSameChart()
    {
        var output = await new FigureFormatter()
            .FormatAsync(Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]), new StubFormatterContext("text/html"));

        Assert.Contains("<svg", output.Content, StringComparison.Ordinal);
        Assert.False(output.IsError);
    }

    [Fact]
    public async Task AMosaicBuilderToo()
    {
        var output = await new FigureFormatter()
            .FormatAsync(Plt.Mosaic("AB"), new StubFormatterContext("text/html"));

        Assert.Contains("<svg", output.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AskedForAnImage_ItHandsTheSvgOverBare()
    {
        // Under image/svg+xml the notebook renders the picture itself, so a wrapper would be markup inside an
        // image. The HTML route is the one that keeps the links, the accessible name and the responsive width.
        var output = await new FigureFormatter().FormatAsync(ASimpleFigure(), new StubFormatterContext("image/svg+xml"));

        Assert.Equal("image/svg+xml", output.MimeType);
        Assert.StartsWith("<svg", output.Content, StringComparison.Ordinal);
    }

    // ---- how big it may be ---------------------------------------------------------------------------------

    [Fact]
    public async Task AChartThatFitsTheCell_IsHandedOverUntouched()
    {
        // The library's own SVG is already responsive — width:100% with a viewBox — so a chart the cell can hold
        // needs no wrapper at all, and adding one would put a scrollbar around every picture.
        var figure = ASimpleFigure();

        var output = await new FigureFormatter()
            .FormatAsync(figure, new StubFormatterContext("text/html", maxWidth: 800, maxHeight: 600));

        Assert.Equal(figure.ToSvg(), output.Content);
    }

    [Fact]
    public async Task ATallChartInAShortCell_GetsAScrollBoxAndKeepsItsProportions()
    {
        // 800 x 2400 projected into a 700-wide cell is 2100 tall; in a 400-tall cell that is the one case where
        // the reader needs a box. The figure itself is never resized: rewriting Height re-runs the layout, so the
        // caller's twelve rows would come back as slivers.
        var figure = Plt.Create().WithSize(800, 2400).Plot([1.0, 2.0], [3.0, 4.0]).Build();

        var output = await new FigureFormatter()
            .FormatAsync(figure, new StubFormatterContext("text/html", maxWidth: 700, maxHeight: 400));

        Assert.Contains("max-height:400", output.Content, StringComparison.Ordinal);
        Assert.Contains("overflow-y:auto", output.Content, StringComparison.Ordinal);
        Assert.Contains(figure.ToSvg(), output.Content, StringComparison.Ordinal);
        Assert.Equal(2400, figure.Height);
    }

    [Fact]
    public async Task ACellThatNamesNoHeight_GetsNoBox()
    {
        var figure = Plt.Create().WithSize(800, 2400).Plot([1.0, 2.0], [3.0, 4.0]).Build();

        var output = await new FigureFormatter()
            .FormatAsync(figure, new StubFormatterContext("text/html", maxWidth: 700, maxHeight: 0));

        Assert.Equal(figure.ToSvg(), output.Content);
    }

    // ---- what a reader sees when it goes wrong -------------------------------------------------------------

    [Fact]
    public async Task AValueItCannotDraw_ComesBackAsAnErrorAndNotAsAnException()
    {
        var output = await new FigureFormatter().FormatAsync("not a figure", new StubFormatterContext("text/html"));

        Assert.True(output.IsError);
        Assert.Contains("figure", output.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AChartThatRefusesToDraw_HandsBackTheRenderersOwnMessage_NotTheWrapper()
    {
        // Reflection wraps whatever the renderer threw, and the wrapper's message ("Exception has been thrown by
        // the target of an invocation") tells a reader nothing. Here the mosaic parser is the one with something
        // to say — two rows of different lengths — and it has to arrive in the cell intact.
        var output = await new FigureFormatter().FormatAsync(Plt.Mosaic("AB\nCDE"), new StubFormatterContext("text/html"));

        Assert.True(output.IsError);
        Assert.Contains("All rows must have the same length", output.Content, StringComparison.Ordinal);
        Assert.Equal("System.ArgumentException", output.ErrorName);
        Assert.False(string.IsNullOrWhiteSpace(output.ErrorStackTrace));
    }

    [Fact]
    public async Task AValueFromAnAssemblyThisPackageHasNeverSeen_IsStillClaimed()
    {
        // The design in one test. The value comes from a second assembly calling itself MatPlotLibNet, which is
        // what a notebook's own load context produces; a typeof comparison would reject it forever. It is
        // claimed by name — and the rendering verb is looked for on THAT assembly, where this stranger has none,
        // so the cell is told exactly what was missing rather than shown an empty output.
        object stranger = FakeLibraryAssembly.ValueWithNoRenderer("MatPlotLibNet.FigureBuilder");
        var formatter = new FigureFormatter();
        var context = new StubFormatterContext("text/html");

        Assert.True(formatter.CanFormat(stranger, context));

        var output = await formatter.FormatAsync(stranger, context);

        Assert.True(output.IsError);
        Assert.Contains("ToSvg()", output.Content, StringComparison.Ordinal);
        Assert.Equal("System.MissingMethodException", output.ErrorName);
    }

    [Fact]
    public async Task AnSvgWhoseRootDeclaresNoSize_IsHandedOverWithoutABox()
    {
        // A picture that does not say how big it is cannot overflow the cell — it scales into whatever room it
        // is given. The fitting rule has to read a missing width as "fits", not as "unknown, so wrap it": the
        // cell here is deliberately far too short for the aspect ratio, and still nothing is wrapped.
        const string svg = "<svg viewBox=\"0 0 10 10\"><rect width=\"10\" height=\"10\" /></svg>";
        object builder = FakeLibraryAssembly.ValueRendering("MatPlotLibNet.FigureBuilder", svg);

        var output = await new FigureFormatter()
            .FormatAsync(builder, new StubFormatterContext("text/html", maxWidth: 400, maxHeight: 10));

        Assert.Equal(svg, output.Content);
    }

    [Fact]
    public async Task ACellThatHandsOverNothingAtAll_IsToldThat()
    {
        // A cell can end on a null, and the refusal names what arrived. "nothing" is the one case where naming
        // the type is impossible, so it is the one case where the sentence has to be written out.
        var output = await new FigureFormatter().FormatAsync(null!, new StubFormatterContext("text/html"));

        Assert.True(output.IsError);
        Assert.Contains("nothing", output.Content, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ARendererThatAnswersWithNothing_IsReportedRatherThanShownAsAnEmptyCell(string? rendered)
    {
        // An empty output is the worst answer available: the reader sees a blank cell and cannot tell a chart
        // with no data from a chart that was never drawn. Both empty answers become the same sentence.
        object builder = FakeLibraryAssembly.ValueRendering("MatPlotLibNet.FigureBuilder", rendered);

        var output = await new FigureFormatter().FormatAsync(builder, new StubFormatterContext("text/html"));

        Assert.True(output.IsError);
        Assert.Contains("no SVG", output.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnSvgWhoseSizeCannotBeRead_IsTreatedAsUnsized()
    {
        // The size is read off the root element as text, and text can be anything: an attribute with no value,
        // a percentage, a unit. Nothing there is worth an exception — a size that cannot be read is a chart
        // whose proportions are unknown, and an unknown chart is handed over as it is.
        object builder = FakeLibraryAssembly.ValueRendering(
            "MatPlotLibNet.FigureBuilder", "<svg width=\"\" height=\"12pt\"><rect /></svg>");

        var output = await new FigureFormatter()
            .FormatAsync(builder, new StubFormatterContext("text/html", maxWidth: 400, maxHeight: 10));

        Assert.False(output.IsError);
        Assert.StartsWith("<svg", output.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("overflow-y", output.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnSvgWithNoElementToReadAtAll_StillReachesTheCell()
    {
        // Truncated output, or something that is not markup at all. The formatter is not a validator: whatever
        // the library answered is what the reader gets, because a cell showing nothing would hide the problem.
        object builder = FakeLibraryAssembly.ValueRendering("MatPlotLibNet.FigureBuilder", "<svg width=\"800\"");

        var output = await new FigureFormatter()
            .FormatAsync(builder, new StubFormatterContext("text/html", maxWidth: 400, maxHeight: 10));

        Assert.False(output.IsError);
        Assert.Contains("<svg width=\"800\"", output.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ACancelledCell_StopsBeforeItDraws()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAsync<TaskCanceledException>(() => new FigureFormatter()
            .FormatAsync(ASimpleFigure(), new StubFormatterContext("text/html", cancellationToken: cancelled.Token)));
    }

    // ---- what the host reads off it ------------------------------------------------------------------------

    [Fact]
    public void ItNamesItselfTheWayTheExtensionsPanelExpects()
    {
        var formatter = new FigureFormatter();

        Assert.False(string.IsNullOrWhiteSpace(formatter.ExtensionId));
        Assert.False(string.IsNullOrWhiteSpace(formatter.Name));
        Assert.False(string.IsNullOrWhiteSpace(formatter.Version));
        Assert.False(string.IsNullOrWhiteSpace(formatter.Author));
        Assert.False(string.IsNullOrWhiteSpace(formatter.Description));
    }

    [Fact]
    public void TheVersionTheExtensionsPanelShows_IsTheOneTheBuildStamped()
    {
        // The formatter reads this straight off the assembly with no fallback behind it, so the stamp being
        // there is a real condition rather than an assumption. It is asserted here instead of being papered
        // over with a branch that no test could reach.
        var stamped = typeof(FigureFormatter).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

        Assert.NotNull(stamped);
        Assert.Equal(stamped.InformationalVersion.Split('+')[0], new FigureFormatter().Version);
        Assert.DoesNotContain('+', new FigureFormatter().Version);
    }

    [Fact]
    public async Task ItIsDiscoverableAndHasNothingToDoOnLoadOrUnload()
    {
        // The attribute IS the registration: without it the host never finds the class, whatever it implements.
        Assert.NotNull(typeof(FigureFormatter).GetCustomAttributes(typeof(VersoExtensionAttribute), false).SingleOrDefault());
        Assert.NotNull(typeof(FigureFormatter).GetConstructor(Type.EmptyTypes));

        var formatter = new FigureFormatter();
        await formatter.OnLoadedAsync(null!);
        await formatter.OnUnloadedAsync();
    }
}
