// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;

namespace MatPlotLibNet.Styling;

/// <summary>All 148 CSS Level 4 named colors as static <see cref="Color"/> properties.</summary>
/// <remarks>Use <see cref="Color.FromName"/> for case-insensitive name lookup.</remarks>
public static class Css4Colors
{
    // --- Pinks ---
    /// <summary>Gets the <c>Pink</c> color (#FFC0CB).</summary>
    public static Color Pink             => new(0xFF, 0xC0, 0xCB);
    /// <summary>Gets the <c>LightPink</c> color (#FFB6C1).</summary>
    public static Color LightPink        => new(0xFF, 0xB6, 0xC1);
    /// <summary>Gets the <c>HotPink</c> color (#FF69B4).</summary>
    public static Color HotPink          => new(0xFF, 0x69, 0xB4);
    /// <summary>Gets the <c>DeepPink</c> color (#FF1493).</summary>
    public static Color DeepPink         => new(0xFF, 0x14, 0x93);
    /// <summary>Gets the <c>PaleVioletRed</c> color (#DB7093).</summary>
    public static Color PaleVioletRed    => new(0xDB, 0x70, 0x93);
    /// <summary>Gets the <c>MediumVioletRed</c> color (#C71585).</summary>
    public static Color MediumVioletRed  => new(0xC7, 0x15, 0x85);

    // --- Reds ---
    /// <summary>Gets the <c>LightSalmon</c> color (#FFA07A).</summary>
    public static Color LightSalmon  => new(0xFF, 0xA0, 0x7A);
    /// <summary>Gets the <c>Salmon</c> color (#FA8072).</summary>
    public static Color Salmon       => new(0xFA, 0x80, 0x72);
    /// <summary>Gets the <c>DarkSalmon</c> color (#E9967A).</summary>
    public static Color DarkSalmon   => new(0xE9, 0x96, 0x7A);
    /// <summary>Gets the <c>LightCoral</c> color (#F08080).</summary>
    public static Color LightCoral   => new(0xF0, 0x80, 0x80);
    /// <summary>Gets the <c>IndianRed</c> color (#CD5C5C).</summary>
    public static Color IndianRed    => new(0xCD, 0x5C, 0x5C);
    /// <summary>Gets the <c>Crimson</c> color (#DC143C).</summary>
    public static Color Crimson      => new(0xDC, 0x14, 0x3C);
    /// <summary>Gets the <c>FireBrick</c> color (#B22222).</summary>
    public static Color FireBrick    => new(0xB2, 0x22, 0x22);
    /// <summary>Gets the <c>DarkRed</c> color (#8B0000).</summary>
    public static Color DarkRed      => new(0x8B, 0x00, 0x00);
    /// <summary>Gets the <c>Red</c> color (#FF0000).</summary>
    public static Color Red          => new(0xFF, 0x00, 0x00);

    // --- Oranges ---
    /// <summary>Gets the <c>OrangeRed</c> color (#FF4500).</summary>
    public static Color OrangeRed   => new(0xFF, 0x45, 0x00);
    /// <summary>Gets the <c>Tomato</c> color (#FF6347).</summary>
    public static Color Tomato      => new(0xFF, 0x63, 0x47);
    /// <summary>Gets the <c>Coral</c> color (#FF7F50).</summary>
    public static Color Coral       => new(0xFF, 0x7F, 0x50);
    /// <summary>Gets the <c>DarkOrange</c> color (#FF8C00).</summary>
    public static Color DarkOrange  => new(0xFF, 0x8C, 0x00);
    /// <summary>Gets the <c>Orange</c> color (#FFA500).</summary>
    public static Color Orange      => new(0xFF, 0xA5, 0x00);

