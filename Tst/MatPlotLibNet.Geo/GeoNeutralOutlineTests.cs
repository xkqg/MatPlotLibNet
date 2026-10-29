// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Geo.Series;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>The outline of a country border, a choropleth and a cartogram wears the theme's neutral grey, as every other reference
/// graphic of a chart does, instead of a #808080 fixed when the verb ran: a map drawn under Nord, whose plot #808080 stands
/// at 2.55 : 1 against, has an outline that can be seen. A colour the caller names wins.</summary>
[Collection("ChartSerializerGlobalState")]
public class GeoNeutralOutlineTests
{
    private static readonly ChartSerializer Serializer = new();
    private static readonly IReadOnlyDictionary<string, double> Values = new Dictionary<string, double> { ["FRA"] = 1, ["DEU"] = 2, ["ESP"] = 4 };

    public static TheoryData<string> Layers => ["borders", "choropleth", "cartogram"];

    private static void Add(string layer, AxesBuilder ax)
    {
        ax.WithProjection(GeoProjection.PlateCarree);
        switch (layer)
        {
            case "borders": ax.Borders(); break;
            case "choropleth": ax.Choropleth(Values); break;
            default: ax.Cartogram(Values); break;
        }
    }

    private static Figure Map(string layer, Theme theme) =>
        Plt.Create().WithSize(600, 330).WithTheme(theme).AddSubPlot(1, 1, 1, ax => Add(layer, ax)).Build();

    private static string Stroke(string hex) => $"stroke=\"{hex}\"";

    /// <summary>Measured before the change: under Nord each of these outlined its regions in #808080.</summary>
    [Theory]
    [MemberData(nameof(Layers))]
    public void AnOutline_WearsTheNeutralOfTheTheme_UnderNord(string layer)
    {
        string svg = Map(layer, Theme.Nord).ToSvg();

        Assert.Contains(Stroke("#8D8D8D"), svg);
        Assert.DoesNotContain(Stroke("#808080"), svg);
    }

    [Theory]
    [MemberData(nameof(Layers))]
    public void AnOutline_WearsTheLibrarysGrey_UnderTheDefaultTheme(string layer) =>
        Assert.Contains(Stroke("#808080"), Map(layer, Theme.Default).ToSvg());

    [Fact]
    public void ANamedOutlineColour_WinsOverTheNeutral()
    {
        string svg = Plt.Create().WithSize(600, 330).WithTheme(Theme.Nord)
            .AddSubPlot(1, 1, 1, ax => ax.WithProjection(GeoProjection.PlateCarree).Borders(Colors.Red)).Build().ToSvg();

        Assert.Contains(Stroke("#FF0000"), svg);
        Assert.DoesNotContain(Stroke("#8D8D8D"), svg);
    }

    [Theory]
    [MemberData(nameof(Layers))]
    public void AnOutline_NamesNoColour_AndWearsTheNeutralRole(string layer)
    {
        var figure = Map(layer, Theme.Default);

        Assert.Null(figure.SubPlots[0].Series.OfType<GeoPolygonSeries>().Single().StrokeColor);
        Assert.Contains("\"wearsNeutral\":true", figure.ToJson());
    }

    /// <summary>The role is written, not a colour, so a map read back and drawn under another theme wears that theme's grey.</summary>
    [Fact]
    public void TheRole_SurvivesTheJson_AndFollowsTheTheme()
    {
        var figure = Map("borders", Theme.Default);

        var read = Serializer.FromJson(figure.ToJson());
        read.Theme = Theme.Nord;

        Assert.Contains("\"wearsNeutral\":true", figure.ToJson());
        Assert.Contains(Stroke("#8D8D8D"), read.ToSvg());
    }

    /// <summary>A series wearing the neutral holds no colour of the cycle, so the layer drawn after the borders wears the theme's
    /// first colour and not its second.</summary>
    [Fact]
    public void TheBordersTakeNoColourSlot_SoThePointsOnTopWearTheFirstColour()
    {
        var figure = Plt.Create().WithSize(600, 330).AddSubPlot(1, 1, 1, ax => ax
            .WithProjection(GeoProjection.PlateCarree).Borders().Points([48.85], [2.35])).Build();

        string svg = figure.ToSvg();

        Assert.Contains($"fill=\"{figure.Theme.CycleColors[0].ToHex()}\"", svg);
        Assert.DoesNotContain($"fill=\"{figure.Theme.CycleColors[1].ToHex()}\"", svg);
    }

    /// <summary>A visitor that is not the SVG renderer has no theme to read the neutral from, so a series that wears the neutral
    /// draws its regions filled with no outline, not outlined in a colour nobody chose.</summary>
    [Fact]
    public void AVisitorWithNoTheme_GivesTheNeutralNothingToWear_SoTheRegionsHaveNoOutline()
    {
        var series = Map("choropleth", Theme.Default).SubPlots[0].Series.OfType<GeoPolygonSeries>().Single();
        var context = new SvgRenderContext();

        series.Accept(DispatchProxy.Create<ISeriesVisitor, SilentVisitor>(), new RenderArea(new Rect(0, 0, 600, 330), context));

        string markup = context.GetOutput();
        Assert.Contains("<path", markup);
        Assert.DoesNotContain("stroke=\"#", markup);
    }

    /// <summary>An <see cref="ISeriesVisitor"/> that does nothing and is not the SVG renderer.</summary>
    public class SilentVisitor : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => null;
    }
}
