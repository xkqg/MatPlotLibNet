// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;

namespace MatPlotLibNet.Styling;

/// <summary>All 148 CSS Level 4 named colors as static <see cref="Color"/> properties.</summary>
/// <remarks>Use <see cref="Color.FromName"/> for case-insensitive name lookup.</remarks>
public static class Css4Colors
{
    // --- Pinks ---
    public static Color Pink             => new(0xFF, 0xC0, 0xCB);
    public static Color LightPink        => new(0xFF, 0xB6, 0xC1);
    public static Color HotPink          => new(0xFF, 0x69, 0xB4);
    public static Color DeepPink         => new(0xFF, 0x14, 0x93);
    public static Color PaleVioletRed    => new(0xDB, 0x70, 0x93);
    public static Color MediumVioletRed  => new(0xC7, 0x15, 0x85);

    // --- Reds ---
    public static Color LightSalmon  => new(0xFF, 0xA0, 0x7A);
    public static Color Salmon       => new(0xFA, 0x80, 0x72);
    public static Color DarkSalmon   => new(0xE9, 0x96, 0x7A);
    public static Color LightCoral   => new(0xF0, 0x80, 0x80);
    public static Color IndianRed    => new(0xCD, 0x5C, 0x5C);
    public static Color Crimson      => new(0xDC, 0x14, 0x3C);
    public static Color FireBrick    => new(0xB2, 0x22, 0x22);
    public static Color DarkRed      => new(0x8B, 0x00, 0x00);
    public static Color Red          => new(0xFF, 0x00, 0x00);

    // --- Oranges ---
    public static Color OrangeRed   => new(0xFF, 0x45, 0x00);
    public static Color Tomato      => new(0xFF, 0x63, 0x47);
    public static Color Coral       => new(0xFF, 0x7F, 0x50);
    public static Color DarkOrange  => new(0xFF, 0x8C, 0x00);
    public static Color Orange      => new(0xFF, 0xA5, 0x00);

    // --- Yellows ---
    public static Color Yellow               => new(0xFF, 0xFF, 0x00);
    public static Color LightYellow          => new(0xFF, 0xFF, 0xE0);
    public static Color LemonChiffon         => new(0xFF, 0xFA, 0xCD);
    public static Color LightGoldenrodYellow => new(0xFA, 0xFA, 0xD2);
    public static Color PapayaWhip           => new(0xFF, 0xEF, 0xD5);
    public static Color Moccasin             => new(0xFF, 0xE4, 0xB5);
    public static Color PeachPuff            => new(0xFF, 0xDA, 0xB9);
    public static Color PaleGoldenrod        => new(0xEE, 0xE8, 0xAA);
    public static Color Khaki                => new(0xF0, 0xE6, 0x8C);
    public static Color DarkKhaki            => new(0xBD, 0xB7, 0x6B);
    public static Color Gold                 => new(0xFF, 0xD7, 0x00);

    // --- Browns ---
    public static Color Cornsilk      => new(0xFF, 0xF8, 0xDC);
    public static Color BlanchedAlmond => new(0xFF, 0xEB, 0xCD);
    public static Color Bisque         => new(0xFF, 0xE4, 0xC4);
    public static Color NavajoWhite    => new(0xFF, 0xDE, 0xAD);
    public static Color Wheat          => new(0xF5, 0xDE, 0xB3);
    public static Color BurlyWood      => new(0xDE, 0xB8, 0x87);
    public static Color Tan            => new(0xD2, 0xB4, 0x8C);
    public static Color RosyBrown      => new(0xBC, 0x8F, 0x8F);
    public static Color SandyBrown     => new(0xF4, 0xA4, 0x60);
    public static Color GoldenRod      => new(0xDA, 0xA5, 0x20);
    public static Color DarkGoldenRod  => new(0xB8, 0x86, 0x0B);
    public static Color Peru           => new(0xCD, 0x85, 0x3F);
    public static Color Chocolate      => new(0xD2, 0x69, 0x1E);
    public static Color SaddleBrown    => new(0x8B, 0x45, 0x13);
    public static Color Sienna         => new(0xA0, 0x52, 0x2D);
    public static Color Brown          => new(0xA5, 0x2A, 0x2A);
    public static Color Maroon         => new(0x80, 0x00, 0x00);

