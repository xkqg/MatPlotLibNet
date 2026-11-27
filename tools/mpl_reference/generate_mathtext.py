"""
MatPlotLibNet math text grammar generator.

Writes the five files that hold matplotlib's mathtext grammar for the library:

  Src/MatPlotLibNet/Rendering/MathText/MathGrammar.Data.cs
      the tables the parser reads, as matplotlib's own parser holds them (matplotlib._mathtext_data.tex2uni and the tables of
      matplotlib._mathtext.Parser): every symbol name and its character, the function names and the ones that take limits,
      the accents and the mark each sets, the spaces and their widths in ems, the large operators, and the sets of symbols
      matplotlib puts space around (binary operators, relations, arrows) or after (punctuation). An accent matplotlib draws
      with a spacing character (\\mathring a ring operator, \\overleftarrow and \\overrightarrow an arrow) is written as the
      combining mark of that shape, because the library sets an accent as a combining mark after its letter. The library keeps
      a few names matplotlib has not (the Greek capitals that look Latin, \\omicron, \\gets, \\lnot, \\land, \\lor, \\dprime,
      the \\mathbb{R} names of its first table, \\check as an accent): they are listed
      below with their reason and written apart, so the test can tell them from matplotlib's.

  Src/MatPlotLibNet/Rendering/MathText/ComputerModernMathFontSet.Data.cs
      the face and the code of every character the parser can put in math that matplotlib's 'cm' font set (its classic
      style) sets in a Computer Modern face, read by parsing the character's name with matplotlib's own MathTextParser under
      mathtext.fontset 'cm' and reading the font and the code of the glyph it set. A character matplotlib takes from STIX is
      written as a comment: StixFallback sets it (or, when no face holds it, the label's face).

  Src/MatPlotLibNet/Rendering/MathText/StixFallback.Data.cs
      the STIX glyph matplotlib takes for each character the parser can put in math, for each font switch, under the two
      font sets that fall back to STIX ('cm': BakomaFonts falls back to StixFonts, 'dejavusans': DejaVuFonts falls back to
      StixSansFonts), read by parsing the character under the set and keeping the glyphs a STIX face set: the face, and the
      code of the character in it (a switch can set a letter as a mathematical alphanumeric or a private-use code of
      STIXNonUnicode).

  Src/MatPlotLibNet/Rendering/MathText/DejaVuSansMathFontSet.Data.cs (and the sized-alternatives table of
  ComputerModernMathFontSet.Data.cs)
      the glyphs matplotlib's AutoHeightChar chooses among to grow a delimiter or a radical sign to the height of what it stands
      beside, for each such character, read from the font set's get_sized_alternatives_for_symbol: for 'dejavusans' DejaVu Sans
      then the five STIXSize faces, for 'cm' the Computer Modern faces' own sizes.

  Tst/MatPlotLibNet/Rendering/MathText/matplotlib-mathtext.json
      what matplotlib's parser answers, for the test that holds the library's grammar to it: every symbol name, the character
      each is set as where a parse of the name alone under mathtext.fontset 'dejavusans' sets one glyph of a Unicode face (read
      from the parse, not from the table above, so a mistake in the table is seen), and the other tables as the parser holds them.

Usage (from anywhere; needs matplotlib 3.11.2):
    python generate_mathtext.py              # writes the five files in place
    python generate_mathtext.py --out <dir>  # writes them into <dir>, to compare with what is checked in
"""

import inspect
import json
import re
import sys
import unicodedata
from pathlib import Path

import matplotlib

matplotlib.use("Agg")
from matplotlib import rcParams  # noqa: E402
from matplotlib import _mathtext as mathtext  # noqa: E402
from matplotlib import _mathtext_data as mathtext_data  # noqa: E402
from matplotlib.mathtext import MathTextParser  # noqa: E402

ROOT = Path(__file__).parent.parent.parent
GRAMMAR_CS = ROOT / "Src" / "MatPlotLibNet" / "Rendering" / "MathText" / "MathGrammar.Data.cs"
COMPUTER_MODERN_CS = ROOT / "Src" / "MatPlotLibNet" / "Rendering" / "MathText" / "ComputerModernMathFontSet.Data.cs"
STIX_CS = ROOT / "Src" / "MatPlotLibNet" / "Rendering" / "MathText" / "StixFallback.Data.cs"
DEJAVU_CS = ROOT / "Src" / "MatPlotLibNet" / "Rendering" / "MathText" / "DejaVuSansMathFontSet.Data.cs"
REFERENCE_JSON = ROOT / "Tst" / "MatPlotLibNet" / "Rendering" / "MathText" / "matplotlib-mathtext.json"

