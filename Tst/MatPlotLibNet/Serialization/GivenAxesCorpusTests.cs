// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>The bytes of a figure whose axes, figure settings and overlays have every drawn property GIVEN, pinned in one checked-in
/// file, as <see cref="GivenSeriesCorpusTests"/> pins the series. The axes-level tests read a value back; this holds the key, the
/// value and the order of every setting on the wire, so a step that moves a rule between the model, the DTO and the renderer proves
/// the JSON stayed identical by this passing without the file regenerated. The file lives under
/// <c>Serialization/Golden/GivenAxes</c>; <c>MPL_REGEN_GOLDEN=1</c> writes it.</summary>
[Collection("ChartSerializerGlobalState")]
public class GivenAxesCorpusTests
{
    private static readonly ChartSerializer Serializer = new();

    private const string Folder = "GivenAxes";

    private const string Name = "figure";

    private static string Json() => Serializer.ToJson(DrawnAxesFixtures.Build(everyPropertySet: true));

    [Fact]
    public void AFigureWhoseAxesHaveEveryPropertyGiven_WritesTheBytesPinned()
    {
        string json = Json();

        Assert.Equal(BytePins.Pinned(Folder, Name, json), json);
    }

    [Fact]
    public void TheBytesPinned_ReadBackAndWrittenAgain_AreTheSameBytes()
    {
        string pinned = BytePins.Pinned(Folder, Name, Json());

        Assert.Equal(pinned, Serializer.ToJson(Serializer.FromJson(pinned)));
    }

    [Fact]
    public void ThePinnedFiles_AreExactlyTheFigure()
    {
        Assert.Equal([Name], BytePins.Names(Folder));
    }

    [Fact]
    public void AnAxes_WithEveryPropertyGiven_WritesMoreThanAtRest()
    {
        string atRest = Serializer.ToJson(DrawnAxesFixtures.Build(everyPropertySet: false));

        Assert.True(Json().Length > atRest.Length * 2, $"at rest {atRest.Length} chars, given {Json().Length} chars");
    }
}
