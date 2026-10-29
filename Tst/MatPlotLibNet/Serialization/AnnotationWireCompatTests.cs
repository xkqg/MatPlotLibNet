// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>
/// An annotation's pixel offset and vertical alignment join the JSON without touching what was written before them: a
/// figure that has neither writes the bytes it always wrote, a document written before they existed reads and writes
/// back unchanged, and a document that has them reads them back.
/// </summary>
[Collection("ChartSerializerGlobalState")]
public class AnnotationWireCompatTests
{
    private static readonly ChartSerializer Serializer = new();

    /// <summary>One annotation at its defaults, as the serializer wrote it before the offset existed.</summary>
    private const string WrittenBeforeTheOffset =
        """{"width":400,"height":300,"dpi":96,"subPlots":[{"xAxis":{"scale":"linear"},"yAxis":{"scale":"linear"},"zAxis":{"scale":"linear"},"grid":{"visible":false},"series":[{"type":"line","xData":[0,10],"yData":[0,10],"lineStyle":"solid","lineWidth":1.5}],"annotations":[{"text":"peak","x":5,"y":5}]}]}""";

    private static Figure Figure(Action<Annotation>? configure = null) =>
        Plt.Create().WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 10.0], [0.0, 10.0]).Annotate("peak", 5, 5, configure))
            .Build();

    [Fact]
    public void AnAnnotationAtItsDefaults_WritesNeitherAnOffsetNorAVerticalAlignment()
    {
        string json = Serializer.ToJson(Figure());

        Assert.Equal(WrittenBeforeTheOffset, json);
        Assert.DoesNotContain("offset", json);
        Assert.DoesNotContain("verticalAlignment", json);
        Assert.DoesNotContain("avoidOverlap", json);
    }

    [Fact]
    public void JsonWrittenBeforeTheOffsetExisted_ReadsAtTheDefaults_AndWritesBackIdentical()
    {
        var read = Serializer.FromJson(WrittenBeforeTheOffset);

        var annotation = Assert.Single(read.SubPlots[0].Annotations);
        Assert.Equal(0, annotation.OffsetX);
        Assert.Equal(0, annotation.OffsetY);
        Assert.Equal(TextVerticalAlignment.Baseline, annotation.VerticalAlignment);
        Assert.Equal(WrittenBeforeTheOffset, Serializer.ToJson(read));
    }

    [Fact]
    public void AnOffsetAndAVerticalAlignment_AreWrittenByName_AndComeBack()
    {
        string json = Serializer.ToJson(Figure(a =>
        {
            a.OffsetX = 12;
            a.OffsetY = -8;
            a.VerticalAlignment = TextVerticalAlignment.Center;
        }));

        Assert.Contains("\"offsetX\":12", json);
        Assert.Contains("\"offsetY\":-8", json);
        Assert.Contains("\"verticalAlignment\":\"center\"", json);
        var annotation = Assert.Single(Serializer.FromJson(json).SubPlots[0].Annotations);
        Assert.Equal(12, annotation.OffsetX);
        Assert.Equal(-8, annotation.OffsetY);
        Assert.Equal(TextVerticalAlignment.Center, annotation.VerticalAlignment);
    }

    [Fact]
    public void AnAnnotationThatAsksToMove_IsWrittenByName_AndComesBack()
    {
        string json = Serializer.ToJson(Figure(a => a.AvoidOverlap = true));

        Assert.Contains("\"avoidOverlap\":true", json);
        Assert.True(Assert.Single(Serializer.FromJson(json).SubPlots[0].Annotations).AvoidOverlap);
    }
}
