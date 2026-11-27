// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging.OwnedRules;

/// <summary>The rules of math text: a label that holds math is parsed by its <c>TextLabel</c> and laid out once by its tree of
/// boxes, every size of it is a value of its font set, the STIX faces are one table, and the names, characters and glyphs of the
/// grammar are written in the generated tables.</summary>
internal static class MathTextRules
{
    private static readonly OwnedRule MathParsedByTheLabel = new(
        "a text that holds math is parsed by its TextLabel when it is read (TextLabel.Parsed), its tree kept by the figure that draws it from one draw to the next (KeptMathTexts, told to the surface with the looks): no store of parsed texts of the process beside it, and nothing parses a label by itself",
        @"ConditionalWeakTable<\s*string\s*,\s*(?:RichText|\w*Box)|\bstatic\b[^;=(]*Dictionary<\s*string\s*,\s*(?:RichText|\w*Box)\s*>|(?:MathTextParser|MathTextCursor)\.Parse\(",
        "Src/MatPlotLibNet/Rendering/MathText/TextLabel.cs",
        "Src/MatPlotLibNet/Rendering/MathText/MathTextParser.cs") { Under = "Src/MatPlotLibNet/Rendering/", Finds = [
        "    private static readonly ConditionalWeakTable<string, RichText> Parsed = new();",
        "    private static readonly ConcurrentDictionary<string, LineBox> Trees = new();",
        "    internal static Dictionary<string, MathBox> Kept { get; } = [];",
        "        var tree = MathTextCursor.Parse(axes.Title);",
        "    private static readonly ConditionalWeakTable<string, RichText> Parsed = new();",
        "        var rich = MathTextParser.Parse(title);"], Ignores = [
        "    private Dictionary<string, LineBox> _read = new(StringComparer.Ordinal);",
        "        var label = TextLabel.Of(title);"] };

    private static readonly OwnedRule MathLaidOutOnce = OwnedRule.Nowhere(
        "math is laid out once, by its tree of boxes (MathBox.Lay into a MathInk), which the verb draws and the seam measures: no surface walks the pieces of a rich text or raises a kind of span by a shift of its own",
        @"\bPiecesIn\(|\bPaintOrderIn\(|\bShiftEm\b|baseline-shift",
        under: "Src/") with { Finds = [
        "        var pieces = richText.PaintOrderIn(((IThemedRenderContext)this).MathFonts, font);",
        "        foreach (var piece in text.PiecesIn(set, font))",
        "            float spanY = baseY - (float)(font.Size * piece.ShiftEm);",
        "            if (shift != 0) _sb.Append(\" baseline-shift=\\\"\").Append(shift > 0 ? \"super\" : \"sub\").Append('\"');"] };

    private static readonly OwnedRule SpanKindsAtTheBoundary = new(
        "a construction of math (a script, a fraction's part, a limit, a radical, an accent, a large operator, a matrix) is written into the flat view by its own box and read back only where a hand-made rich text becomes a tree (MathBoxes.Of): no surface or measure reads it",
        @"TextSpanKind\.(?:Superscript|Subscript|FractionNumerator|FractionDenominator|Radical|Accent|LargeOperator|OperatorSubscript|OperatorSuperscript|Matrix\w+)",
        "Src/MatPlotLibNet/Rendering/MathText/MathBox.cs",
        "Src/MatPlotLibNet/Rendering/MathText/ScriptsBox.cs",
        "Src/MatPlotLibNet/Rendering/MathText/LimitsBox.cs",
        "Src/MatPlotLibNet/Rendering/MathText/FractionBox.cs",
        "Src/MatPlotLibNet/Rendering/MathText/RadicalBox.cs",
        "Src/MatPlotLibNet/Rendering/MathText/MathBoxes.cs",
        "Src/MatPlotLibNet/Rendering/MathText/MathSpanWriter.cs") { Under = "Src/", Finds = [
        "            if (piece.Span.Kind != TextSpanKind.FractionNumerator) continue;",
        "            TextSpanKind.Superscript or TextSpanKind.FractionNumerator or TextSpanKind.OperatorSuperscript => Raise,"] };

    private static readonly OwnedRule MathSizesOfTheFontSet = new(
        "every size and place of math is a value of its font set's MathConstants: the math layout types no constant of an em of its own, nor a share of an x-height (the grammar's space width is the generated table's)",
        @"\bconst\s+double\b|\b\d+\.\d+\s*\*\s*(?:\w+\.)?[xX]Height\b",
        "Src/MatPlotLibNet/Rendering/MathText/MathConstants.cs",
        "Src/MatPlotLibNet/Rendering/MathText/MathGrammar.Data.cs") { Under = "Src/MatPlotLibNet/Rendering/MathText/", Finds = [
        "    private const double Raise = 0.40;",
        "    private const double Drop = 0.20;",
        "    internal const double ScriptScale = 0.70;",
        "    internal const double FractionScale = 0.70;",
        "    private protected const double Scale = 1.4;",
        "        double target = height + depth, least = target - 0.2 * style.XHeight;",
        "        if (own <= 0 || own >= target - 0.2 * style.XHeight) return natural;"] };

