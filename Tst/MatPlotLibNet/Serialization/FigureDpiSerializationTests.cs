// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>The resolution of a figure travels in its JSON, and JSON that says nothing about it reads as a new figure's:
/// a raster export draws at the resolution, so a figure read from sparse JSON must not arrive with none.</summary>
public class FigureDpiSerializationTests
{
    private static readonly IChartSerializer Serializer = ChartServices.Serializer;

    [Fact]
    public void JsonWithoutADpi_ReadsAsTheDefaultResolution()
    {
        var figure = Serializer.FromJson("""{"width":400,"height":300,"subPlots":[{"series":[{"type":"line","xData":[1,2],"yData":[1,2]}]}]}""");

        Assert.Equal(96, figure.Dpi);
    }

    [Fact]
    public void JsonWithANullDpi_ReadsAsTheDefaultResolution()
    {
        var figure = Serializer.FromJson("""{"width":400,"height":300,"dpi":null,"subPlots":[]}""");

        Assert.Equal(96, figure.Dpi);
    }

    [Fact]
    public void ADpiInTheJson_IsKept()
    {
        var figure = Serializer.FromJson("""{"width":400,"height":300,"dpi":192,"subPlots":[]}""");

        Assert.Equal(192, figure.Dpi);
    }

    [Fact]
    public void ADpiOfZeroInTheJson_IsKept_ForTheExportToRefuse()
    {
        // Written, so said: the reader does not invent a different resolution than the document states.
        var figure = Serializer.FromJson("""{"width":400,"height":300,"dpi":0,"subPlots":[]}""");

        Assert.Equal(0, figure.Dpi);
    }

    [Fact]
    public void TheResolution_RoundTripsThroughJson()
    {
        var figure = Plt.Create().WithSize(400, 300).WithDpi(150).Plot([1.0, 2.0], [1.0, 2.0]).Build();

        var back = Serializer.FromJson(Serializer.ToJson(figure));

        Assert.Equal(150, back.Dpi);
    }

    [Fact]
    public void TheDefaultResolution_IsWrittenAsBefore()
    {
        var figure = Plt.Create().WithSize(400, 300).Plot([1.0, 2.0], [1.0, 2.0]).Build();

        Assert.Contains("\"dpi\":96", Serializer.ToJson(figure));
    }
}
