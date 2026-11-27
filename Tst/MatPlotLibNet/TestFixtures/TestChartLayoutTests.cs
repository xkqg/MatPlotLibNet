// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests;

/// <summary>The layout every test double of <see cref="IChartLayout"/> derives from states what a host has to state and leaves the
/// interface's defaults to the interface.</summary>
public sealed class TestChartLayoutTests
{
    private static readonly Rect Plot = new(10, 20, 200, 100);
    private static readonly DataRange Range = new(0, 10, 0, 100);

    [Fact]
    public void TheBase_DeclaresTheMembersNoDefaultBodyCovers_AndNoMemberTheInterfaceGivesABody()
    {
        static string Signature(MethodInfo m) => $"{m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))})";
        var withoutBody = typeof(IChartLayout).GetMethods().Where(m => m.IsAbstract).Select(Signature).Order().ToList();
        var declared = typeof(TestChartLayout)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName).Select(Signature).Order().ToList();

        Assert.Equal(withoutBody.Where(s => !s.StartsWith("get_", StringComparison.Ordinal)), declared);
    }

    [Fact]
    public void ThePixelRead_AndTheSecondaryRanges_AreTheInterfacesOwnDefaults()
    {
        IChartLayout layout = new TestChartLayout(Plot, Range);

        Assert.Empty(layout.GetSecondaryDataRanges(0));
        Assert.Null(layout.PixelToData(5, 50, 0));
        Assert.Equal(5.0, layout.PixelToData(110, 70, 0)!.Value.DataX, 6);
    }
}