    // --- Yellows ---
    /// <summary>Gets the <c>Yellow</c> color (#FFFF00).</summary>
    public static Color Yellow               => new(0xFF, 0xFF, 0x00);
    /// <summary>Gets the <c>LightYellow</c> color (#FFFFE0).</summary>
    public static Color LightYellow          => new(0xFF, 0xFF, 0xE0);
    /// <summary>Gets the <c>LemonChiffon</c> color (#FFFACD).</summary>
    public static Color LemonChiffon         => new(0xFF, 0xFA, 0xCD);
    /// <summary>Gets the <c>LightGoldenrodYellow</c> color (#FAFAD2).</summary>
    public static Color LightGoldenrodYellow => new(0xFA, 0xFA, 0xD2);
    /// <summary>Gets the <c>PapayaWhip</c> color (#FFEFD5).</summary>
    public static Color PapayaWhip           => new(0xFF, 0xEF, 0xD5);
    /// <summary>Gets the <c>Moccasin</c> color (#FFE4B5).</summary>
    public static Color Moccasin             => new(0xFF, 0xE4, 0xB5);
    /// <summary>Gets the <c>PeachPuff</c> color (#FFDAB9).</summary>
    public static Color PeachPuff            => new(0xFF, 0xDA, 0xB9);
    /// <summary>Gets the <c>PaleGoldenrod</c> color (#EEE8AA).</summary>
    public static Color PaleGoldenrod        => new(0xEE, 0xE8, 0xAA);
    /// <summary>Gets the <c>Khaki</c> color (#F0E68C).</summary>
    public static Color Khaki                => new(0xF0, 0xE6, 0x8C);
    /// <summary>Gets the <c>DarkKhaki</c> color (#BDB76B).</summary>
    public static Color DarkKhaki            => new(0xBD, 0xB7, 0x6B);
    /// <summary>Gets the <c>Gold</c> color (#FFD700).</summary>
    public static Color Gold                 => new(0xFF, 0xD7, 0x00);

    // --- Browns ---
    /// <summary>Gets the <c>Cornsilk</c> color (#FFF8DC).</summary>
    public static Color Cornsilk      => new(0xFF, 0xF8, 0xDC);
    /// <summary>Gets the <c>BlanchedAlmond</c> color (#FFEBCD).</summary>
    public static Color BlanchedAlmond => new(0xFF, 0xEB, 0xCD);
    /// <summary>Gets the <c>Bisque</c> color (#FFE4C4).</summary>
    public static Color Bisque         => new(0xFF, 0xE4, 0xC4);
    /// <summary>Gets the <c>NavajoWhite</c> color (#FFDEAD).</summary>
    public static Color NavajoWhite    => new(0xFF, 0xDE, 0xAD);
    /// <summary>Gets the <c>Wheat</c> color (#F5DEB3).</summary>
    public static Color Wheat          => new(0xF5, 0xDE, 0xB3);
    /// <summary>Gets the <c>BurlyWood</c> color (#DEB887).</summary>
    public static Color BurlyWood      => new(0xDE, 0xB8, 0x87);
    /// <summary>Gets the <c>Tan</c> color (#D2B48C).</summary>
    public static Color Tan            => new(0xD2, 0xB4, 0x8C);
    /// <summary>Gets the <c>RosyBrown</c> color (#BC8F8F).</summary>
    public static Color RosyBrown      => new(0xBC, 0x8F, 0x8F);
    /// <summary>Gets the <c>SandyBrown</c> color (#F4A460).</summary>
    public static Color SandyBrown     => new(0xF4, 0xA4, 0x60);
    /// <summary>Gets the <c>GoldenRod</c> color (#DAA520).</summary>
    public static Color GoldenRod      => new(0xDA, 0xA5, 0x20);
    /// <summary>Gets the <c>DarkGoldenRod</c> color (#B8860B).</summary>
    public static Color DarkGoldenRod  => new(0xB8, 0x86, 0x0B);
    /// <summary>Gets the <c>Peru</c> color (#CD853F).</summary>
    public static Color Peru           => new(0xCD, 0x85, 0x3F);
    /// <summary>Gets the <c>Chocolate</c> color (#D2691E).</summary>
    public static Color Chocolate      => new(0xD2, 0x69, 0x1E);
    /// <summary>Gets the <c>SaddleBrown</c> color (#8B4513).</summary>
    public static Color SaddleBrown    => new(0x8B, 0x45, 0x13);
    /// <summary>Gets the <c>Sienna</c> color (#A0522D).</summary>
    public static Color Sienna         => new(0xA0, 0x52, 0x2D);
    /// <summary>Gets the <c>Brown</c> color (#A52A2A).</summary>
    public static Color Brown          => new(0xA5, 0x2A, 0x2A);
    /// <summary>Gets the <c>Maroon</c> color (#800000).</summary>
    public static Color Maroon         => new(0x80, 0x00, 0x00);