VERSION = "3.11.2"
HEADER = [
    "// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.",
    "// Licensed under the MIT License. See LICENSE file in the project root for full license information.",
    "",
]
Parser = mathtext.Parser
TEX2UNI = mathtext_data.tex2uni

# Names the library keeps that matplotlib has not, with their character and the reason.
LIBRARY_SYMBOLS = {
    # The Greek capitals that look like Latin ones, and omicron: the library's first table had all 48 Greek letters.
    "Alpha": 0x0391, "Beta": 0x0392, "Epsilon": 0x0395, "Zeta": 0x0396, "Eta": 0x0397, "Iota": 0x0399,
    "Kappa": 0x039A, "Mu": 0x039C, "Nu": 0x039D, "Omicron": 0x039F, "Rho": 0x03A1, "Tau": 0x03A4,
    "Chi": 0x03A7, "omicron": 0x03BF,
    # TeX names of signs matplotlib spells otherwise (\leftarrow, \neg, \wedge, \vee, a double prime).
    "gets": 0x2190, "lnot": 0x00AC, "land": 0x2227, "lor": 0x2228, "dprime": 0x2033,
    # The double-struck capitals the first table named by their font switch; MathSymbols.TryGet answers them.
    "mathbb{R}": 0x211D, "mathbb{C}": 0x2102, "mathbb{Z}": 0x2124, "mathbb{N}": 0x2115, "mathbb{Q}": 0x211A,
}

# Accents the library keeps that matplotlib reads as something else (\check a symbol).
LIBRARY_ACCENTS = {"check": 0x030C}

# The font switches of the library's FontVariant (RichText.cs), in the order of the enum, and how each is written in math.
STIX_VARIANTS = [("Default", "{}"), ("Roman", r"\mathrm{{{}}}"), ("Bold", r"\mathbf{{{}}}"), ("Italic", r"\mathit{{{}}}"),
                 ("Calligraphic", r"\mathcal{{{}}}"), ("BlackboardBold", r"\mathbb{{{}}}")]
# The STIX faces a glyph of matplotlib's fallback can be set in, as (family, style) -> StixFace member; a new one stops the generator.
STIX_FACES = {("STIXGeneral", "Regular"): "Regular", ("STIXGeneral", "Italic"): "Italic", ("STIXGeneral", "Bold"): "Bold",
              ("STIXNonUnicode", "Regular"): "NonUnicode", ("STIXNonUnicode", "Italic"): "NonUnicodeItalic",
              ("STIXSizeOneSym", "Regular"): "SizeOne", ("STIXSizeTwoSym", "Regular"): "SizeTwo",
              ("STIXSizeThreeSym", "Regular"): "SizeThree", ("STIXSizeFourSym", "Regular"): "SizeFour"}
# How each host names its own faces in C# (ComputerModernMathFontSet, DejaVuSansMathFontSet), as (family, style) -> member.
CM_FACES = {("cmr10", "Regular"): "Roman", ("cmmi10", "Regular"): "MathItalic", ("cmti10", "Regular"): "TextItalic",
            ("cmb10", "Regular"): "Bold", ("cmsy10", "Regular"): "Symbols", ("cmex10", "Regular"): "Extension",
            ("cmtt10", "Regular"): "Typewriter"}
DEJAVU_FACES = {("DejaVu Sans", "Book"): "Book", ("DejaVu Sans", "Oblique"): "Oblique", ("DejaVu Sans", "Bold"): "Bold",
                ("DejaVu Sans Display", "Display"): "Display", ("cmsy10", "Regular"): "ComputerModernMathFontSet.Symbols"}
STIX_HOSTS = [("cm", "ComputerModernRows"), ("dejavusans", "DejaVuSansRows")]
STIX_SHORT = {"Regular": "R", "Italic": "I", "Bold": "B", "NonUnicode": "N", "NonUnicodeItalic": "NI"}

# Characters a backslash escapes in the library that matplotlib does not read: an ampersand splits the cells of a matrix, so
# \& is an ampersand in a cell.
LIBRARY_ESCAPES = "&"

# The combining mark of the shape matplotlib draws with a spacing character over the letter.
COMBINING_FORM = {0x2218: 0x030A, 0x2190: 0x20D6, 0x2192: 0x20D7}