    // --- Greens ---
    public static Color DarkOliveGreen    => new(0x55, 0x6B, 0x2F);
    public static Color Olive             => new(0x80, 0x80, 0x00);
    public static Color OliveDrab         => new(0x6B, 0x8E, 0x23);
    public static Color YellowGreen       => new(0x9A, 0xCD, 0x32);
    public static Color LimeGreen         => new(0x32, 0xCD, 0x32);
    public static Color Lime              => new(0x00, 0xFF, 0x00);
    public static Color LawnGreen         => new(0x7C, 0xFC, 0x00);
    public static Color Chartreuse        => new(0x7F, 0xFF, 0x00);
    public static Color GreenYellow       => new(0xAD, 0xFF, 0x2F);
    public static Color SpringGreen       => new(0x00, 0xFF, 0x7F);
    public static Color MediumSpringGreen => new(0x00, 0xFA, 0x9A);
    public static Color LightGreen        => new(0x90, 0xEE, 0x90);
    public static Color PaleGreen         => new(0x98, 0xFB, 0x98);
    public static Color DarkSeaGreen      => new(0x8F, 0xBC, 0x8F);
    public static Color MediumAquamarine  => new(0x66, 0xCD, 0xAA);
    public static Color MediumSeaGreen    => new(0x3C, 0xB3, 0x71);
    public static Color SeaGreen          => new(0x2E, 0x8B, 0x57);
    public static Color ForestGreen       => new(0x22, 0x8B, 0x22);
    public static Color Green             => new(0x00, 0x80, 0x00);
    public static Color DarkGreen         => new(0x00, 0x64, 0x00);

    // --- Cyans ---
    public static Color Aqua            => new(0x00, 0xFF, 0xFF);
    public static Color Cyan            => new(0x00, 0xFF, 0xFF);
    public static Color LightCyan       => new(0xE0, 0xFF, 0xFF);
    public static Color PaleTurquoise   => new(0xAF, 0xEE, 0xEE);
    public static Color Aquamarine      => new(0x7F, 0xFF, 0xD4);
    public static Color Turquoise       => new(0x40, 0xE0, 0xD0);
    public static Color MediumTurquoise => new(0x48, 0xD1, 0xCC);
    public static Color DarkTurquoise   => new(0x00, 0xCE, 0xD1);
    public static Color LightSeaGreen   => new(0x20, 0xB2, 0xAA);
    public static Color CadetBlue       => new(0x5F, 0x9E, 0xA0);
    public static Color DarkCyan        => new(0x00, 0x8B, 0x8B);
    public static Color Teal            => new(0x00, 0x80, 0x80);

    // --- Blues ---
    public static Color LightSteelBlue => new(0xB0, 0xC4, 0xDE);
    public static Color PowderBlue     => new(0xB0, 0xE0, 0xE6);
    public static Color LightBlue      => new(0xAD, 0xD8, 0xE6);
    public static Color SkyBlue        => new(0x87, 0xCE, 0xEB);
    public static Color LightSkyBlue   => new(0x87, 0xCE, 0xFA);
    public static Color DeepSkyBlue    => new(0x00, 0xBF, 0xFF);
    public static Color DodgerBlue     => new(0x1E, 0x90, 0xFF);
    public static Color CornflowerBlue => new(0x64, 0x95, 0xED);
    public static Color SteelBlue      => new(0x46, 0x82, 0xB4);
    public static Color RoyalBlue      => new(0x41, 0x69, 0xE1);
    public static Color Blue           => new(0x00, 0x00, 0xFF);
    public static Color MediumBlue     => new(0x00, 0x00, 0xCD);
    public static Color DarkBlue       => new(0x00, 0x00, 0x8B);
    public static Color Navy           => new(0x00, 0x00, 0x80);
    public static Color MidnightBlue   => new(0x19, 0x19, 0x70);