    // --- Greens ---
    /// <summary>Gets the <c>DarkOliveGreen</c> color (#556B2F).</summary>
    public static Color DarkOliveGreen    => new(0x55, 0x6B, 0x2F);
    /// <summary>Gets the <c>Olive</c> color (#808000).</summary>
    public static Color Olive             => new(0x80, 0x80, 0x00);
    /// <summary>Gets the <c>OliveDrab</c> color (#6B8E23).</summary>
    public static Color OliveDrab         => new(0x6B, 0x8E, 0x23);
    /// <summary>Gets the <c>YellowGreen</c> color (#9ACD32).</summary>
    public static Color YellowGreen       => new(0x9A, 0xCD, 0x32);
    /// <summary>Gets the <c>LimeGreen</c> color (#32CD32).</summary>
    public static Color LimeGreen         => new(0x32, 0xCD, 0x32);
    /// <summary>Gets the <c>Lime</c> color (#00FF00).</summary>
    public static Color Lime              => new(0x00, 0xFF, 0x00);
    /// <summary>Gets the <c>LawnGreen</c> color (#7CFC00).</summary>
    public static Color LawnGreen         => new(0x7C, 0xFC, 0x00);
    /// <summary>Gets the <c>Chartreuse</c> color (#7FFF00).</summary>
    public static Color Chartreuse        => new(0x7F, 0xFF, 0x00);
    /// <summary>Gets the <c>GreenYellow</c> color (#ADFF2F).</summary>
    public static Color GreenYellow       => new(0xAD, 0xFF, 0x2F);
    /// <summary>Gets the <c>SpringGreen</c> color (#00FF7F).</summary>
    public static Color SpringGreen       => new(0x00, 0xFF, 0x7F);
    /// <summary>Gets the <c>MediumSpringGreen</c> color (#00FA9A).</summary>
    public static Color MediumSpringGreen => new(0x00, 0xFA, 0x9A);
    /// <summary>Gets the <c>LightGreen</c> color (#90EE90).</summary>
    public static Color LightGreen        => new(0x90, 0xEE, 0x90);
    /// <summary>Gets the <c>PaleGreen</c> color (#98FB98).</summary>
    public static Color PaleGreen         => new(0x98, 0xFB, 0x98);
    /// <summary>Gets the <c>DarkSeaGreen</c> color (#8FBC8F).</summary>
    public static Color DarkSeaGreen      => new(0x8F, 0xBC, 0x8F);
    /// <summary>Gets the <c>MediumAquamarine</c> color (#66CDAA).</summary>
    public static Color MediumAquamarine  => new(0x66, 0xCD, 0xAA);
    /// <summary>Gets the <c>MediumSeaGreen</c> color (#3CB371).</summary>
    public static Color MediumSeaGreen    => new(0x3C, 0xB3, 0x71);
    /// <summary>Gets the <c>SeaGreen</c> color (#2E8B57).</summary>
    public static Color SeaGreen          => new(0x2E, 0x8B, 0x57);
    /// <summary>Gets the <c>ForestGreen</c> color (#228B22).</summary>
    public static Color ForestGreen       => new(0x22, 0x8B, 0x22);
    /// <summary>Gets the <c>Green</c> color (#008000).</summary>
    public static Color Green             => new(0x00, 0x80, 0x00);
    /// <summary>Gets the <c>DarkGreen</c> color (#006400).</summary>
    public static Color DarkGreen         => new(0x00, 0x64, 0x00);