LETTER_FAMILIES = {"frac", "dfrac", "binom", "genfrac", "sqrt", "overline", "underline", "overset", "underset", "substack", "phantom",
                   "llap", "rlap", "hspace", "operatorname", "mathrm", "text", "mathbf", "mathit", "mathcal", "mathbb", "left", "middle",
                   "right", "begin", "end"}


def strip(command: str) -> str:
    return command[1:] if command.startswith("\\") else command


def split_set(members):
    """A matplotlib set of symbols as its single characters (a string) and its command names (without the backslash)."""
    chars = "".join(sorted(m for m in members if len(m) == 1))
    names = sorted(strip(m) for m in members if m.startswith("\\"))
    return chars, names


def parse(fontset: str, text: str):
    rcParams["mathtext.fontset"] = fontset
    try:
        _, _, _, glyphs, _ = MathTextParser("path").parse(text, 72, None)
    except Exception:  # noqa: BLE001 - a name matplotlib cannot set alone
        return None
    return glyphs


def accent_mark(command: str) -> int:
    code = TEX2UNI[strip(command)]
    code = COMBINING_FORM.get(code, code)
    if unicodedata.category(chr(code)) != "Mn":
        sys.exit(f"accent {command} has no combining form for U+{code:04X}")
    return code


def cs_string(code: int) -> str:
    return f"\\u{code:04X}"


def cs_text(text: str) -> str:
    """A C# string literal's body: a backslash and a quote escaped, a character outside ASCII as its \\u escape."""
    escaped = text.replace("\\", "\\\\").replace('"', '\\"').replace("'", "\\'")
    return "".join(c if ord(c) < 0x7F else cs_string(ord(c)) for c in escaped)


def frozen_set(name: str, doc: str, values) -> list[str]:
    items = [f'"{cs_text(v)}"' for v in values]
    rows = [", ".join(items[i:i + 8]) for i in range(0, len(items), 8)]
    return [
        f"    /// <summary>{doc}</summary>",
        f"    internal static readonly FrozenSet<string> {name} = FrozenSet.ToFrozenSet(",
        "    [",
        *[f"        {row}," for row in rows],
        "    ], StringComparer.Ordinal);",
        "",
    ]