    // --- Purples ---
    public static Color Lavender        => new(0xE6, 0xE6, 0xFA);
    public static Color Thistle         => new(0xD8, 0xBF, 0xD8);
    public static Color Plum            => new(0xDD, 0xA0, 0xDD);
    public static Color Violet          => new(0xEE, 0x82, 0xEE);
    public static Color Orchid          => new(0xDA, 0x70, 0xD6);
    public static Color Fuchsia         => new(0xFF, 0x00, 0xFF);
    public static Color Magenta         => new(0xFF, 0x00, 0xFF);
    public static Color MediumOrchid    => new(0xBA, 0x55, 0xD3);
    public static Color MediumPurple    => new(0x93, 0x70, 0xDB);
    public static Color BlueViolet      => new(0x8A, 0x2B, 0xE2);
    public static Color DarkViolet      => new(0x94, 0x00, 0xD3);
    public static Color DarkOrchid      => new(0x99, 0x32, 0xCC);
    public static Color DarkMagenta     => new(0x8B, 0x00, 0x8B);
    public static Color Purple          => new(0x80, 0x00, 0x80);
    public static Color Indigo          => new(0x4B, 0x00, 0x82);
    public static Color DarkSlateBlue   => new(0x48, 0x3D, 0x8B);
    public static Color SlateBlue       => new(0x6A, 0x5A, 0xCD);
    public static Color MediumSlateBlue => new(0x7B, 0x68, 0xEE);
    public static Color RebeccaPurple   => new(0x66, 0x33, 0x99);

    // --- Whites ---
    public static Color White         => new(0xFF, 0xFF, 0xFF);
    public static Color Snow          => new(0xFF, 0xFA, 0xFA);
    public static Color Honeydew      => new(0xF0, 0xFF, 0xF0);
    public static Color MintCream     => new(0xF5, 0xFF, 0xFA);
    public static Color Azure         => new(0xF0, 0xFF, 0xFF);
    public static Color AliceBlue     => new(0xF0, 0xF8, 0xFF);
    public static Color GhostWhite    => new(0xF8, 0xF8, 0xFF);
    public static Color WhiteSmoke    => new(0xF5, 0xF5, 0xF5);
    public static Color Seashell      => new(0xFF, 0xF5, 0xEE);
    public static Color Beige         => new(0xF5, 0xF5, 0xDC);
    public static Color OldLace       => new(0xFD, 0xF5, 0xE6);
    public static Color FloralWhite   => new(0xFF, 0xFA, 0xF0);
    public static Color Ivory         => new(0xFF, 0xFF, 0xF0);
    public static Color AntiqueWhite  => new(0xFA, 0xEB, 0xD7);
    public static Color Linen         => new(0xFA, 0xF0, 0xE6);
    public static Color LavenderBlush => new(0xFF, 0xF0, 0xF5);
    public static Color MistyRose     => new(0xFF, 0xE4, 0xE1);

    // --- Grays (and grey aliases) ---
    public static Color Gainsboro      => new(0xDC, 0xDC, 0xDC);
    public static Color LightGray      => new(0xD3, 0xD3, 0xD3);
    public static Color LightGrey      => new(0xD3, 0xD3, 0xD3);
    public static Color Silver         => new(0xC0, 0xC0, 0xC0);
    public static Color DarkGray       => new(0xA9, 0xA9, 0xA9);
    public static Color DarkGrey       => new(0xA9, 0xA9, 0xA9);
    public static Color Gray           => new(0x80, 0x80, 0x80);
    public static Color Grey           => new(0x80, 0x80, 0x80);
    public static Color DimGray        => new(0x69, 0x69, 0x69);
    public static Color DimGrey        => new(0x69, 0x69, 0x69);
    public static Color LightSlateGray => new(0x77, 0x88, 0x99);
    public static Color LightSlateGrey => new(0x77, 0x88, 0x99);
    public static Color SlateGray      => new(0x70, 0x80, 0x90);
    public static Color SlateGrey      => new(0x70, 0x80, 0x90);
    public static Color DarkSlateGray  => new(0x2F, 0x4F, 0x4F);
    public static Color DarkSlateGrey  => new(0x2F, 0x4F, 0x4F);
    public static Color Black          => new(0x00, 0x00, 0x00);

    /// <summary>All 148 CSS4 named colors keyed by lowercase name.</summary>
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
