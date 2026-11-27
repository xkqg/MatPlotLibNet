// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>What a figure-level verb asks for is kept as one step on the figure and taken again at every
/// <see cref="FigureBuilder.Build"/>, in the order of the calls: settings, texts, images and insets alike. The builder states
/// no default of its own, so a figure it builds with nothing asked for is the figure <c>new Figure()</c> is.</summary>
public class FigureBuilderReplayTests
{
    private const BindingFlags Instance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    [Fact]
    public void TheFigureLevelSteps_AreOneListOfActionsOnTheFigure()
    {
        var lists = typeof(FigureBuilder).GetFields(Instance).Count(f => f.FieldType == typeof(List<Action<Figure>>));
        var pending = typeof(FigureBuilder).GetNestedTypes(BindingFlags.NonPublic).Where(t => t.Name.StartsWith("Pending")).Select(t => t.Name);

        Assert.Equal(1, lists);
        Assert.Empty(pending);
    }

    [Fact]
    public void TheBuilder_RestatesNoDefaultOfTheFigure()
    {
        var restated = typeof(FigureBuilder).GetFields(Instance)
            .Where(f => f.FieldType == typeof(double) || f.FieldType == typeof(bool) || f.FieldType == typeof(InteractionTheme))
            .Select(f => f.Name);

        Assert.Empty(restated);
    }

    [Fact]
    public void AFigureWithNothingAskedFor_IsTheFigureItsOwnDefaultsMake()
    {
        var built = Plt.Create().Build();
        var bare = new Figure();

        Assert.Equivalent(new { bare.Width, bare.Height, bare.Dpi, bare.ResponsiveSvg, bare.InteractionTheme, bare.Spacing }, built);
        Assert.False(built.HasInteractivity);
        Assert.Null(built.FigureColorBar);
        Assert.Empty(built.Legends);
    }

    [Fact]
    public void Settings_AreTakenInTheOrderOfTheCalls()
    {
        var allThenOne = Plt.Create().WithBrowserInteraction().WithZoomPan(false).Build();
        var oneThenAll = Plt.Create().WithZoomPan(false).WithBrowserInteraction().Build();
        var sizedTwice = Plt.Create().WithSize(300, 200).WithSize(500, 400).Build();

        Assert.False(allThenOne.EnableZoomPan);
        Assert.True(allThenOne.EnableSankeyHover);
        Assert.True(oneThenAll.EnableZoomPan);
        Assert.Equal(500, sizedTwice.Width);
        Assert.Equal(400, sizedTwice.Height);
    }

    [Fact]
    public void EverySetting_ReachesEachBuild()
    {
        var builder = Plt.Create()
            .WithTitle("t").WithAltText("a").WithDescription("d").WithSize(640, 480).WithDpi(192)
            .WithBackground(Colors.Red).WithTheme(Theme.Dark).WithGridSpec(2, 1)
            .WithLegendToggle().WithRichTooltips().WithHighlight().WithSelection().With3DRotation()
            .WithTreemapDrilldown().WithSankeyHover().WithResponsiveSvg(false)
            .WithInteractionTheme(InteractionTheme.Default with { HighlightOpacity = 0.5 })
            .LinkSelection("rows", ["a"]);

        foreach (var figure in new[] { builder.Build(), builder.Build() })
        {
            Assert.Equal("t", figure.Title);
            Assert.Equal("a", figure.AltText);
            Assert.Equal("d", figure.Description);
            Assert.Equal(640, figure.Width);
            Assert.Equal(480, figure.Height);
            Assert.Equal(192, figure.Dpi);
            Assert.Equal(Colors.Red, figure.BackgroundColor);
            Assert.Same(Theme.Dark, figure.Theme);
            Assert.Equal(2, figure.GridSpec!.Rows);
            Assert.True(figure.EnableLegendToggle && figure.EnableRichTooltips && figure.EnableHighlight && figure.EnableSelection);
            Assert.True(figure.Enable3DRotation && figure.EnableTreemapDrilldown && figure.EnableSankeyHover);
            Assert.False(figure.ResponsiveSvg);
            Assert.Equal(0.5, figure.InteractionTheme.HighlightOpacity);
            Assert.Equal("rows", figure.SelectionGroup);
            Assert.Equal(["a"], figure.SelectedRows!);
        }
    }