    // --- Cyans ---
    /// <summary>Gets the <c>Aqua</c> color (#00FFFF).</summary>
    public static Color Aqua            => new(0x00, 0xFF, 0xFF);
    /// <summary>Gets the <c>Cyan</c> color (#00FFFF).</summary>
    public static Color Cyan            => new(0x00, 0xFF, 0xFF);
    /// <summary>Gets the <c>LightCyan</c> color (#E0FFFF).</summary>
    public static Color LightCyan       => new(0xE0, 0xFF, 0xFF);
    /// <summary>Gets the <c>PaleTurquoise</c> color (#AFEEEE).</summary>
    public static Color PaleTurquoise   => new(0xAF, 0xEE, 0xEE);
    /// <summary>Gets the <c>Aquamarine</c> color (#7FFFD4).</summary>
    public static Color Aquamarine      => new(0x7F, 0xFF, 0xD4);
    /// <summary>Gets the <c>Turquoise</c> color (#40E0D0).</summary>
    public static Color Turquoise       => new(0x40, 0xE0, 0xD0);
    /// <summary>Gets the <c>MediumTurquoise</c> color (#48D1CC).</summary>
    public static Color MediumTurquoise => new(0x48, 0xD1, 0xCC);
    /// <summary>Gets the <c>DarkTurquoise</c> color (#00CED1).</summary>
    public static Color DarkTurquoise   => new(0x00, 0xCE, 0xD1);
    /// <summary>Gets the <c>LightSeaGreen</c> color (#20B2AA).</summary>
    public static Color LightSeaGreen   => new(0x20, 0xB2, 0xAA);
    /// <summary>Gets the <c>CadetBlue</c> color (#5F9EA0).</summary>
    public static Color CadetBlue       => new(0x5F, 0x9E, 0xA0);
    /// <summary>Gets the <c>DarkCyan</c> color (#008B8B).</summary>
    public static Color DarkCyan        => new(0x00, 0x8B, 0x8B);
    /// <summary>Gets the <c>Teal</c> color (#008080).</summary>
    public static Color Teal            => new(0x00, 0x80, 0x80);

    // --- Blues ---
    /// <summary>Gets the <c>LightSteelBlue</c> color (#B0C4DE).</summary>
    public static Color LightSteelBlue => new(0xB0, 0xC4, 0xDE);
    /// <summary>Gets the <c>PowderBlue</c> color (#B0E0E6).</summary>
    public static Color PowderBlue     => new(0xB0, 0xE0, 0xE6);
    /// <summary>Gets the <c>LightBlue</c> color (#ADD8E6).</summary>
    public static Color LightBlue      => new(0xAD, 0xD8, 0xE6);
    /// <summary>Gets the <c>SkyBlue</c> color (#87CEEB).</summary>
    public static Color SkyBlue        => new(0x87, 0xCE, 0xEB);
    /// <summary>Gets the <c>LightSkyBlue</c> color (#87CEFA).</summary>
    public static Color LightSkyBlue   => new(0x87, 0xCE, 0xFA);
    /// <summary>Gets the <c>DeepSkyBlue</c> color (#00BFFF).</summary>
    public static Color DeepSkyBlue    => new(0x00, 0xBF, 0xFF);
    /// <summary>Gets the <c>DodgerBlue</c> color (#1E90FF).</summary>
    public static Color DodgerBlue     => new(0x1E, 0x90, 0xFF);
    /// <summary>Gets the <c>CornflowerBlue</c> color (#6495ED).</summary>
    public static Color CornflowerBlue => new(0x64, 0x95, 0xED);
    /// <summary>Gets the <c>SteelBlue</c> color (#4682B4).</summary>
    public static Color SteelBlue      => new(0x46, 0x82, 0xB4);
    /// <summary>Gets the <c>RoyalBlue</c> color (#4169E1).</summary>
    public static Color RoyalBlue      => new(0x41, 0x69, 0xE1);
    /// <summary>Gets the <c>Blue</c> color (#0000FF).</summary>
    public static Color Blue           => new(0x00, 0x00, 0xFF);
    /// <summary>Gets the <c>MediumBlue</c> color (#0000CD).</summary>
    public static Color MediumBlue     => new(0x00, 0x00, 0xCD);
    /// <summary>Gets the <c>DarkBlue</c> color (#00008B).</summary>
    public static Color DarkBlue       => new(0x00, 0x00, 0x8B);
    /// <summary>Gets the <c>Navy</c> color (#000080).</summary>
    public static Color Navy           => new(0x00, 0x00, 0x80);
    /// <summary>Gets the <c>MidnightBlue</c> color (#191970).</summary>
    public static Color MidnightBlue   => new(0x19, 0x19, 0x70);

