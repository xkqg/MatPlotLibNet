// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>The resolution of a figure travels in its JSON, and JSON that says nothing about it reads as a new figure's:
/// a raster export draws at the resolution, so a figure read from sparse JSON must not arrive with none.</summary>
public class FigureDpiSerializationTests
{
    private static readonly IChartSerializer Serializer = ChartSerializerDefault.Current;

    /// <summary>No dpi, or a null one, reads as the default resolution; a dpi in the JSON is kept, and so is a dpi of zero (written, so said: the
    /// reader does not invent a different resolution than the document states, and the export refuses it).</summary>
    [Theory]
    [InlineData("""{"width":400,"height":300,"subPlots":[{"series":[{"type":"line","xData":[1,2],"yData":[1,2]}]}]}""", 96)]
    [InlineData("""{"width":400,"height":300,"dpi":null,"subPlots":[]}""", 96)]
    [InlineData("""{"width":400,"height":300,"dpi":192,"subPlots":[]}""", 192)]
    [InlineData("""{"width":400,"height":300,"dpi":0,"subPlots":[]}""", 0)]
    public void TheDpiOfTheJson_IsRead(string json, double expected)
    {
        var figure = Serializer.FromJson(json);

        Assert.Equal(expected, figure.Dpi);
    }

    [Fact]
    public void TheResolution_RoundTripsThroughJson()
    {
        var figure = Plt.Create().WithSize(400, 300).WithDpi(150).Plot([1.0, 2.0], [1.0, 2.0]).Build();

        var back = figure.RoundTripped();

        Assert.Equal(150, back.Dpi);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("TheDefaultResolution_IsWrittenAsBefore", () =>
        {
            var figure = Plt.Create().WithSize(400, 300).Plot([1.0, 2.0], [1.0, 2.0]).Build();
            return Serializer.ToJson(figure);
        }, "\"dpi\":96"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