    [Fact]
    public void ServerInteraction_TakesItsCallbackOnce_AndSwitchesOnWhatItAskedFor()
    {
        int calls = 0;
        var builder = Plt.Create().WithServerInteraction("c1", s => { calls++; s.EnableZoom().EnableLegendToggle().EnableDataCursor(); });

        var first = builder.Build();
        var second = builder.Build();

        Assert.Equal(1, calls);
        foreach (var figure in new[] { first, second })
        {
            Assert.Equal("c1", figure.ChartId);
            Assert.True(figure.ServerInteraction);
            Assert.True(figure.EnableZoomPan);
            Assert.True(figure.EnableLegendToggle);
            Assert.True(figure.EnableDataCursor);
            Assert.False(figure.EnableSelection);
            Assert.False(figure.EnableRichTooltips);
            Assert.False(figure.EnableViewerCursors);
        }
    }

    [Fact]
    public void ACallbackThatShapesASetting_IsTakenOnceAtTheCall()
    {
        int legend = 0, spacing = 0, colorBar = 0;
        var builder = Plt.Create()
            .FigLegend(l => { legend++; return l with { NCols = 3 }; })
            .TightLayout()
            .WithSubPlotSpacing(s => { spacing++; return s with { HorizontalGap = 7 }; })
            .WithSubPlotSpacing()
            .WithColorBar(c => { colorBar++; return c with { Label = "v" }; })
            .WithColorBar(c => c with { Shrink = 0.5 });

        Assert.Equal((1, 1, 1), (legend, spacing, colorBar));
        foreach (var figure in new[] { builder.Build(), builder.Build() })
        {
            Assert.Equal(3, Assert.Single(figure.Legends).NCols);
            Assert.True(figure.Spacing.TightLayout);
            Assert.Equal(7, figure.Spacing.HorizontalGap);
            Assert.True(figure.FigureColorBar!.Visible);
            Assert.Equal("v", figure.FigureColorBar.Label);
            Assert.Equal(0.5, figure.FigureColorBar.Shrink);
        }
        Assert.Equal((1, 1, 1), (legend, spacing, colorBar));
    }

    [Fact]
    public void TextsImagesAndInsets_AreNewInEachBuild()
    {
        var builder = Plt.Create()
            .FigText("note", 0.1, 0.2, t => t.Color = Colors.Blue)
            .FigImage(new double[1, 1], 3, 4)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0], [1.0, 2.0]))
            .AddInset(0, new InsetBounds(0.1, 0.1, 0.3, 0.3), inset => inset.Title = "zoom")
            .AddInset(5, 0.1, 0.1, 0.3, 0.3)
            .AddInset(-1, 0.1, 0.1, 0.3, 0.3);

        var first = builder.Build();
        var second = builder.Build();

        Assert.NotSame(first.Texts[0], second.Texts[0]);
        Assert.NotSame(first.Images[0], second.Images[0]);
        Assert.NotSame(first.SubPlots[0].Insets[0], second.SubPlots[0].Insets[0]);
        Assert.Equal(Colors.Blue, second.Texts[0].Color);
        Assert.Equal((3.0, 4.0), (second.Images[0].XOffset, second.Images[0].YOffset));
        Assert.Single(second.SubPlots[0].Insets);
        Assert.Equal("zoom", second.SubPlots[0].Insets[0].Title);
        Assert.Equal(new InsetBounds(0.1, 0.1, 0.3, 0.3), second.SubPlots[0].Insets[0].InsetBounds);
    }

    [Fact]
    public void ABlankSelectedRow_IsRefusedWhenTheFigureIsBuilt()
    {
        var builder = Plt.Create().LinkSelection("rows", [" "]);

        Assert.Throws<ArgumentException>(() => builder.Build());
    }
}