def main() -> None:
    global GRAMMAR_CS, COMPUTER_MODERN_CS, STIX_CS, DEJAVU_CS, REFERENCE_JSON
    if len(sys.argv) == 3 and sys.argv[1] == "--out":
        out = Path(sys.argv[2])
        out.mkdir(parents=True, exist_ok=True)
        GRAMMAR_CS, COMPUTER_MODERN_CS, STIX_CS, DEJAVU_CS, REFERENCE_JSON = (out / p.name for p in (GRAMMAR_CS, COMPUTER_MODERN_CS, STIX_CS, DEJAVU_CS, REFERENCE_JSON))
    if matplotlib.__version__ != VERSION:
        sys.exit(f"matplotlib {VERSION} is needed, found {matplotlib.__version__}")

    # ---- the tables as matplotlib's parser holds them -------------------------------------------------------------------
    symbols = {name: TEX2UNI[name] for name in sorted(TEX2UNI, key=lambda n: (n.lower(), n))}
    clash = set(symbols) & set(LIBRARY_SYMBOLS)
    if clash:
        sys.exit(f"library names matplotlib has: {sorted(clash)}")
    functions = sorted(Parser._function_names)
    limit_functions = sorted(Parser._overunder_functions)
    accents = {name: accent_mark(command) for name, command in Parser._accent_map.items()}
    for wide in Parser._wide_accents:
        accents[wide] = accent_mark(wide)
    spaces = {}
    literal_spaces = {}
    for command, width in Parser._space_widths.items():
        if command.startswith("\\"):
            spaces[command[1:]] = width
        else:
            literal_spaces[command] = width
    limit_operators = sorted(strip(s) for s in Parser._overunder_symbols)
    large_operators = sorted(strip(s) for s in Parser._overunder_symbols | Parser._dropsub_symbols if strip(s) in TEX2UNI)
    spaced_chars, spaced_names = split_set(Parser._spaced_symbols)
    binary_chars, binary_names = split_set(Parser._binary_operators)
    punctuation_chars, punctuation_names = split_set(Parser._punctuation_symbols)
    relation_chars, _ = split_set(Parser._relation_symbols)
    left_chars, _ = split_set(Parser._left_delims)
    delimiter_chars, _ = split_set(Parser._delims)
    # The space Parser.symbol puts around a spaced symbol and after punctuation: the one width its code passes to _make_space.
    symbol_spaces = set(re.findall(r"_make_space\(([\d.]+)\)", inspect.getsource(Parser.symbol)))
    if len(symbol_spaces) != 1:
        sys.exit(f"Parser.symbol makes spaces of several widths: {symbol_spaces}")
    symbol_space = float(symbol_spaces.pop())
    unspaced_after = "".join(sorted(set("{" + left_chars + relation_chars)))
    # What a symbol name is to the spacing: a name in both the binary operators and the relations is a binary operator, as
    # Parser.symbol reads it (it tests _binary_operators first).
    roles = {}
    for name in punctuation_names:
        roles[name] = "Punctuation"
    for name in spaced_names:
        roles[name] = "Relation"
    for name in binary_names:
        roles[name] = "Binary"
    # A name of those sets that tex2uni has not (\ldotp) is no symbol matplotlib can set ("Unknown symbol"): it has no role here.
    roles = {name: role for name, role in roles.items() if name in symbols}
    function_followers = "".join(sorted(set(delimiter_chars + "^_")))

    # The characters a backslash escapes in matplotlib's symbol grammar (\$ is a dollar sign): every ASCII punctuation mark
    # that parses after a backslash to that one character under the default font set.
    escaped = "".join(sorted(c for c in map(chr, range(0x21, 0x7F))
                             if not c.isalnum() and c != "\\"
                             and (g := parse("dejavusans", f"$\\{c}$")) is not None and len(g) == 1 and g[0][2] == ord(c)))

    overlap = sorted(set(functions) & set(symbols))
    if overlap:
        sys.exit(f"function names that are symbols too: {overlap}")

    # ---- the STIX glyphs matplotlib falls back to ---------------------------------------------------------------------------
    # Every character the library's grammar can put in a math span: ASCII, the characters of matplotlib's symbol names, the
    # library's own. Each is parsed alone under each font set that falls back to STIX, in each font switch, and a glyph set in a
    # STIX face is kept (a glyph set in the font set's own face is not: the set holds it). A character outside the Basic
    # Multilingual Plane is not a key of a C# char table; none is named by the grammar.
    stix_bodies = {}
    for char in map(chr, range(0x21, 0x7F)):
        stix_bodies[char] = [char] if char.isalnum() else [char, "\\" + char]
    for name, code in {**symbols, **LIBRARY_SYMBOLS}.items():
        if name in accents:
            continue
        char = chr(code)
        if ord(char) > 0xFFFF:
            sys.exit(f"{name} is outside the Basic Multilingual Plane: StixFallback keys its rows by char")
        bodies = stix_bodies.setdefault(char, [])
        body = "\\" + name if name in TEX2UNI else char
        if body not in bodies:
            bodies.append(body)

    def stix_glyph(host, wrapper, bodies):
        for body in bodies:
            glyphs = parse(host, "$" + wrapper.format(body) + "$")
            if glyphs is None or len(glyphs) != 1:
                continue
            font, code = glyphs[0][0], glyphs[0][2]
            if not font.family_name.startswith("STIX"):
                return None
            face = STIX_FACES.get((font.family_name, font.style_name))
            if face is None:
                sys.exit(f"a STIX face the library has no member for: {font.family_name} {font.style_name} ({body})")
            return face, code
        return None

    stix = {}
    for host, _ in STIX_HOSTS:
        rows = {}
        for char, bodies in sorted(stix_bodies.items()):
            row = [stix_glyph(host, wrapper, bodies) for _, wrapper in STIX_VARIANTS]
            if any(row):
                rows[char] = row
        stix[host] = rows

    # ---- the sizes a delimiter or a radical sign is chosen among --------------------------------------------------------------
    # matplotlib's AutoHeightChar walks the font set's sized alternatives of a character, size 0 first, takes the first that is tall
    # enough and sets it at the size that makes it exactly as tall as what it stands beside. The alternatives are read from
    # get_sized_alternatives_for_symbol, each as the face and the code _get_glyph sets it in. Keyed by the character the library's
    # \left, \middle and \right (and a radical) hand its layout, as MathDelimiter.GlyphOf makes it.
    from matplotlib.backends.backend_agg import get_hinting_flag
    from matplotlib.font_manager import FontProperties

    def delimiter_char(token):
        if token.startswith("\\"):
            name = token[1:]
            if len(name) == 1 and name in "#$%_{}&":
                return name
            return chr(TEX2UNI[name])
        return token

    delimiter_tokens = sorted(set(Parser._delims) - {"."}) + ["\\__sqrt__"]
    sizes = {}
    for host, fonts_class in (("cm", mathtext.BakomaFonts), ("dejavusans", mathtext.DejaVuSansFonts)):
        rcParams["mathtext.fontset"] = host
        fonts = fonts_class(FontProperties(size=12), get_hinting_flag())
        rows = {}
        for token in delimiter_tokens:
            char = delimiter_char(token)
            if ord(char) > 0xFFFF:
                sys.exit(f"{token} is outside the Basic Multilingual Plane")
            glyphs = []
            alternatives = fonts.get_sized_alternatives_for_symbol("it", token)
            for font_name, symbol in alternatives:
                font, code, _ = fonts._get_glyph(font_name, "it", symbol)
                if code > 0xFFFF:
                    sys.exit(f"a size of {token} is outside the Basic Multilingual Plane: SizedGlyph reads a size as one char")
                glyphs.append((font.family_name, font.style_name, code))
            # AutoHeightChar sets a size by the factor that makes it as tall as asked, except size '0' of a set that has other sizes
            # (`if state.font != '0' or len(alternatives) == 1`): that one keeps its own size.
            natural = len(alternatives) > 1 and alternatives[0][0] == "0"
            # two spellings of one character (`\lbrack` and `[`) differ where matplotlib lists sizes for one and not the other: the
            # spelling with sizes is the character's, as the library reads both as the one character
            if char not in rows or len(glyphs) > len(rows[char][0]):
                rows[char] = (glyphs, natural)
        sizes[host] = rows

    def size_face(host, family, style):
        if (family, style) in STIX_FACES:
            return f"StixFallback.FaceOf(StixFace.{STIX_FACES[(family, style)]})"
        table = CM_FACES if host == "cm" else DEJAVU_FACES
        if (family, style) not in table:
            sys.exit(f"a face of a sized alternative the library has no name for under {host}: {family} {style}")
        return table[(family, style)]

    def size_rows(host):
        lines = []
        for char, (glyphs, natural) in sizes[host].items():
            cells = ", ".join(f"new({size_face(host, family, style)}, 0x{code:X})" for family, style, code in glyphs)
            lines.append(f"        ['{cs_string(ord(char))}'] = new([{cells}], {'true' if natural else 'false'}),")
        return lines

    # ---- what the parser answers, for the test --------------------------------------------------------------------------
    answered = {}
    for name in symbols:
        if name in accents:
            continue  # an accent needs its letter; the accent is read before the symbol, in matplotlib and here
        glyphs = parse("dejavusans", f"$\\{name}$")
        # One glyph from a face that codes characters as Unicode does (the prime comes from cmsy10, coded by TeX).
        if glyphs is not None and len(glyphs) == 1 and not glyphs[0][0].family_name.startswith("cm"):
            answered[name] = glyphs[0][2]
    reference = {
        "matplotlib": VERSION,
        "symbols": answered,
        "symbolNames": sorted(symbols),
        "functionNames": functions,
        "limitFunctions": limit_functions,
        "accents": sorted(list(Parser._accent_map) + sorted(Parser._wide_accents)),
        "spaces": {command: width for command, width in Parser._space_widths.items()},
        "largeOperators": large_operators,
        "limitOperators": limit_operators,
        "spacedCharacters": spaced_chars,
        "binaryCharacters": binary_chars,
        "punctuationCharacters": punctuation_chars,
        "escapedCharacters": escaped,
        "symbolSpace": symbol_space,
        "libraryNames": sorted(LIBRARY_SYMBOLS),
        "libraryAccents": sorted(LIBRARY_ACCENTS),
        "libraryEscapes": LIBRARY_ESCAPES,
        # per font set, per character: its STIX glyph in each font switch (the order of FontVariant), "Face:code" or empty
        "stixFallback": {host: {f"{ord(char):04X}": ",".join("" if g is None else f"{g[0]}:{g[1]:X}" for g in row)
                                for char, row in rows.items()} for host, rows in stix.items()},
        # per font set, per delimiter character: the faces and codes it is chosen among, "Family/Style:code" in order of size
        # (a leading * marks a first size that keeps its own size)
        "sizedAlternatives": {host: {f"{ord(char):04X}": [("*" if natural and i == 0 else "") + f"{f}/{st}:{c:X}"
                                                         for i, (f, st, c) in enumerate(glyphs)] for char, (glyphs, natural) in rows.items()}
                              for host, rows in sizes.items()},
    }
    REFERENCE_JSON.write_text(json.dumps(reference, indent=1, sort_keys=True) + "\n", encoding="utf-8", newline="\n")

    # ---- the grammar's C# data ------------------------------------------------------------------------------------------
    lines = [
        *HEADER,
        f"// Written by tools/mpl_reference/generate_mathtext.py from matplotlib {VERSION}'s mathtext parser",
        "// (matplotlib._mathtext_data.tex2uni and the tables of matplotlib._mathtext.Parser): change the tables by running it again;",
        "// MathGrammarContractTests tells when they and matplotlib's parser disagree.",
        "",
        "using System.Collections.Frozen;",
        "",
        "namespace MatPlotLibNet.Rendering.MathText;",
        "",
        "internal static partial class MathGrammar",
        "{",
        "    /// <summary>The matplotlib release the tables were read from.</summary>",
        f'    internal const string MatplotlibVersion = "{VERSION}";',
        "",
        f"    /// <summary>Every symbol name, the character it is set as and what it is to the spacing of math: matplotlib's",
        f"    /// {len(symbols)} (<c>tex2uni</c>; the role from <c>Parser._binary_operators</c>, <c>_relation_symbols</c> and",
        f"    /// <c>_arrow_symbols</c>, <c>_punctuation_symbols</c>), then the {len(LIBRARY_SYMBOLS)} the library keeps that matplotlib has",
        "    /// not (<see cref=\"LibrarySymbols\"/>).</summary>",
        "    internal static readonly FrozenDictionary<string, MathSymbolEntry> Symbols = new Dictionary<string, MathSymbolEntry>(StringComparer.Ordinal)",
        "    {",
    ]

    def entry(name, code):
        role = roles.get(name)
        return f'        ["{cs_text(name)}"] = new("{cs_string(code)}"' + (f", MathSymbolRole.{role})," if role else "),")

    lines += [entry(n, c) for n, c in symbols.items()]
    lines += ["        // the library's own names"]
    lines += [entry(n, c) for n, c in LIBRARY_SYMBOLS.items()]
    lines += ["    }.ToFrozenDictionary(StringComparer.Ordinal);", ""]
    lines += frozen_set("LibrarySymbols", "The names of <see cref=\"Symbols\"/> matplotlib has not.", LIBRARY_SYMBOLS)
    lines += frozen_set("FunctionNames", f"The {len(functions)} names matplotlib sets as a function (<c>\\sin</c>, <c>\\lim</c>): upright, as a word.", functions)
    lines += frozen_set("LimitFunctions", "The function names whose scripts are limits, set below and above the name.", limit_functions)
    lines += [
        f"    /// <summary>Every accent and the combining mark it sets over its letter: matplotlib's {len(accents)}, then the",
        f"    /// {len(LIBRARY_ACCENTS)} the library keeps (<see cref=\"LibraryAccents\"/>).</summary>",
        "    internal static readonly FrozenDictionary<string, string> Accents = new Dictionary<string, string>(StringComparer.Ordinal)",
        "    {",
    ]
    lines += [f'        ["{cs_text(n)}"] = "{cs_string(c)}",' for n, c in sorted(accents.items())]
    lines += ["        // the library's own accents"]
    lines += [f'        ["{cs_text(n)}"] = "{cs_string(c)}",' for n, c in LIBRARY_ACCENTS.items()]
    lines += ["    }.ToFrozenDictionary(StringComparer.Ordinal);", ""]
    lines += frozen_set("LibraryAccents", "The names of <see cref=\"Accents\"/> matplotlib has not as an accent.", LIBRARY_ACCENTS)
    lines += [
        "    /// <summary>Every space command (the name after the backslash) and its width in ems; a negative width takes room away.</summary>",
        "    internal static readonly FrozenDictionary<string, double> Spaces = new Dictionary<string, double>(StringComparer.Ordinal)",
        "    {",
    ]
    lines += [f'        ["{cs_text(n)}"] = {w!r},' for n, w in spaces.items()]
    lines += ["    }.ToFrozenDictionary(StringComparer.Ordinal);", ""]
    lines += [
        "    /// <summary>Every character that is a space in math (<c>~</c>, a space that does not break) and its width in ems.</summary>",
        "    internal static readonly FrozenDictionary<char, double> LiteralSpaces = new Dictionary<char, double>",
        "    {",
    ]
    lines += [f"        ['{cs_text(n)}'] = {w!r}," for n, w in literal_spaces.items()]
    lines += ["    }.ToFrozenDictionary();", ""]
    lines += frozen_set("LargeOperators", "The symbols set larger than the text: the sums and products and the integrals.", large_operators)
    lines += frozen_set("LimitOperators", "The large operators whose scripts are limits (the sums and products, not the integrals).", limit_operators)
    lines += [
        "    /// <summary>The width, in ems, of the space matplotlib puts around a binary operator, a relation and an arrow, and after",
        "    /// punctuation (<c>Parser.symbol</c>).</summary>",
        f"    internal const double SymbolSpaceWidth = {symbol_space!r};",
        "",
        "    /// <summary>The characters matplotlib puts space around: its binary operators, relations and arrows.</summary>",
        f'    internal const string SpacedCharacters = "{cs_text(spaced_chars)}";',
        "",
        "    /// <summary>The characters that are binary operators, set without space at the start or after an opening.</summary>",
        f'    internal const string BinaryCharacters = "{cs_text(binary_chars)}";',
        "",
        "    /// <summary>The punctuation characters matplotlib puts space after.</summary>",
        f'    internal const string PunctuationCharacters = "{cs_text(punctuation_chars)}";',
        "",
    ]
    lines += [
        "    /// <summary>The characters after which a binary operator takes no space: an opening brace, an opening delimiter and a",
        "    /// relation.</summary>",
        f'    internal const string UnspacedAfter = "{cs_text(unspaced_after)}";',
        "",
        "    /// <summary>The characters that, written right after a function name, take the thin space after it away: a delimiter, a",
        "    /// superscript or a subscript.</summary>",
        f'    internal const string FunctionFollowers = "{cs_text(function_followers)}";',
        "",
        "    /// <summary>The characters a backslash escapes: <c>\\$</c> is a dollar sign, not the end of math.</summary>",
        f'    internal const string EscapedCharacters = "{cs_text("".join(sorted(escaped + LIBRARY_ESCAPES)))}";',
        "}",
    ]
    GRAMMAR_CS.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")

    # ---- the Computer Modern faces ----------------------------------------------------------------------------------------
    faces = {"cmr10": "Roman", "cmmi10": "MathItalic", "cmti10": "TextItalic", "cmb10": "Bold", "cmsy10": "Symbols",
             "cmex10": "Extension", "cmtt10": "Typewriter"}
    table = {}
    comments = {}
    sources = [(c, f"${c}$", "ASCII") for c in map(chr, range(0x21, 0x7F)) if not c.isalnum()]
    sources += [(chr(code), f"$\\{name}$", f"\\{name}") for name, code in symbols.items() if name not in accents]
    sources += [(chr(code), None, f"\\{name}") for name, code in LIBRARY_SYMBOLS.items()]
    for char, text, label in sources:
        if char in table or char.isascii() and char.isalnum():
            continue
        glyphs = parse("cm", text) if text else None
        if glyphs is None and label == "ASCII":
            glyphs = parse("cm", f"$\\{char}$")
        if glyphs is not None and len(glyphs) == 1 and glyphs[0][0].family_name in faces:
            table[char] = (faces[glyphs[0][0].family_name], glyphs[0][2], label)
            comments.pop(char, None)
        elif char not in comments:
            comments[char] = label
    cm = [
        *HEADER,
        f"// Written by tools/mpl_reference/generate_mathtext.py from matplotlib {VERSION}: each character's name parsed with",
        "// matplotlib's own MathTextParser under mathtext.fontset 'cm' (its classic style), and the font and code of the glyph it",
        "// set read: change the table by running it again.",
        "",
        "using System.Collections.Frozen;",
        "",
        "namespace MatPlotLibNet.Rendering.MathText;",
        "",
        "internal sealed partial class ComputerModernMathFontSet",
        "{",
        "    /// <summary>Every character but an ASCII letter or digit that the parser can put in a math span and Computer Modern holds: its",
        "    /// face and its code there. A character matplotlib takes from STIX is a comment: StixFallback sets it. A method, so the",
        "    /// table is made after the faces it reads (the initialisers of two parts of a class run in no set order).</summary>",
        "    private static FrozenDictionary<char, MathGlyph> TableOfFaces() => new Dictionary<char, MathGlyph>",
        "    {",
    ]
    for char in sorted(set(table) | set(comments)):
        if char in table:
            face, code, label = table[char]
            cm.append(f"        ['{cs_string(ord(char))}'] = new({face}, '{cs_string(code)}'), // {label}, {face} 0x{code:02x}")
        else:
            where = "StixFallback sets it" if char in stix["cm"] and stix["cm"][char][0] else "no face holds it, the label's face"
            cm.append(f"        // {comments[char]} (U+{ord(char):04X}): not in Computer Modern, {where}")
    cm += [
        "    }.ToFrozenDictionary();",
        "",
        "    /// <summary>The glyphs matplotlib's AutoHeightChar chooses among to grow each delimiter and the radical sign under 'cm', size 0",
        "    /// first (<c>BakomaFonts.get_sized_alternatives_for_symbol</c>), by the character the layout hands it.</summary>",
        "    private static FrozenDictionary<char, MathSizes> SizedTable() => new Dictionary<char, MathSizes>",
        "    {",
        *size_rows("cm"),
        "    }.ToFrozenDictionary();",
        "}",
    ]
    COMPUTER_MODERN_CS.write_text("\n".join(cm) + "\n", encoding="utf-8", newline="\n")

    # ---- StixFallback.Data.cs -------------------------------------------------------------------------------------------------
    def stix_cell(glyph):
        if glyph is None:
            return "default"
        face, code = glyph
        return f"{STIX_SHORT[face]}(0x{code:X})"

    sx = [
        *HEADER,
        f"// Written by tools/mpl_reference/generate_mathtext.py from matplotlib {VERSION}: each character parsed alone, in each font",
        "// switch, under mathtext.fontset 'cm' and 'dejavusans' (the two sets that fall back to STIX), and the STIX glyph read where",
        "// the parser set one: change the tables by running it again.",
        "",
        "using System.Collections.Frozen;",
        "",
        "namespace MatPlotLibNet.Rendering.MathText;",
        "",
        "internal sealed partial class StixFallback",
        "{",
    ]
    for host, method in STIX_HOSTS:
        sx += [
            f"    /// <summary>The STIX glyphs matplotlib takes under mathtext.fontset '{host}': {len(stix[host])} characters, each with its glyph",
            "    /// in the font switches whose parse set one in a STIX face (<c>default</c>: the set holds it itself).</summary>",
            f"    private static FrozenDictionary<char, StixRow> {method}() => new Dictionary<char, StixRow>",
            "    {",
        ]
        for char, row in stix[host].items():
            sx.append(f"        ['{cs_string(ord(char))}'] = new({', '.join(stix_cell(g) for g in row)}),")
        sx += ["    }.ToFrozenDictionary();", ""]
    sx[-1:] = ["}"]
    STIX_CS.write_text("\n".join(sx) + "\n", encoding="utf-8", newline="\n")

    dj = [
        *HEADER,
        f"// Written by tools/mpl_reference/generate_mathtext.py from matplotlib {VERSION}: the glyphs AutoHeightChar chooses among under",
        "// mathtext.fontset 'dejavusans', read from DejaVuSansFonts.get_sized_alternatives_for_symbol: change the table by running it again.",
        "",
        "using System.Collections.Frozen;",
        "",
        "namespace MatPlotLibNet.Rendering.MathText;",
        "",
        "internal sealed partial class DejaVuSansMathFontSet",
        "{",
        "    /// <summary>The glyphs matplotlib's AutoHeightChar chooses among to grow each delimiter and the radical sign under 'dejavusans',",
        "    /// size 0 first (DejaVu Sans, then the STIXSize faces that hold the character), by the character the layout hands it.</summary>",
        "    private static FrozenDictionary<char, MathSizes> SizedTable() => new Dictionary<char, MathSizes>",
        "    {",
        *size_rows("dejavusans"),
        "    }.ToFrozenDictionary();",
        "}",
    ]
    DEJAVU_CS.write_text("\n".join(dj) + "\n", encoding="utf-8", newline="\n")

    print(f"symbols {len(symbols)} + {len(LIBRARY_SYMBOLS)} library, functions {len(functions)}, accents {len(accents)} + "
          f"{len(LIBRARY_ACCENTS)}, spaces {len(spaces)} + {len(literal_spaces)}, large operators {len(large_operators)}, "
          f"Computer Modern {len(table)} characters, {len(comments)} not in it, STIX rows "
          f"{', '.join(f'{h} {len(r)}' for h, r in stix.items())}, escapes '{escaped}'")


if __name__ == "__main__":
    main()