    // --- Purples ---
    /// <summary>Gets the <c>Lavender</c> color (#E6E6FA).</summary>
    public static Color Lavender        => new(0xE6, 0xE6, 0xFA);
    /// <summary>Gets the <c>Thistle</c> color (#D8BFD8).</summary>
    public static Color Thistle         => new(0xD8, 0xBF, 0xD8);
    /// <summary>Gets the <c>Plum</c> color (#DDA0DD).</summary>
    public static Color Plum            => new(0xDD, 0xA0, 0xDD);
    /// <summary>Gets the <c>Violet</c> color (#EE82EE).</summary>
    public static Color Violet          => new(0xEE, 0x82, 0xEE);
    /// <summary>Gets the <c>Orchid</c> color (#DA70D6).</summary>
    public static Color Orchid          => new(0xDA, 0x70, 0xD6);
    /// <summary>Gets the <c>Fuchsia</c> color (#FF00FF).</summary>
    public static Color Fuchsia         => new(0xFF, 0x00, 0xFF);
    /// <summary>Gets the <c>Magenta</c> color (#FF00FF).</summary>
    public static Color Magenta         => new(0xFF, 0x00, 0xFF);
    /// <summary>Gets the <c>MediumOrchid</c> color (#BA55D3).</summary>
    public static Color MediumOrchid    => new(0xBA, 0x55, 0xD3);
    /// <summary>Gets the <c>MediumPurple</c> color (#9370DB).</summary>
    public static Color MediumPurple    => new(0x93, 0x70, 0xDB);
    /// <summary>Gets the <c>BlueViolet</c> color (#8A2BE2).</summary>
    public static Color BlueViolet      => new(0x8A, 0x2B, 0xE2);
    /// <summary>Gets the <c>DarkViolet</c> color (#9400D3).</summary>
    public static Color DarkViolet      => new(0x94, 0x00, 0xD3);
    /// <summary>Gets the <c>DarkOrchid</c> color (#9932CC).</summary>
    public static Color DarkOrchid      => new(0x99, 0x32, 0xCC);
    /// <summary>Gets the <c>DarkMagenta</c> color (#8B008B).</summary>
    public static Color DarkMagenta     => new(0x8B, 0x00, 0x8B);
    /// <summary>Gets the <c>Purple</c> color (#800080).</summary>
    public static Color Purple          => new(0x80, 0x00, 0x80);
    /// <summary>Gets the <c>Indigo</c> color (#4B0082).</summary>
    public static Color Indigo          => new(0x4B, 0x00, 0x82);
    /// <summary>Gets the <c>DarkSlateBlue</c> color (#483D8B).</summary>
    public static Color DarkSlateBlue   => new(0x48, 0x3D, 0x8B);
    /// <summary>Gets the <c>SlateBlue</c> color (#6A5ACD).</summary>
    public static Color SlateBlue       => new(0x6A, 0x5A, 0xCD);
    /// <summary>Gets the <c>MediumSlateBlue</c> color (#7B68EE).</summary>
    public static Color MediumSlateBlue => new(0x7B, 0x68, 0xEE);
    /// <summary>Gets the <c>RebeccaPurple</c> color (#663399).</summary>
    public static Color RebeccaPurple   => new(0x66, 0x33, 0x99);

