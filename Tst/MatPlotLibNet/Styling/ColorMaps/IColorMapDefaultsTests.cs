// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Styling.ColorMapNormalizers;

/// <summary>Exercises the default interface methods on <see cref="IColorMap"/>
/// (currently 0% line coverage because no minimal implementor in the suite touches
/// the defaults). A bespoke <see cref="MinimalColorMap"/> exercises the three
/// default-returning-<c>null</c> methods so the lines are counted.</summary>
public class IColorMapDefaultsTests
{
    private sealed class MinimalColorMap : IColorMap
    {
        public string Name => "minimal";
        public Color GetColor(double value) => new(0, 0, 0);
        // Don't override GetUnderColor / GetOverColor / GetBadColor → defaults apply
    }

    [Fact]
    public void DefaultGetUnderColor_IsNull()
        => Assert.Null(((IColorMap)new MinimalColorMap()).GetUnderColor());

    [Fact]
    public void DefaultGetOverColor_IsNull()
        => Assert.Null(((IColorMap)new MinimalColorMap()).GetOverColor());

    [Fact]
    public void DefaultGetBadColor_IsNull()
        => Assert.Null(((IColorMap)new MinimalColorMap()).GetBadColor());
}
