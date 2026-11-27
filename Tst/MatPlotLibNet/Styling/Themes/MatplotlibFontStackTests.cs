// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling.Themes;

namespace MatPlotLibNet.Tests.Styling.Themes;

/// <summary>Verifies the <see cref="Styling.Themes.MatplotlibThemeFactory.MatplotlibFontStack"/> internal record struct.</summary>
public class MatplotlibFontStackTests
{
    [Fact]
    public void MatplotlibFontStack_TwoEqualValuesAreEqual()
    {
        var a = new MatplotlibThemeFactory.MatplotlibFontStack("DejaVu Sans", 10, 1.2);
        var b = new MatplotlibThemeFactory.MatplotlibFontStack("DejaVu Sans", 10, 1.2);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void MatplotlibFontStack_DifferentValuesAreUnequal()
    {
        var a = new MatplotlibThemeFactory.MatplotlibFontStack("DejaVu Sans", 10, 1.2);
        var b = new MatplotlibThemeFactory.MatplotlibFontStack("DejaVu Sans", 12, 1.2);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void MatplotlibFontStack_IsValueType()
    {
        Assert.True(typeof(MatplotlibThemeFactory.MatplotlibFontStack).IsValueType);
    }
}