    // --- Whites ---
    /// <summary>Gets the <c>White</c> color (#FFFFFF).</summary>
    public static Color White         => new(0xFF, 0xFF, 0xFF);
    /// <summary>Gets the <c>Snow</c> color (#FFFAFA).</summary>
    public static Color Snow          => new(0xFF, 0xFA, 0xFA);
    /// <summary>Gets the <c>Honeydew</c> color (#F0FFF0).</summary>
    public static Color Honeydew      => new(0xF0, 0xFF, 0xF0);
    /// <summary>Gets the <c>MintCream</c> color (#F5FFFA).</summary>
    public static Color MintCream     => new(0xF5, 0xFF, 0xFA);
    /// <summary>Gets the <c>Azure</c> color (#F0FFFF).</summary>
    public static Color Azure         => new(0xF0, 0xFF, 0xFF);
    /// <summary>Gets the <c>AliceBlue</c> color (#F0F8FF).</summary>
    public static Color AliceBlue     => new(0xF0, 0xF8, 0xFF);
    /// <summary>Gets the <c>GhostWhite</c> color (#F8F8FF).</summary>
    public static Color GhostWhite    => new(0xF8, 0xF8, 0xFF);
    /// <summary>Gets the <c>WhiteSmoke</c> color (#F5F5F5).</summary>
    public static Color WhiteSmoke    => new(0xF5, 0xF5, 0xF5);
    /// <summary>Gets the <c>Seashell</c> color (#FFF5EE).</summary>
    public static Color Seashell      => new(0xFF, 0xF5, 0xEE);
    /// <summary>Gets the <c>Beige</c> color (#F5F5DC).</summary>
    public static Color Beige         => new(0xF5, 0xF5, 0xDC);
    /// <summary>Gets the <c>OldLace</c> color (#FDF5E6).</summary>
    public static Color OldLace       => new(0xFD, 0xF5, 0xE6);
    /// <summary>Gets the <c>FloralWhite</c> color (#FFFAF0).</summary>
    public static Color FloralWhite   => new(0xFF, 0xFA, 0xF0);
    /// <summary>Gets the <c>Ivory</c> color (#FFFFF0).</summary>
    public static Color Ivory         => new(0xFF, 0xFF, 0xF0);
    /// <summary>Gets the <c>AntiqueWhite</c> color (#FAEBD7).</summary>
    public static Color AntiqueWhite  => new(0xFA, 0xEB, 0xD7);
    /// <summary>Gets the <c>Linen</c> color (#FAF0E6).</summary>
    public static Color Linen         => new(0xFA, 0xF0, 0xE6);
    /// <summary>Gets the <c>LavenderBlush</c> color (#FFF0F5).</summary>
    public static Color LavenderBlush => new(0xFF, 0xF0, 0xF5);
    /// <summary>Gets the <c>MistyRose</c> color (#FFE4E1).</summary>
    public static Color MistyRose     => new(0xFF, 0xE4, 0xE1);

    // --- Grays (and grey aliases) ---
    /// <summary>Gets the <c>Gainsboro</c> color (#DCDCDC).</summary>
    public static Color Gainsboro      => new(0xDC, 0xDC, 0xDC);
    /// <summary>Gets the <c>LightGray</c> color (#D3D3D3).</summary>
    public static Color LightGray      => new(0xD3, 0xD3, 0xD3);
    /// <summary>Gets the <c>LightGrey</c> color (#D3D3D3).</summary>
    public static Color LightGrey      => new(0xD3, 0xD3, 0xD3);
    /// <summary>Gets the <c>Silver</c> color (#C0C0C0).</summary>
    public static Color Silver         => new(0xC0, 0xC0, 0xC0);
    /// <summary>Gets the <c>DarkGray</c> color (#A9A9A9).</summary>
    public static Color DarkGray       => new(0xA9, 0xA9, 0xA9);
    /// <summary>Gets the <c>DarkGrey</c> color (#A9A9A9).</summary>
    public static Color DarkGrey       => new(0xA9, 0xA9, 0xA9);
    /// <summary>Gets the <c>Gray</c> color (#808080).</summary>
    public static Color Gray           => new(0x80, 0x80, 0x80);
    /// <summary>Gets the <c>Grey</c> color (#808080).</summary>
    public static Color Grey           => new(0x80, 0x80, 0x80);
    /// <summary>Gets the <c>DimGray</c> color (#696969).</summary>
    public static Color DimGray        => new(0x69, 0x69, 0x69);
    /// <summary>Gets the <c>DimGrey</c> color (#696969).</summary>
    public static Color DimGrey        => new(0x69, 0x69, 0x69);
    /// <summary>Gets the <c>LightSlateGray</c> color (#778899).</summary>
    public static Color LightSlateGray => new(0x77, 0x88, 0x99);
    /// <summary>Gets the <c>LightSlateGrey</c> color (#778899).</summary>
    public static Color LightSlateGrey => new(0x77, 0x88, 0x99);
    /// <summary>Gets the <c>SlateGray</c> color (#708090).</summary>
    public static Color SlateGray      => new(0x70, 0x80, 0x90);
    /// <summary>Gets the <c>SlateGrey</c> color (#708090).</summary>
    public static Color SlateGrey      => new(0x70, 0x80, 0x90);
    /// <summary>Gets the <c>DarkSlateGray</c> color (#2F4F4F).</summary>
    public static Color DarkSlateGray  => new(0x2F, 0x4F, 0x4F);
    /// <summary>Gets the <c>DarkSlateGrey</c> color (#2F4F4F).</summary>
    public static Color DarkSlateGrey  => new(0x2F, 0x4F, 0x4F);
    /// <summary>Gets the <c>Black</c> color (#000000).</summary>
    public static Color Black          => new(0x00, 0x00, 0x00);

