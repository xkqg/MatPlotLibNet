// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>The fields of a figure record, and the kinds of a described family, as the library's serializer reads them: its
/// naming policy, its attributes and the internal members it carries. The schema the server describes, the seats its reader
/// walks and the vocabulary it checks enum values by all read the records through here, so none of them holds a second copy
/// of a wire name.</summary>
public class WireContractExtensionsTests
{
    private static Type Record(string name) => typeof(SeriesDto).Assembly.GetType($"MatPlotLibNet.Serialization.{name}")!;

    [Fact]
    public void ARecordsFields_AreNamedAsTheSerializerWritesThem()
    {
        var names = Record("AxisDto").WireFields.Select(field => field.Name).ToArray();

        Assert.Equal("label", names[0]);
        Assert.Contains("tickLabelColor", names);
        Assert.Contains("symLogLinThresh", names);
    }

    [Fact]
    public void AFieldWithAWireNameOfItsOwn_IsNamedByIt()
    {
        var names = typeof(WaterSampleDto).WireFields.Select(field => field.Name).ToArray();

        Assert.Contains("hco3", names);
        Assert.Contains("co3", names);
        Assert.Contains("so4", names);
        Assert.DoesNotContain("hcO3", names);
    }

    [Fact]
    public void AnInternalMemberTheWireCarries_IsAField()
    {
        var names = typeof(SeriesDto).WireFields.Select(field => field.Name).ToArray();

        Assert.Contains("normalizer", names);
        Assert.Contains("colorMap", names);
    }

    [Fact]
    public void TheFieldsALayerDeclares_LeaveOutTheLayersAboveIt()
    {
        var filled = Record("FilledPatchDto");

        var declared = filled.DeclaredWireFields().Select(field => field.Name).ToArray();
        var all = filled.WireFields.Select(field => field.Name).ToArray();

        Assert.Equal(["faceColor"], declared);
        Assert.Contains("edgeColor", all);
    }

    [Fact]
    public void AFamilysKinds_AreTheOnesTheSerializerReadsTheTypeFieldBy()
    {
        var kinds = Record("PatchDto").WireKinds.Select(kind => (string)kind.TypeDiscriminator!).ToArray();

        Assert.Equal("rectangle", kinds[0]);
        Assert.Contains("polygon", kinds);
        Assert.Contains("arc", kinds);
    }
}
