// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Models;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>The verbs of the map layers (<c>GeoAxesExtensions</c>) have the shape every verb of the library has: an extension of
/// <see cref="AxesBuilder"/> that returns it, takes at most four parameters besides it, and ends with a <c>configure</c> callback
/// that can be left out. They live in the package's own namespace, since the package ships its own series. A layer that is refused
/// leaves the axes as it was, and a layer that draws existing series travels through JSON as those series.</summary>
[Collection("ChartSerializerGlobalState")]
public class GeoVerbShapeTests
{
    private const int ParameterBudget = 4;

    private static readonly ChartSerializer Serializer = new();

    private static MethodInfo[] Verbs =>
    [
        .. typeof(GeoAxesExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.IsDefined(typeof(ExtensionAttribute), inherit: false))
            .OrderBy(method => method.Name, StringComparer.Ordinal),
    ];

    public static TheoryData<int> VerbIndices => [.. Enumerable.Range(0, Verbs.Length)];

    [Fact]
    public void TheVerbsOfTheMapLayers_AreFound() =>
        Assert.Equal(
            ["Borders", "Bubbles", "Cartogram", "Choropleth", "Coastlines", "Dorling", "Flows", "HexBins", "Land", "Ocean", "Points", "WithProjection"],
            Verbs.Select(verb => verb.Name).Distinct());

    [Theory]
    [MemberData(nameof(VerbIndices))]
    public void EveryVerb_ExtendsTheAxesBuilder_AndReturnsIt(int index)
    {
        var verb = Verbs[index];

        Assert.Equal(typeof(AxesBuilder), verb.GetParameters()[0].ParameterType);
        Assert.Equal(typeof(AxesBuilder), verb.ReturnType);
        Assert.Equal("MatPlotLibNet.Geo", verb.DeclaringType!.Namespace);
    }

    [Theory]
    [MemberData(nameof(VerbIndices))]
    public void EveryVerb_TakesAtMostFourParametersBesidesTheBuilder(int index) =>
        Assert.True(Verbs[index].GetParameters().Length - 1 <= ParameterBudget, $"{Verbs[index].Name} takes too many parameters");

    [Theory]
    [MemberData(nameof(VerbIndices))]
    public void ACallback_IsNamedConfigure_IsLast_AndCanBeLeftOut(int index)
    {
        var parameters = Verbs[index].GetParameters();
        for (int i = 1; i < parameters.Length; i++)
        {
            bool callback = parameters[i].ParameterType.IsGenericType && parameters[i].ParameterType.GetGenericTypeDefinition() == typeof(Action<>);
            Assert.Equal(callback, parameters[i].Name == "configure");
            if (!callback) continue;
            Assert.Equal(parameters.Length - 1, i);
            Assert.True(parameters[i].HasDefaultValue && parameters[i].DefaultValue is null);
        }
    }

    // ---- a refused layer leaves the axes as it was --------------------------------------------------------------------------

    private static Figure Build(Action<AxesBuilder> layers) =>
        Plt.Create().WithSize(720, 360).AddSubPlot(1, 1, 1, ax =>
        {
            ax.WithProjection(GeoProjection.PlateCarree);
            layers(ax);
        }).Build();

    [Fact]
    public void ABubbleLayerWhoseValuesDoNotPairWithThePlaces_IsRefused_BeforeAnythingIsAdded()
    {
        var figure = Build(ax =>
        {
            Assert.Throws<ArgumentException>(() => ax.Bubbles([0.0, 1.0], [0.0, 1.0], [1.0]));
            Assert.Throws<ArgumentException>(() => ax.HexBins([0.0, 1.0], [0.0]));
        });

        Assert.Empty(figure.SubPlots[0].Series);
    }

    // ---- the layers that draw existing series travel as those series --------------------------------------------------------

    public static TheoryData<string> Layers => ["points", "bubbles", "hexbins", "flows"];

    [Theory]
    [MemberData(nameof(Layers))]
    public void ALayerOfExistingSeries_ComesBackFromJson_AsTheSamePicture(string layer)
    {
        var figure = Build(ax =>
        {
            ax.Ocean(Colors.Blue);
            switch (layer)
            {
                case "points": ax.Points([10.0, 20.0, 30.0], [10.0, 20.0, 30.0]); break;
                case "bubbles": ax.Bubbles([10.0, 20.0, 30.0], [10.0, 20.0, 30.0], [1.0, 2.0, 3.0]); break;
                case "hexbins": ax.HexBins([10.0, 20.0, 30.0, 12.0], [10.0, 20.0, 30.0, 11.0]); break;
                default: ax.Flows([new GeoFlow(new GeoCoordinate(35.7, 139.7), new GeoCoordinate(34, -118.2), 2), new GeoFlow(new GeoCoordinate(52, 5), new GeoCoordinate(-33, 151), 1)]); break;
            }
        });

        Assert.Equal(figure.ToSvg(), Serializer.FromJson(Serializer.ToJson(figure)).ToSvg());
    }
}
