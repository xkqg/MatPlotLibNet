// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json.Serialization;
using static MatPlotLibNet.Mcp.Tests.SpecFixtures;

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>A family of kinds the figure JSON names (the four links of an axis, the seven patches) is held by the cases and by the schema
/// the spec reader describes to an agent: a case for every kind the wire family declares (a new kind turns this red until a case stands for
/// it), and no kind in the schema that the family does not declare.</summary>
public class SpecKindFamiliesTests
{
    /// <summary>Each family: the wire record that declares its kinds, and the kinds the cases stand for.</summary>
    public static TheoryData<Type, IEnumerable<string>> Declarations => new()
    {
        { SpecLinkedAxisTests.TransformRecord(), SpecLinkedAxisTests.CaseKinds },
        { SpecPatchesTests.PatchRecord(), SpecPatchesTests.CaseKinds },
    };

    /// <summary>Each family: the kinds the cases stand for, and the schema section that lists them.</summary>
    public static TheoryData<IEnumerable<string>, string> Sections => new()
    {
        { SpecLinkedAxisTests.CaseKinds, "Axis link kinds" },
        { SpecPatchesTests.CaseKinds, "Patch kinds" },
    };

    [Theory]
    [MemberData(nameof(Declarations))]
    public void TheCases_StandForEveryKindTheFamilyDeclares(Type record, IEnumerable<string> caseKinds)
    {
        var declared = record.GetCustomAttributes<JsonDerivedTypeAttribute>().Select(derived => (string)derived.TypeDiscriminator!);

        Assert.Equal(declared.Order(StringComparer.Ordinal), caseKinds.Order(StringComparer.Ordinal));
    }

    [Theory]
    [MemberData(nameof(Sections))]
    public void TheSchema_ListsNoKindTheFamilyDoesNotDeclare(IEnumerable<string> caseKinds, string section)
    {
        var kinds = Description.SectionLines(section).Where(line => !line.StartsWith("    ", StringComparison.Ordinal)).Select(line => line.Trim().Split(' ')[0]);

        Assert.Equal(caseKinds.Order(StringComparer.Ordinal), kinds.Order(StringComparer.Ordinal));
    }
}
