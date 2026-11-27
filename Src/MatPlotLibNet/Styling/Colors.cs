// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Styling;

/// <summary>Catalog of named <see cref="Color"/> constants. Equivalent to matplotlib's named color strings.</summary>
public static class Colors
{
    // --- Basic ---

    /// <summary>Red (255, 0, 0).</summary>
    public static Color Red => Css4Colors.Red;

    /// <summary>Green (0, 128, 0).</summary>
    public static Color Green => Css4Colors.Green;

    /// <summary>Blue (0, 0, 255).</summary>
    public static Color Blue => Css4Colors.Blue;

    /// <summary>White (255, 255, 255).</summary>
    public static Color White => Css4Colors.White;

    /// <summary>Black (0, 0, 0).</summary>
    public static Color Black => Css4Colors.Black;

    /// <summary>Yellow (255, 255, 0).</summary>
    public static Color Yellow => Css4Colors.Yellow;

    /// <summary>Cyan (0, 255, 255).</summary>
    public static Color Cyan => Css4Colors.Cyan;

    /// <summary>Magenta (255, 0, 255).</summary>
    public static Color Magenta => Css4Colors.Magenta;

    /// <summary>Orange (255, 165, 0).</summary>
    public static Color Orange => Css4Colors.Orange;

    /// <summary>Gray (128, 128, 128).</summary>
    public static Color Gray => Css4Colors.Gray;

    /// <summary>Light gray (211, 211, 211).</summary>
    public static Color LightGray => Css4Colors.LightGray;

    /// <summary>Dark gray (64, 64, 64).</summary>
    public static Color DarkGray => new(64, 64, 64);

    /// <summary>Fully transparent (0, 0, 0, 0).</summary>
    public static Color Transparent => new(0, 0, 0, 0);

    // --- Matplotlib Tab10: the ten colours of the default cycle, read from the one list that writes them ---

    /// <summary>Matplotlib default blue (#1f77b4).</summary>
    public static Color Tab10Blue => QualitativeColorMaps.Tab10At(0);

    /// <summary>Matplotlib default orange (#ff7f0e).</summary>
    public static Color Tab10Orange => QualitativeColorMaps.Tab10At(1);

    /// <summary>Matplotlib default green (#2ca02c).</summary>
    public static Color Tab10Green => QualitativeColorMaps.Tab10At(2);

    /// <summary>Matplotlib default red (#d62728).</summary>
    public static Color Tab10Red => QualitativeColorMaps.Tab10At(3);

    /// <summary>Matplotlib default purple (#9467bd).</summary>
    public static Color Tab10Purple => QualitativeColorMaps.Tab10At(4);

    /// <summary>Matplotlib default brown (#8c564b).</summary>
    public static Color Tab10Brown => QualitativeColorMaps.Tab10At(5);

    /// <summary>Matplotlib default pink (#e377c2).</summary>
    public static Color Tab10Pink => QualitativeColorMaps.Tab10At(6);

    /// <summary>Matplotlib default gray (#7f7f7f).</summary>
    public static Color Tab10Gray => QualitativeColorMaps.Tab10At(7);

    /// <summary>Matplotlib default olive (#bcbd22).</summary>
    public static Color Tab10Olive => QualitativeColorMaps.Tab10At(8);

    /// <summary>Matplotlib default cyan (#17becf).</summary>
    public static Color Tab10Cyan => QualitativeColorMaps.Tab10At(9);

    // --- CSS4 aliases (most-used names, full catalog in Css4Colors) ---

    /// <summary>Alice blue (#F0F8FF). Full catalog: <see cref="Css4Colors"/>.</summary>
    public static Color AliceBlue      => Css4Colors.AliceBlue;
    /// <summary>Cornflower blue (#6495ED).</summary>
    public static Color CornflowerBlue => Css4Colors.CornflowerBlue;
    /// <summary>Steel blue (#4682B4).</summary>
    public static Color SteelBlue      => Css4Colors.SteelBlue;
    /// <summary>Royal blue (#4169E1).</summary>
    public static Color RoyalBlue      => Css4Colors.RoyalBlue;
    /// <summary>Navy (#000080).</summary>
    public static Color Navy           => Css4Colors.Navy;
    /// <summary>Crimson (#DC143C).</summary>
    public static Color Crimson        => Css4Colors.Crimson;
    /// <summary>Tomato (#FF6347).</summary>
    public static Color Tomato         => Css4Colors.Tomato;
    /// <summary>Gold (#FFD700).</summary>
    public static Color Gold           => Css4Colors.Gold;
    /// <summary>Forest green (#228B22).</summary>
    public static Color ForestGreen    => Css4Colors.ForestGreen;
    /// <summary>Teal (#008080).</summary>
    public static Color Teal           => Css4Colors.Teal;
    /// <summary>Indigo (#4B0082).</summary>
    public static Color Indigo         => Css4Colors.Indigo;
    /// <summary>Violet (#EE82EE).</summary>
    public static Color Violet         => Css4Colors.Violet;
    /// <summary>Orchid (#DA70D6).</summary>
    public static Color Orchid         => Css4Colors.Orchid;
    /// <summary>Salmon (#FA8072).</summary>
    public static Color Salmon         => Css4Colors.Salmon;
    /// <summary>Coral (#FF7F50).</summary>
    public static Color Coral          => Css4Colors.Coral;
    /// <summary>Khaki (#F0E68C).</summary>
    public static Color Khaki          => Css4Colors.Khaki;
    /// <summary>Silver (#C0C0C0).</summary>
    public static Color Silver         => Css4Colors.Silver;
    /// <summary>Midnight blue (#191970).</summary>
    public static Color MidnightBlue   => Css4Colors.MidnightBlue;
    /// <summary>Rebecca purple (#663399).</summary>
    public static Color RebeccaPurple  => Css4Colors.RebeccaPurple;
    /// <summary>Chocolate (#D2691E).</summary>
    public static Color Chocolate      => Css4Colors.Chocolate;

    // --- Rendering defaults ---

    /// <summary>Default grid line color (#CCCCCC).</summary>
    public static Color GridGray => new(0xCC, 0xCC, 0xCC);

    /// <summary>Default 3D edge color (#666666).</summary>
    public static Color EdgeGray => new(0x66, 0x66, 0x66);

    /// <summary>Amber / warning color (#FFC107).</summary>
    public static Color Amber => new(0xFF, 0xC1, 0x07);

    /// <summary>Fibonacci retracement orange (#FF9800).</summary>
    public static Color FibonacciOrange => new(0xFF, 0x98, 0x00);

    /// <summary>The drop shadow the library lays under a shape that asks for one (a legend frame, a pie slice): black at 80 of 255.</summary>
    internal static Color DropShadow => new(0, 0, 0, 80);
}