    public static IReadOnlyDictionary<string, Color> All { get; } = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
    {
        // Pinks
        ["pink"]             = Pink,
        ["lightpink"]        = LightPink,
        ["hotpink"]          = HotPink,
        ["deeppink"]         = DeepPink,
        ["palevioletred"]    = PaleVioletRed,
        ["mediumvioletred"]  = MediumVioletRed,
        // Reds
        ["lightsalmon"]   = LightSalmon,
        ["salmon"]        = Salmon,
        ["darksalmon"]    = DarkSalmon,
        ["lightcoral"]    = LightCoral,
        ["indianred"]     = IndianRed,
        ["crimson"]       = Crimson,
        ["firebrick"]     = FireBrick,
        ["darkred"]       = DarkRed,
        ["red"]           = Red,
        // Oranges
        ["orangered"]   = OrangeRed,
        ["tomato"]      = Tomato,
        ["coral"]       = Coral,
        ["darkorange"]  = DarkOrange,
        ["orange"]      = Orange,
        // Yellows
        ["yellow"]               = Yellow,
        ["lightyellow"]          = LightYellow,
        ["lemonchiffon"]         = LemonChiffon,
        ["lightgoldenrodyellow"] = LightGoldenrodYellow,
        ["papayawhip"]           = PapayaWhip,
        ["moccasin"]             = Moccasin,
        ["peachpuff"]            = PeachPuff,
        ["palegoldenrod"]        = PaleGoldenrod,
        ["khaki"]                = Khaki,
        ["darkkhaki"]            = DarkKhaki,
        ["gold"]                 = Gold,
        // Browns
        ["cornsilk"]       = Cornsilk,
        ["blanchedalmond"] = BlanchedAlmond,
        ["bisque"]         = Bisque,
        ["navajowhite"]    = NavajoWhite,
        ["wheat"]          = Wheat,
        ["burlywood"]      = BurlyWood,
        ["tan"]            = Tan,
        ["rosybrown"]      = RosyBrown,
        ["sandybrown"]     = SandyBrown,
        ["goldenrod"]      = GoldenRod,
        ["darkgoldenrod"]  = DarkGoldenRod,
        ["peru"]           = Peru,
        ["chocolate"]      = Chocolate,
        ["saddlebrown"]    = SaddleBrown,
        ["sienna"]         = Sienna,
        ["brown"]          = Brown,
        ["maroon"]         = Maroon,
        // Greens
        ["darkolivegreen"]    = DarkOliveGreen,
        ["olive"]             = Olive,
        ["olivedrab"]         = OliveDrab,
        ["yellowgreen"]       = YellowGreen,
        ["limegreen"]         = LimeGreen,
        ["lime"]              = Lime,
        ["lawngreen"]         = LawnGreen,
        ["chartreuse"]        = Chartreuse,
        ["greenyellow"]       = GreenYellow,
        ["springgreen"]       = SpringGreen,
        ["mediumspringgreen"] = MediumSpringGreen,
        ["lightgreen"]        = LightGreen,
        ["palegreen"]         = PaleGreen,
        ["darkseagreen"]      = DarkSeaGreen,
        ["mediumaquamarine"]  = MediumAquamarine,
        ["mediumseagreen"]    = MediumSeaGreen,
        ["seagreen"]          = SeaGreen,
        ["forestgreen"]       = ForestGreen,
        ["green"]             = Green,
        ["darkgreen"]         = DarkGreen,
        // Cyans
        ["aqua"]            = Aqua,
        ["cyan"]            = Cyan,
        ["lightcyan"]       = LightCyan,
        ["paleturquoise"]   = PaleTurquoise,
        ["aquamarine"]      = Aquamarine,
        ["turquoise"]       = Turquoise,
        ["mediumturquoise"] = MediumTurquoise,
        ["darkturquoise"]   = DarkTurquoise,
        ["lightseagreen"]   = LightSeaGreen,
        ["cadetblue"]       = CadetBlue,
        ["darkcyan"]        = DarkCyan,
        ["teal"]            = Teal,
        // Blues
        ["lightsteelblue"] = LightSteelBlue,
        ["powderblue"]     = PowderBlue,
        ["lightblue"]      = LightBlue,
        ["skyblue"]        = SkyBlue,
        ["lightskyblue"]   = LightSkyBlue,
        ["deepskyblue"]    = DeepSkyBlue,
        ["dodgerblue"]     = DodgerBlue,
        ["cornflowerblue"] = CornflowerBlue,
        ["steelblue"]      = SteelBlue,
        ["royalblue"]      = RoyalBlue,
        ["blue"]           = Blue,
        ["mediumblue"]     = MediumBlue,
        ["darkblue"]       = DarkBlue,
        ["navy"]           = Navy,
        ["midnightblue"]   = MidnightBlue,
        // Purples
        ["lavender"]        = Lavender,
        ["thistle"]         = Thistle,
        ["plum"]            = Plum,
        ["violet"]          = Violet,
        ["orchid"]          = Orchid,
        ["fuchsia"]         = Fuchsia,
        ["magenta"]         = Magenta,
        ["mediumorchid"]    = MediumOrchid,
        ["mediumpurple"]    = MediumPurple,
        ["blueviolet"]      = BlueViolet,
        ["darkviolet"]      = DarkViolet,
        ["darkorchid"]      = DarkOrchid,
        ["darkmagenta"]     = DarkMagenta,
        ["purple"]          = Purple,
        ["indigo"]          = Indigo,
        ["darkslateblue"]   = DarkSlateBlue,
        ["slateblue"]       = SlateBlue,
        ["mediumslateblue"] = MediumSlateBlue,
        ["rebeccapurple"]   = RebeccaPurple,
        // Whites
        ["white"]         = White,
        ["snow"]          = Snow,
        ["honeydew"]      = Honeydew,
        ["mintcream"]     = MintCream,
        ["azure"]         = Azure,
        ["aliceblue"]     = AliceBlue,
        ["ghostwhite"]    = GhostWhite,
        ["whitesmoke"]    = WhiteSmoke,
        ["seashell"]      = Seashell,
        ["beige"]         = Beige,
        ["oldlace"]       = OldLace,
        ["floralwhite"]   = FloralWhite,
        ["ivory"]         = Ivory,
        ["antiquewhite"]  = AntiqueWhite,
        ["linen"]         = Linen,
        ["lavenderblush"] = LavenderBlush,
        ["mistyrose"]     = MistyRose,
        // Grays + grey aliases
        ["gainsboro"]      = Gainsboro,
        ["lightgray"]      = LightGray,
        ["lightgrey"]      = LightGrey,
        ["silver"]         = Silver,
        ["darkgray"]       = DarkGray,
        ["darkgrey"]       = DarkGrey,
        ["gray"]           = Gray,
        ["grey"]           = Grey,
        ["dimgray"]        = DimGray,
        ["dimgrey"]        = DimGrey,
        ["lightslategray"] = LightSlateGray,
        ["lightslategrey"] = LightSlateGrey,
        ["slategray"]      = SlateGray,
        ["slategrey"]      = SlateGrey,
        ["darkslategray"]  = DarkSlateGray,
        ["darkslategrey"]  = DarkSlateGrey,
        ["black"]          = Black,
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
}
