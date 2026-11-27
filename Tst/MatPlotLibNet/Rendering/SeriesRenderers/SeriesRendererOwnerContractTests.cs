// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.XY;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Serialization;
using MatPlotLibNet.Tests.Packaging;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>The renderer base is one owner and one mechanism: a series' colour is asked of it by <c>ColorOf(series)</c> (which asks
/// the slot, the owner the legend asks too), and it declares no virtual member for a renderer to replace.</summary>
public class SeriesRendererOwnerContractTests
{
    private static readonly Rect Bounds = new(80, 60, 640, 480);

    private static SeriesRenderContext ContextOf(Color unnamed, Theme? theme = null) =>
        new(new DataTransform(0, 10, 0, 10, Bounds), new SvgRenderContext(), unnamed, new RenderArea(Bounds, new SvgRenderContext()))
        {
            Theme = theme ?? Theme.Default,
        };

    private sealed class Probe(SeriesRenderContext context) : SeriesRenderer<LineSeries>(context)
    {
        public override void Render(LineSeries series) { }
        public Color ColorOfSeries<TSeries>(TSeries series) where TSeries : ISeries => ColorOf(series);
    }

    [Fact]
    public void TheColourAseriesNames_IsTheColourItIsDrawnIn()
    {
        var probe = new Probe(ContextOf(Colors.Tab10Blue));

        Assert.Equal(Colors.Red, probe.ColorOfSeries(new LineSeries([1.0, 2.0], [1.0, 2.0]) { Color = Colors.Red }));
    }

    [Fact]
    public void ASeriesThatNamesNone_IsDrawnInTheColourOfItsSlot()
    {
        var probe = new Probe(ContextOf(Colors.Tab10Green));

        Assert.Equal(Colors.Tab10Green, probe.ColorOfSeries(new LineSeries([1.0, 2.0], [1.0, 2.0])));
    }

    [Fact]
    public void ASeriesThatNamesNone_WearsTheColourItsVerbChoseUnderTheLibrarysOwnThemes()
    {
        var series = new LineSeries([1.0, 2.0], [1.0, 2.0]) { VerbColor = Colors.Teal };
        var probe = new Probe(ContextOf(Colors.Tab10Blue, Theme.Default));

        Assert.Equal(Colors.Teal, probe.ColorOfSeries(series));
    }

    [Fact]
    public void ASeriesThatNamesNone_WearsTheColourOfItsSlotUnderTheMatplotlibThemes_NotTheVerbsChoice()
    {
        var series = new LineSeries([1.0, 2.0], [1.0, 2.0]) { VerbColor = Colors.Teal };
        var probe = new Probe(ContextOf(Colors.Tab10Green, Theme.MatplotlibV2));

        Assert.Equal(Colors.Tab10Green, probe.ColorOfSeries(series));
    }

    [Fact]
    public void AKindTheThemeColours_IsDrawnInThatColour_AsTheLegendShowsIt()
    {
        var violin = new ViolinSeries([[1.0, 2.0, 3.0, 4.0]]);
        var context = ContextOf(Colors.Tab10Blue, Theme.MatplotlibClassic);
        var probe = new Probe(context);

        Assert.Equal(Theme.MatplotlibClassic.ViolinBodyColor, probe.ColorOfSeries(violin));
        Assert.Equal(context.Slot.ColorOf(violin), probe.ColorOfSeries(violin));
    }

    [Fact]
    public void TheColourOfAseries_IsWhatItsSlotSays_ForEveryKindOfSeries()
    {
        var context = ContextOf(Colors.Tab10Orange, Theme.MatplotlibV2);
        var probe = new Probe(context);

        foreach (var type in DrawnSeriesFixtures.DrawnTypes)
        {
            var series = DrawnSeriesFixtures.Instance(type.Name);
            Assert.Equal(context.Slot.ColorOf(series), probe.ColorOfSeries(series));
        }
    }

    [Fact]
    public void TheRendererBase_DeclaresNoVirtualMember()
    {
        var virtuals = typeof(SeriesRenderer)
            .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .OfType<MethodBase>()
            .Where(m => m is MethodInfo { IsVirtual: true, IsFinal: false })
            .Select(m => m.Name);

        Assert.Empty(virtuals);
    }

    [Fact]
    public void NoRenderer_OverridesAMemberOfTheRendererBase()
    {
        var baseMembers = typeof(SeriesRenderer)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .ToHashSet();

        var overriding = typeof(SeriesRenderer).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(SeriesRenderer).IsAssignableFrom(t))
            .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(m => m.GetBaseDefinition().DeclaringType == typeof(SeriesRenderer) && m.GetBaseDefinition() != m && baseMembers.Contains(m.Name))
                .Select(m => $"{t.Name}.{m.Name}"));

        Assert.Empty(overriding);
    }

    [Fact]
    public void NoSource_SpellsTheColourEntryThatColorOfReplaced()
    {
        var spellings = RepositoryTree.SourceFiles()
            .SelectMany(file => file.CodeLines.Select((line, i) => (file.Path, Line: i + 1, Text: line)))
            .Where(l => Regex.IsMatch(l.Text, @"\bResolveColor\("))
            .Select(l => $"{l.Path}:{l.Line}: {l.Text.Trim()}");

        Assert.Empty(spellings);
    }
}
