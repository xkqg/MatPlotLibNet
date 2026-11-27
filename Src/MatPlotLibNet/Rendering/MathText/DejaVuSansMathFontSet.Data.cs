// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

// Written by tools/mpl_reference/generate_mathtext.py from matplotlib 3.11.2: the glyphs AutoHeightChar chooses among under
// mathtext.fontset 'dejavusans', read from DejaVuSansFonts.get_sized_alternatives_for_symbol: change the table by running it again.

using System.Collections.Frozen;

namespace MatPlotLibNet.Rendering.MathText;

internal sealed partial class DejaVuSansMathFontSet
{
    /// <summary>The glyphs matplotlib's AutoHeightChar chooses among to grow each delimiter and the radical sign under 'dejavusans',
    /// size 0 first (DejaVu Sans, then the STIXSize faces that hold the character), by the character the layout hands it.</summary>
    private static FrozenDictionary<char, MathSizes> SizedTable() => new Dictionary<char, MathSizes>
    {
        ['\u0028'] = new([new(Book, 0x28), new(StixFallback.FaceOf(StixFace.SizeOne), 0x28), new(StixFallback.FaceOf(StixFace.SizeTwo), 0x28), new(StixFallback.FaceOf(StixFace.SizeThree), 0x28), new(StixFallback.FaceOf(StixFace.SizeFour), 0x28)], true),
        ['\u0029'] = new([new(Book, 0x29), new(StixFallback.FaceOf(StixFace.SizeOne), 0x29), new(StixFallback.FaceOf(StixFace.SizeTwo), 0x29), new(StixFallback.FaceOf(StixFace.SizeThree), 0x29), new(StixFallback.FaceOf(StixFace.SizeFour), 0x29)], true),
        ['\u002F'] = new([new(Book, 0x2F), new(StixFallback.FaceOf(StixFace.SizeOne), 0x2F), new(StixFallback.FaceOf(StixFace.SizeTwo), 0x2F), new(StixFallback.FaceOf(StixFace.SizeThree), 0x2F), new(StixFallback.FaceOf(StixFace.SizeFour), 0x2F)], true),
        ['\u003C'] = new([new(Book, 0x27E8), new(StixFallback.FaceOf(StixFace.SizeOne), 0x27E8), new(StixFallback.FaceOf(StixFace.SizeTwo), 0x27E8), new(StixFallback.FaceOf(StixFace.SizeThree), 0x27E8), new(StixFallback.FaceOf(StixFace.SizeFour), 0x27E8)], true),
        ['\u003E'] = new([new(Book, 0x27E9), new(StixFallback.FaceOf(StixFace.SizeOne), 0x27E9), new(StixFallback.FaceOf(StixFace.SizeTwo), 0x27E9), new(StixFallback.FaceOf(StixFace.SizeThree), 0x27E9), new(StixFallback.FaceOf(StixFace.SizeFour), 0x27E9)], true),
        ['\u005B'] = new([new(Book, 0x5B), new(StixFallback.FaceOf(StixFace.SizeOne), 0x5B), new(StixFallback.FaceOf(StixFace.SizeTwo), 0x5B), new(StixFallback.FaceOf(StixFace.SizeThree), 0x5B), new(StixFallback.FaceOf(StixFace.SizeFour), 0x5B)], true),
        ['\u21D3'] = new([new(Book, 0x21D3)], false),
        ['\u21D1'] = new([new(Book, 0x21D1)], false),
        ['\u21D5'] = new([new(Book, 0x21D5)], false),
        ['\u2016'] = new([new(Book, 0x2016)], false),
        ['\u005C'] = new([new(Book, 0x5C), new(StixFallback.FaceOf(StixFace.SizeOne), 0x5C), new(StixFallback.FaceOf(StixFace.SizeTwo), 0x5C), new(StixFallback.FaceOf(StixFace.SizeThree), 0x5C), new(StixFallback.FaceOf(StixFace.SizeFour), 0x5C)], true),
        ['\u2193'] = new([new(Book, 0x2193)], false),
        ['\u27E8'] = new([new(Book, 0x27E8), new(StixFallback.FaceOf(StixFace.SizeOne), 0x27E8), new(StixFallback.FaceOf(StixFace.SizeTwo), 0x27E8), new(StixFallback.FaceOf(StixFace.SizeThree), 0x27E8), new(StixFallback.FaceOf(StixFace.SizeFour), 0x27E8)], true),
        ['\u007B'] = new([new(Book, 0x7B), new(StixFallback.FaceOf(StixFace.SizeOne), 0x7B), new(StixFallback.FaceOf(StixFace.SizeTwo), 0x7B), new(StixFallback.FaceOf(StixFace.SizeThree), 0x7B), new(StixFallback.FaceOf(StixFace.SizeFour), 0x7B)], true),
        ['\u2308'] = new([new(Book, 0x2308), new(StixFallback.FaceOf(StixFace.SizeOne), 0x2308), new(StixFallback.FaceOf(StixFace.SizeTwo), 0x2308), new(StixFallback.FaceOf(StixFace.SizeThree), 0x2308), new(StixFallback.FaceOf(StixFace.SizeFour), 0x2308)], true),
        ['\u230A'] = new([new(Book, 0x230A), new(StixFallback.FaceOf(StixFace.SizeOne), 0x230A), new(StixFallback.FaceOf(StixFace.SizeTwo), 0x230A), new(StixFallback.FaceOf(StixFace.SizeThree), 0x230A), new(StixFallback.FaceOf(StixFace.SizeFour), 0x230A)], true),
        ['\u27EE'] = new([new(StixFallback.FaceOf(StixFace.Regular), 0x27EE)], false),
        ['\u27E9'] = new([new(Book, 0x27E9), new(StixFallback.FaceOf(StixFace.SizeOne), 0x27E9), new(StixFallback.FaceOf(StixFace.SizeTwo), 0x27E9), new(StixFallback.FaceOf(StixFace.SizeThree), 0x27E9), new(StixFallback.FaceOf(StixFace.SizeFour), 0x27E9)], true),
        ['\u007D'] = new([new(Book, 0x7D), new(StixFallback.FaceOf(StixFace.SizeOne), 0x7D), new(StixFallback.FaceOf(StixFace.SizeTwo), 0x7D), new(StixFallback.FaceOf(StixFace.SizeThree), 0x7D), new(StixFallback.FaceOf(StixFace.SizeFour), 0x7D)], true),
        ['\u005D'] = new([new(Book, 0x5D), new(StixFallback.FaceOf(StixFace.SizeOne), 0x5D), new(StixFallback.FaceOf(StixFace.SizeTwo), 0x5D), new(StixFallback.FaceOf(StixFace.SizeThree), 0x5D), new(StixFallback.FaceOf(StixFace.SizeFour), 0x5D)], true),
        ['\u2309'] = new([new(Book, 0x2309), new(StixFallback.FaceOf(StixFace.SizeOne), 0x2309), new(StixFallback.FaceOf(StixFace.SizeTwo), 0x2309), new(StixFallback.FaceOf(StixFace.SizeThree), 0x2309), new(StixFallback.FaceOf(StixFace.SizeFour), 0x2309)], true),
        ['\u230B'] = new([new(Book, 0x230B), new(StixFallback.FaceOf(StixFace.SizeOne), 0x230B), new(StixFallback.FaceOf(StixFace.SizeTwo), 0x230B), new(StixFallback.FaceOf(StixFace.SizeThree), 0x230B), new(StixFallback.FaceOf(StixFace.SizeFour), 0x230B)], true),
        ['\u27EF'] = new([new(StixFallback.FaceOf(StixFace.Regular), 0x27EF)], false),
        ['\u2191'] = new([new(Book, 0x2191)], false),
        ['\u2195'] = new([new(Book, 0x2195)], false),
        ['\u007C'] = new([new(Book, 0x7C)], false),
        ['\u221A'] = new([new(Book, 0x221A), new(StixFallback.FaceOf(StixFace.SizeOne), 0x221A), new(StixFallback.FaceOf(StixFace.SizeTwo), 0x221A), new(StixFallback.FaceOf(StixFace.SizeThree), 0x221A)], true),
    }.ToFrozenDictionary();
}
