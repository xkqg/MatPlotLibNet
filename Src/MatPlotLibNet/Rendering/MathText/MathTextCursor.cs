// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>Where a math group stops being read: at the dollar sign that closes math, and before the closing brace of a group, the
/// <c>]</c> of a radical's index, the <c>\right</c> or <c>\middle</c> of a delimited group, the <c>&amp;</c>, <c>\\</c> or
/// <c>\end</c> of a matrix cell, or the <c>\\</c> or closing brace of a row of <c>\substack</c>.</summary>
internal enum MathStop
{
    /// <summary>The dollar sign that closes math, or the end of the text.</summary>
    Math,

    /// <summary>The closing brace of a group.</summary>
    Brace,

    /// <summary>The <c>]</c> that closes a radical's index.</summary>
    Bracket,

    /// <summary>The <c>\right</c> or <c>\middle</c> of a delimited group.</summary>
    Delimiter,

    /// <summary>The end of a matrix cell.</summary>
    Cell,

    /// <summary>The end of a row of <c>\substack</c>: a <c>\\</c> or the substack's closing brace.</summary>
    StackRow,
}

/// <summary>The parse of a label into its tree of boxes (<see cref="MathBox"/>), as matplotlib's mathtext grammar reads it: the text
/// outside <c>$...$</c> kept as written, the math inside read token by token into a row, a <c>\command</c> handed to the family the
/// command table names (<see cref="MathCommand"/>), which reads its own arguments, recursively, through the members here and adds its
/// box; a <c>^</c> or <c>_</c> takes the last box as its nucleus. A stack value: the rows it has open and the run it joins are written
/// on memory the thread keeps (<see cref="Rendering.MathText.MathTextCursor.MathParseScratch"/>), so the boxes of the tree, each row at its own length, are the parse's
/// only allocations.</summary>
internal ref partial struct MathTextCursor
{
    /// <summary>The characters a backslash escapes (<see cref="MathGrammar.EscapedCharacters"/>): <c>\$</c> is a dollar sign, not
    /// the end of math mode.</summary>
    internal static readonly SearchValues<char> Escaped = SearchValues.Create(MathGrammar.EscapedCharacters);

    /// <summary>The ASCII characters a row of math writes as they are: every one but a backslash, a typed space, a brace, a script
    /// mark, an apostrophe, a dollar sign, the ends of a cell and of an index, a space in math and a symbol matplotlib puts space around
    /// or after. A run is scanned for the first character not among them, so a character beyond ASCII stops the scan too and the scan
    /// stays on its vectorised ASCII path.</summary>
    private static readonly SearchValues<char> PlainAscii = SearchValues.Create(
        [.. Enumerable.Range(0, 128).Select(c => (char)c).Except(
            ['\\', ' ', '{', '}', '^', '_', '\'', '$', '&', ']', .. MathGrammar.LiteralSpaces.Keys, .. MathGrammar.SpacedCharacters,
             .. MathGrammar.PunctuationCharacters])]);

    /// <summary>The thin space (<c>\,</c>) matplotlib puts after a function name and after its scripts.</summary>
    private static readonly KernBox ThinSpace = KernBox.Of(MathGrammar.Spaces[","]);

    /// <summary>The space matplotlib puts on a side of an operator, a relation and punctuation.</summary>
    private static readonly KernBox SymbolSpace = KernBox.Of(MathGrammar.SymbolSpaceWidth);

    private readonly string _text;
    private readonly MathRows _rows;
    private MathPendingText _run;
    private int _position;
    private bool _inMath;
    private MathSource _math;
    private FontVariant _variant;

    /// <summary>Whether the scripts being read are a function name's (<see cref="AddFunctionName"/>): matplotlib puts no space around
    /// their symbols and a thin space after them.</summary>
    private bool _functionScripts;

    private MathTextCursor(string text, MathParseScratch scratch)
    {
        _text = text;
        _rows = scratch.Rows;
        _run = new MathPendingText(text, scratch.Text);
    }

    /// <summary>The tree of <paramref name="text"/>: its boxes side by side, the text outside <c>$...$</c> as written.</summary>
    /// <param name="text">The label.</param>
    /// <returns>The label's tree.</returns>
    internal static LineBox Parse(string text)
    {
        var scratch = MathParseScratch.Take();
        var cursor = new MathTextCursor(text, scratch);
        var rows = cursor._rows;
        var line = rows.Open();
        while (!cursor.AtEnd)
        {
            char c = text[cursor._position];
            if (c == '$')
            {
                cursor.FlushRun();
                cursor._inMath = !cursor._inMath;
                cursor._position++;
                rows.StartMath();
                if (cursor._inMath) cursor._math = new MathSource(text, cursor._position);
            }
            else if (!cursor._inMath)
            {
                var rest = text.AsSpan(cursor._position);
                int length = rest.IndexOf('$');
                if (length < 0) length = rest.Length;
                cursor._run.AppendSource(cursor._position, length);
                cursor._position += length;
            }
            else
            {
                cursor.ReadStep(single: false);
            }
        }

        cursor.FlushRun();
        var tree = new LineBox(text, rows.Close(line));
        scratch.Return();
        return tree;
    }

    /// <summary>Whether the whole text has been read.</summary>
    internal readonly bool AtEnd => _position >= _text.Length;

    /// <summary>The math the cursor stands in, which the space around a symbol is read from.</summary>
    internal readonly MathSource Source => _math;

    /// <summary>Where the cursor stands in the text.</summary>
    internal readonly int Position => _position;

    /// <summary>The character at the position, or <see langword="null"/> at the end.</summary>
    internal readonly char? Peek => AtEnd ? null : _text[_position];

    /// <summary>Reads a row of math up to <paramref name="stop"/>, which is left unread.</summary>
    /// <param name="stop">Where the row ends.</param>
    /// <param name="group">Whether the row is a group, kept as a row whatever it holds (a braced group); else a row of one box is that
    /// box and an empty row is <see cref="RowBox.Empty"/>.</param>
    /// <returns>The row as one box.</returns>
    internal MathBox ReadRow(MathStop stop, bool group = false)
    {
        FlushRun();
        var outer = _rows.Open();
        while (!AtEnd && !At(stop)) ReadStep(single: false);
        FlushRun();
        return group ? new RowBox(_rows.Close(outer)) : _rows.CloseAsBox(outer);
    }

    /// <summary>Reads a braced group, the cursor on its <c>{</c>: the row up to its <c>}</c>, kept as a row, and the <c>}</c>.</summary>
    /// <returns>The group.</returns>
    private MathBox ReadGroup()
    {
        _position++;
        var group = ReadRow(MathStop.Brace, group: true);
        if (!AtEnd && _text[_position] == '}') _position++;
        return group;
    }

    /// <summary>Whether the cursor stands at <paramref name="stop"/> (or at the end of the math, where every row stops).</summary>
    private readonly bool At(MathStop stop)
    {
        char c = _text[_position];
        return c == '$' || stop switch
        {
            MathStop.Brace => c == '}',
            MathStop.Bracket => c == ']',
            MathStop.Delimiter => AtCommand("right") || AtCommand("middle"),
            MathStop.Cell => c == '&' || _text.AsSpan(_position).StartsWith(@"\\") || AtCommand("end"),
            MathStop.StackRow => c == '}' || _text.AsSpan(_position).StartsWith(@"\\"),
            _ => false,
        };
    }

    /// <summary>Whether the command <paramref name="name"/> is written at the position (a backslash, the name, and no letter after it).</summary>
    internal readonly bool AtCommand(string name)
    {
        var rest = _text.AsSpan(_position);
        return rest.Length > name.Length && rest[0] == '\\' && rest[1..].StartsWith(name)
            && (rest.Length == name.Length + 1 || !char.IsLetter(rest[name.Length + 1]));
    }

    /// <summary>Reads one step of math: a command, scripts, a group, or a run of characters (one character when
    /// <paramref name="single"/>, the argument of a script or a command written without braces).</summary>
    private void ReadStep(bool single)
    {
        char c = _text[_position];
        switch (c)
        {
            case '\\':
                ReadCommand();
                return;
            case '^' or '_' or '\'':
                ReadScripts();
                return;
            case '{':
                Add(ReadGroup());
                return;
            case '}' or ' ':
                _position++; // a brace that closes no group, a space typed in math: neither is drawn
                return;
        }

        var rest = _text.AsSpan(_position);
        int plain = rest.IndexOfAnyExcept(PlainAscii);
        if (plain < 0) plain = rest.Length;
        if (plain > 0)
        {
            int length = single ? StringInfo.GetNextTextElementLength(rest) : plain;
            _run.AppendSource(_position, length);
            _position += length;
            return;
        }

        _position++;
        if (MathGrammar.LiteralSpaces.TryGetValue(c, out double em))
        {
            Add(KernBox.Of(em));
            return;
        }

        var role = MathGrammar.RoleOf(c);
        if (role == MathSymbolRole.Ordinary)
        {
            int length = StringInfo.GetNextTextElementLength(rest);
            _run.AppendSource(_position - 1, length); // a character beyond ASCII that takes no space, with the marks after it
            _position += length - 1;
            return;
        }

        AddSpaced(GlyphsBox.TextOf(c), role, c, _math.Before(_position - 1), _math.After(_position));
    }

    /// <summary>A <c>\</c>: the name after it is one punctuation character when the table holds that as a command (the spaces
    /// <c>\,</c> <c>\:</c> <c>\;</c> <c>\!</c>, the accents <c>\"</c> <c>\'</c>) or the grammar as a symbol (<c>\|</c>), otherwise the
    /// run of letters, which may be empty. The family the table names reads the rest; a name the table does not hold is a symbol. A
    /// character TeX escapes (<see cref="Escaped"/>) is that character.</summary>
    private void ReadCommand()
    {
        _position++; // the '\'
        if (!AtEnd && !char.IsLetter(_text[_position]))
        {
            char mark = _text[_position];
            if (Escaped.Contains(mark))
            {
                _run.Append(mark);
                _position++;
                return;
            }

            var punctuation = _text.AsSpan(_position, 1);
            if ((MathCommand.Find(punctuation) ?? MathSymbol.Named(punctuation)) is { } family)
            {
                _position++;
                family.Read(ref this, punctuation);
                return;
            }
        }

        string text = _text;
        int start = _position;
        int end = start;
        while (end < text.Length && char.IsLetter(text[end])) end++;
        _position = end;
        var name = text.AsSpan(start, end - start);
        (MathCommand.Find(name) ?? MathSymbol.Instance).Read(ref this, name);
    }

    /// <summary>Scripts after a nucleus (matplotlib's <c>subsuper</c>): the last box written, or the last character of the run being
    /// read, takes every <c>^</c> and <c>_</c> that follows and the apostrophes after them, which add primes to its superscript. A
    /// second superscript or subscript starts a nucleus of its own over the first pair.</summary>
    private void ReadScripts()
    {
        var nucleus = TakeNucleus();
        MathBox? superscript = null, subscript = null;
        while (!AtEnd && _text[_position] is '^' or '_')
        {
            bool raised = _text[_position++] == '^';
            if (raised ? superscript is not null : subscript is not null)
            {
                nucleus = nucleus.WithScripts(superscript, subscript);
                superscript = subscript = null;
            }

            var argument = ReadArgument();
            if (raised) superscript = argument;
            else subscript = argument;
        }

        int primes = 0;
        while (!AtEnd && _text[_position] == '\'')
        {
            primes++;
            _position++;
        }

        if (primes > 0)
        {
            var marks = new GlyphsBox(new string('′', primes), _variant);
            superscript = superscript is null ? marks : new RowBox([superscript, marks]);
        }

        Add(nucleus.WithScripts(superscript, subscript));
        if (_functionScripts)
        {
            _functionScripts = false;
            Add(ThinSpace);
        }
    }

    /// <summary>The nucleus of the scripts at the position: the last character of the run being read (the run before it written out),
    /// else the last box written in this math, else nothing.</summary>
    private MathBox TakeNucleus()
    {
        if (_run.IsEmpty) return _rows.TakeLast() ?? RowBox.Empty;

        string run = _run.Take();
        int last = GlyphsBox.LastClusterStart(run);
        if (last > 0) _rows.Add(Glyphs(run[..last]));
        return Glyphs(run[last..]);
    }

    /// <summary>Reads an argument as TeX does: spaces before it skipped, then a <c>{...}</c> group read as math, or without braces one
    /// token (a command with its own arguments, or one character). A <c>$</c>, which closes math, is never an argument.</summary>
    /// <param name="variant">The font switch the argument is read under; <see langword="null"/>: the one the cursor stands in.</param>
    /// <returns>The argument's box.</returns>
    internal MathBox ReadArgument(FontVariant? variant = null)
    {
        if (variant is { } switched)
        {
            FlushRun();
            var outer = _variant;
            _variant = switched;
            var argument = ReadArgument();
            _variant = outer;
            return argument;
        }

        SkipSpaces();
        if (AtEnd || _text[_position] == '$') return RowBox.Empty;
        if (_text[_position] == '{') return ReadGroup();

        // One character written as it is (the common script, x^2): its box, with no row read for it.
        char c = _text[_position];
        if (PlainAscii.Contains(c))
        {
            _position++;
            FlushRun();
            return Glyphs(GlyphsBox.TextOf(c));
        }

        FlushRun();
        var row = _rows.Open();
        ReadStep(single: true);
        FlushRun();
        return _rows.CloseAsBox(row);
    }

    /// <summary>Reads a <c>{...}</c> group, nested braces included, or without a brace one token, as it was written.</summary>
    internal string ReadBraceGroup()
    {
        SkipSpaces();
        if (AtEnd) return string.Empty;
        if (_text[_position] != '{') return ReadToken();

        string text = _text;
        int start = _position + 1;
        int i = start;
        for (int depth = 1; i < text.Length; i++)
        {
            if (text[i] == '{') depth++;
            else if (text[i] == '}' && --depth == 0) break;
        }

        _position = i < text.Length ? i + 1 : i;
        return text[start..i];
    }

    /// <summary>Takes one token, the way TeX reads an argument without braces: a command (<c>\alpha</c>, or a backslash and the one
    /// character after it) or one character. A <c>$</c> is never taken: there, and at the end of the text, nothing is.</summary>
    /// <returns>The token as written; empty where nothing is taken.</returns>
    internal string ReadToken()
    {
        SkipSpaces();
        if (AtEnd || _text[_position] == '$') return string.Empty;

        int start = _position++;
        if (_text[start] == '\\' && !AtEnd)
        {
            if (!char.IsLetter(_text[_position])) _position++;
            else while (!AtEnd && char.IsLetter(_text[_position])) _position++;
        }

        return _text[start.._position];
    }

    /// <summary>Steps past the spaces at the position.</summary>
    internal void SkipSpaces()
    {
        while (!AtEnd && _text[_position] == ' ') _position++;
    }

    /// <summary>Steps past <paramref name="written"/> when it is written at the position (a bracket, a brace, <c>\\</c>,
    /// <c>\right</c>).</summary>
    internal void Skip(ReadOnlySpan<char> written)
    {
        if (_text.AsSpan(_position).StartsWith(written)) _position += written.Length;
    }

    /// <summary>Writes text into the run being read, in the current font switch.</summary>
    internal void AppendText(string text) => _run.Append(text);

    /// <summary>Adds a box after the run being read, which is written out first.</summary>
    internal void Add(MathBox box)
    {
        FlushRun();
        _rows.Add(box);
    }

    /// <summary>Adds a symbol with the spaces matplotlib puts around it (<see cref="MathGrammar.SpacesAround"/>), read from what is
    /// written before and after it; a symbol that takes none joins the run being read.</summary>
    /// <param name="glyph">The symbol's character.</param>
    /// <param name="role">What the symbol is to the spacing.</param>
    /// <param name="written">The symbol as written, when it is one character; <c>'\0'</c> for a name.</param>
    /// <param name="before">The character written before it.</param>
    /// <param name="after">The character written after it.</param>
    internal void AddSpaced(string glyph, MathSymbolRole role, char written, char before, char after)
    {
        var spaces = MathGrammar.SpacesAround(role, written, before, after, _functionScripts);
        if (spaces is { Before: false, After: false })
        {
            if (written == '\0') _run.Append(glyph);
            else _run.AppendSource(_position - 1, 1); // the character as it is written, just read
            return;
        }

        var symbol = Glyphs(glyph);
        Add(spaces switch
        {
            { Before: true, After: true } => new RowBox([SymbolSpace, symbol, SymbolSpace]),
            { Before: true } => new RowBox([SymbolSpace, symbol]),
            _ => new RowBox([symbol, SymbolSpace]),
        });
    }

    /// <summary>Adds the name of a function the cursor stands just past (<c>Parser.operatorname</c>), as a nucleus whose scripts are
    /// limits when <paramref name="takesLimits"/>, then the thin space matplotlib puts after a function name unless a delimiter or a
    /// script follows it. A script that follows is read as the function's (<see cref="_functionScripts"/>).</summary>
    internal void AddFunctionName(MathBox name, bool takesLimits)
    {
        Add(takesLimits ? new TakesLimitsBox(name) : name);
        char next = _math.After(_position);
        _functionScripts = next is '^' or '_';
        if (!MathGrammar.FunctionFollowers.Contains(next)) Add(ThinSpace);
    }

    /// <summary>A run of <paramref name="text"/> in the current font switch, inside or outside math as the cursor stands.</summary>
    internal readonly GlyphsBox Glyphs(string text) => GlyphsBox.Of(text, _inMath ? _variant : FontVariant.Default, !_inMath);

    /// <summary>Writes out the run being read as a box of glyphs.</summary>
    private void FlushRun()
    {
        if (_run.IsEmpty) return;
        _rows.Add(Glyphs(_run.Take()));
    }

    /// <summary>What a parse (<see cref="MathTextCursor"/>) writes on before it knows how long a row or a run of text is: the rows it has
    /// open (<see cref="Rendering.MathText.MathTextCursor.MathRows"/>) and the builder a run made of several pieces is joined in (<see cref="Rendering.MathText.MathTextCursor.MathPendingText"/>). A thread keeps
    /// one between parses (<see cref="KeptByThread{T}"/>), so a parse allocates the boxes of its tree, each row at its own length, and
    /// nothing else.</summary>
    internal sealed class MathParseScratch
    {
        /// <summary>The rows the parse has open.</summary>
        internal MathRows Rows { get; } = new();

        /// <summary>The builder a run of several pieces is joined in.</summary>
        internal StringBuilder Text { get; } = new();

        /// <summary>The scratch of a parse on this thread.</summary>
        internal static MathParseScratch Take() => KeptByThread<MathParseScratch>.Take();

        /// <summary>Ends a parse: the scratch, emptied, is the thread's for its next parse, unless the label it read grew it past what a
        /// label is worth keeping for (<see cref="Rendering.MathText.MathTextCursor.MathRows.IsWorthKeeping"/>).</summary>
        internal void Return()
        {
            Rows.Clear();
            Text.Clear();
            if (Rows.IsWorthKeeping) KeptByThread<MathParseScratch>.Return(this);
        }
    }

    /// <summary>Where the row a parse was reading starts and where its math starts, to put back when a row read inside it is closed
    /// (<see cref="Rendering.MathText.MathTextCursor.MathRows.Open"/>).</summary>
    /// <param name="Start">Where the row starts among the boxes.</param>
    /// <param name="Floor">Where the math being read starts in the row.</param>
    internal readonly record struct MathRowMark(int Start, int Floor);

    /// <summary>The rows of boxes a parse has open, one inside the other (a group, an argument, a cell inside the row it is written in),
    /// on one list: the row being read is the list's end from where the row was opened, and closing it cuts that end off as a row of its
    /// own length. A script takes its nucleus from the row being read, never from before the math it stands in
    /// (<see cref="Rendering.MathText.MathTextCursor.MathRows.StartMath"/>).</summary>
    internal sealed class MathRows
    {
        /// <summary>The most boxes a list kept between parses may have room for: more than any label of a chart holds open at once, and
        /// little enough to keep for each thread that parses.</summary>
        private const int KeptCapacity = 256;

        private readonly List<MathBox> _boxes = [];
        private int _start;
        private int _floor;

        /// <summary>Whether the list is small enough to keep for the next parse.</summary>
        internal bool IsWorthKeeping => _boxes.Capacity <= KeptCapacity;

        /// <summary>Adds <paramref name="box"/> to the row being read.</summary>
        internal void Add(MathBox box) => _boxes.Add(box);

        /// <summary>Starts a run of math in the row being read: a script written first in it has no nucleus (matplotlib parses each run of
        /// math as a list of its own).</summary>
        internal void StartMath() => _floor = _boxes.Count;

        /// <summary>Takes the last box of the math being read out of its row, as the nucleus of the scripts that follow it.</summary>
        /// <returns>The box; <see langword="null"/> when the math holds none yet.</returns>
        internal MathBox? TakeLast()
        {
            int last = _boxes.Count - 1;
            if (last < _floor) return null;
            var box = _boxes[last];
            _boxes.RemoveAt(last);
            return box;
        }

        /// <summary>Opens a row inside the one being read.</summary>
        /// <returns>The row being read until now, to hand to <see cref="Rendering.MathText.MathTextCursor.MathRows.Close"/> or <see cref="Rendering.MathText.MathTextCursor.MathRows.CloseAsBox"/>.</returns>
        internal MathRowMark Open()
        {
            var outer = new MathRowMark(_start, _floor);
            _start = _floor = _boxes.Count;
            return outer;
        }

        /// <summary>Closes the row being read; the row it was opened in is read again.</summary>
        /// <param name="outer">What <see cref="Rendering.MathText.MathTextCursor.MathRows.Open"/> returned.</param>
        /// <returns>The row's boxes.</returns>
        internal MathBox[] Close(MathRowMark outer)
        {
            MathBox[] row = CollectionsMarshal.AsSpan(_boxes)[_start..].ToArray();
            Reopen(outer);
            return row;
        }

        /// <summary>Closes the row being read as one box: the box itself when the row holds one, with no row made for it, and
        /// <see cref="RowBox.Empty"/> when it holds none.</summary>
        /// <param name="outer">What <see cref="Rendering.MathText.MathTextCursor.MathRows.Open"/> returned.</param>
        /// <returns>The box.</returns>
        internal MathBox CloseAsBox(MathRowMark outer)
        {
            int count = _boxes.Count - _start;
            if (count > 1) return new RowBox(Close(outer));

            var box = count == 1 ? _boxes[_start] : RowBox.Empty;
            Reopen(outer);
            return box;
        }

        /// <summary>Drops the boxes of the row being read and goes back to the row <paramref name="outer"/> was opened in.</summary>
        private void Reopen(MathRowMark outer)
        {
            _boxes.RemoveRange(_start, _boxes.Count - _start);
            (_start, _floor) = (outer.Start, outer.Floor);
        }

        /// <summary>Empties the list for the next parse: no row open, no math started.</summary>
        internal void Clear()
        {
            _boxes.Clear();
            _start = _floor = 0;
        }
    }
}