    private static readonly OwnedRule MathFaceOfTheSetNotTheLabel = new(
        "a math font set names the face of every character it sets, and what matplotlib takes from STIX is set in a STIX face (StixFallback, composed by the sets that fall back to it): only the base of the sets gives a character no face holds the label's face, so no leaf set answers with it",
        @"MathFace\.Label\b|static MathFace Label\b",
        "Src/MatPlotLibNet/Rendering/MathText/MathFontSet.cs",
        "Src/MatPlotLibNet/Rendering/MathText/MathRun.cs") { Under = "Src/MatPlotLibNet/", Finds = [
        "        return Table.TryGetValue(c, out var glyph) ? glyph : new(MathFace.Label, c);",
        "        FontVariant.BlackboardBold => MathFace.Label,",
        "            _ => MathFace.Label,"] };

    private static readonly OwnedRule StixNamedByItsOwner = new(
        "the STIX faces (their family names and the glyph each character is set as) are one table, StixFallback: no set, surface or layout names a STIX family of its own",
        @"STIX(?:General|NonUnicode|Size)",
        "Src/MatPlotLibNet/Rendering/MathText/StixFallback.cs") { Under = "Src/MatPlotLibNet/", Finds = [
        "    private static readonly MathFace Roman = new(new Font { Family = \"STIXGeneral\" });",
        "        => new Font { Family = \"STIXNonUnicode\", Slant = FontSlant.Italic };"] };

    private static readonly OwnedRule MathFacesAreFixedValues = OwnedRule.Nowhere(
        "a math font set keeps no store of the process that a draw fills (a memo of the runs a span was cut into, a dictionary of glyphs or faces made on use): its faces are fixed values and its tables are frozen when the type is made, so two figures on two threads share nothing a draw writes",
        @"\bstatic\b[^;=(]*\b(?:Dictionary|ConcurrentDictionary|ConditionalWeakTable|List|HashSet)<[^;]*\b(?:MathGlyph|MathRuns?|MathFace|StixRow|StixGlyph)\b",
        under: "Src/MatPlotLibNet/Rendering/MathText/") with { Finds = [
        "    private static readonly ConditionalWeakTable<TextSpan, MathRuns> Runs = new();",
        "    private static readonly ConcurrentDictionary<char, MathGlyph> Glyphs = new();",
        "    private static Dictionary<FontVariant, MathFace> Faces = [];"], Ignores = [
        "    private static readonly FrozenDictionary<char, MathGlyph> Table = TableOfFaces();",
        "    private static readonly MathFace Roman = Face(\"cmr10\");",
        "    private static readonly Dictionary<double, KernBox> Shared = MathGrammar.Spaces.Values.ToDictionary(w => w, w => new KernBox(w));"] };

    private const string MathTextFolder = "Src/MatPlotLibNet/Rendering/MathText/";

    private static readonly OwnedRule SymbolCharacters = new(
        "the character a math name stands for is written in the generated grammar table: no other file of the math text has a table of names and characters",
        @"^\s*\[\s*""[^""]*""\s*\]\s*=\s*(?:new\()?""",
        MathTextFolder + "MathGrammar.Data.cs") { Under = MathTextFolder, Finds = [
        "        [\"pm\"]      = \"±\", // ±",
        "        [\"pm\"] = new(\"±\", MathSymbolRole.Binary),"] };

    private static readonly OwnedRule NamesFromTheGrammar = OwnedRule.Nowhere(
        "a space, an accent, a function name and a large operator are registered from the grammar table: no command name of those families is typed by hand",
        @"new Math(?:Spacing|Accent|OperatorName|FunctionName|LargeOperator)\([^)]*\)\s*,\s*""",
        under: MathTextFolder) with { Finds = [
        "        Add(new MathAccent(\"̂\"), \"hat\");",
        "        Add(new MathOperatorName(), \"lim\", \"max\");"] };

    private static readonly OwnedRule ComputerModernGlyphs = new(
        "the face and code of a math character in the Computer Modern faces, the STIX glyph it is set as where a font set falls back to STIX, and the sizes a delimiter is chosen among, are written in the generated tables, read from matplotlib's own parse under its classic and its default style",
        @"^\s*\[\s*'[^']+'\s*\]\s*=\s*new\(",
        MathTextFolder + "ComputerModernMathFontSet.Data.cs", MathTextFolder + "StixFallback.Data.cs",
        MathTextFolder + "DejaVuSansMathFontSet.Data.cs") { Under = MathTextFolder, Finds = [
        "        ['α'] = new(MathItalic, '®'), // \\alpha"] };

    /// <summary>The rules of the area, in the order the scan reads them.</summary>
    internal static OwnedRule[] All { get; } =
    [
        MathParsedByTheLabel,
        MathLaidOutOnce,
        SpanKindsAtTheBoundary,
        MathSizesOfTheFontSet,
        MathFaceOfTheSetNotTheLabel,
        StixNamedByItsOwner,
        MathFacesAreFixedValues,
        SymbolCharacters,
        NamesFromTheGrammar,
        ComputerModernGlyphs,
    ];
}
