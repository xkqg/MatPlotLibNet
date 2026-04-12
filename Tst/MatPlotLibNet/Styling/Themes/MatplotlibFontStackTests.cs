// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling.Themes;

namespace MatPlotLibNet.Tests.Styling.Themes;

/// <summary>Verifies the <see cref="MatplotlibFontStack"/> internal record struct.</summary>
public class MatplotlibFontStackTests
{
    [Fact]
    public void MatplotlibFontStack_StoresAllFields()
    {
        var stack = new MatplotlibFontStack("DejaVu Sans, sans-serif", 10.0, 9.0, 12.0);

        Assert.Equal("DejaVu Sans, sans-serif", stack.PrimaryFamily);
        Assert.Equal(10.0, stack.BaseSize);
        Assert.Equal(9.0, stack.TickSize);
        Assert.Equal(12.0, stack.TitleSize);
    }

    [Fact]
    public void MatplotlibFontStack_TwoEqualValuesAreEqual()
    {
        var a = new MatplotlibFontStack("DejaVu Sans", 10, 9, 12);
        var b = new MatplotlibFontStack("DejaVu Sans", 10, 9, 12);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void MatplotlibFontStack_DifferentValuesAreUnequal()
    {
        var a = new MatplotlibFontStack("DejaVu Sans", 10, 9, 12);
        var b = new MatplotlibFontStack("DejaVu Sans", 12, 9, 12);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void MatplotlibFontStack_IsValueType()
    {
        Assert.True(typeof(MatplotlibFontStack).IsValueType);
    }
}
