// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Catalog of named <see cref="Color"/> constants. Equivalent to matplotlib's named color strings.</summary>
public static class Colors
{
    // --- Basic ---

    /// <summary>Red (255, 0, 0).</summary>
    public static Color Red => new(255, 0, 0);

    /// <summary>Green (0, 128, 0).</summary>
    public static Color Green => new(0, 128, 0);

    /// <summary>Blue (0, 0, 255).</summary>
    public static Color Blue => new(0, 0, 255);

    /// <summary>White (255, 255, 255).</summary>
    public static Color White => new(255, 255, 255);

    /// <summary>Black (0, 0, 0).</summary>
    public static Color Black => new(0, 0, 0);

    /// <summary>Yellow (255, 255, 0).</summary>
    public static Color Yellow => new(255, 255, 0);

    /// <summary>Cyan (0, 255, 255).</summary>
    public static Color Cyan => new(0, 255, 255);

    /// <summary>Magenta (255, 0, 255).</summary>
    public static Color Magenta => new(255, 0, 255);

    /// <summary>Orange (255, 165, 0).</summary>
    public static Color Orange => new(255, 165, 0);

    /// <summary>Gray (128, 128, 128).</summary>
    public static Color Gray => new(128, 128, 128);

    /// <summary>Light gray (211, 211, 211).</summary>
    public static Color LightGray => new(211, 211, 211);

    /// <summary>Dark gray (64, 64, 64).</summary>
    public static Color DarkGray => new(64, 64, 64);

    /// <summary>Fully transparent (0, 0, 0, 0).</summary>
    public static Color Transparent => new(0, 0, 0, 0);

    // --- Matplotlib Tab10 ---

    /// <summary>Matplotlib default blue (#1f77b4).</summary>
    public static Color Tab10Blue => new(0x1F, 0x77, 0xB4);

    /// <summary>Matplotlib default orange (#ff7f0e).</summary>
    public static Color Tab10Orange => new(0xFF, 0x7F, 0x0E);

    /// <summary>Matplotlib default green (#2ca02c).</summary>
    public static Color Tab10Green => new(0x2C, 0xA0, 0x2C);

    // --- Rendering defaults ---

    /// <summary>Default grid line color (#CCCCCC).</summary>
    public static Color GridGray => new(0xCC, 0xCC, 0xCC);

    /// <summary>Default 3D edge color (#666666).</summary>
    public static Color EdgeGray => new(0x66, 0x66, 0x66);

    /// <summary>Amber / warning color (#FFC107).</summary>
    public static Color Amber => new(0xFF, 0xC1, 0x07);

    /// <summary>Fibonacci retracement orange (#FF9800).</summary>
    public static Color FibonacciOrange => new(0xFF, 0x98, 0x00);
}
