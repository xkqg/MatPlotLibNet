// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;

namespace MatPlotLibNet.Rendering.MathText;

internal ref partial struct MathTextCursor
{
    /// <summary>The text of the run a parse is reading (<see cref="MathTextCursor"/>): a stretch of the label's own text while it is one, or
    /// the one text a symbol wrote while the run is nothing else (the grammar's own string of the character, kept as it is), copied into a
    /// builder only once the two meet or a second symbol joins it, so a run read as written is cut from the label without a copy and a run of
    /// one symbol is that symbol's string. The builder is the parse's (<see cref="Rendering.MathText.MathTextCursor.MathParseScratch.Text"/>), so a run joined in it allocates
    /// its text alone.</summary>
    internal struct MathPendingText
    {
        private readonly string _source;
        private readonly StringBuilder _builder;
        private int _start;
        private int _length;
        private string? _lone;
        private bool _copied;

        /// <summary>The run of a parse of <paramref name="source"/>.</summary>
        /// <param name="source">The label's text.</param>
        /// <param name="builder">The builder a run of several pieces is joined in, empty.</param>
        internal MathPendingText(string source, StringBuilder builder)
        {
            _source = source;
            _builder = builder;
        }

        /// <summary>Whether the run holds nothing.</summary>
        internal readonly bool IsEmpty => _copied ? _builder.Length == 0 : _length == 0 && _lone is null;

        /// <summary>Adds the characters of the label from <paramref name="start"/>, <paramref name="length"/> of them.</summary>
        internal void AppendSource(int start, int length)
        {
            if (length == 0) return;
            if (!_copied && _lone is null && (_length == 0 || start == _start + _length))
            {
                if (_length == 0) _start = start;
                _length += length;
                return;
            }

            Copied().Append(_source, start, length);
        }

        /// <summary>Adds characters not written as they stand in the label: the run is that text while it held nothing.</summary>
        internal void Append(string text)
        {
            if (text.Length == 0) return;
            if (!_copied && _lone is null && _length == 0)
            {
                _lone = text;
                return;
            }

            Copied().Append(text);
        }

        /// <summary>Adds one character not written where it stands in the label.</summary>
        internal void Append(char c) => Copied().Append(c);

        /// <summary>The run's text, and the run emptied.</summary>
        internal string Take()
        {
            string text;
            if (_copied)
            {
                text = _builder.ToString();
                _builder.Clear();
                _copied = false;
            }
            else
            {
                text = _lone ?? (_start == 0 && _length == _source.Length ? _source
                    : _length == 1 ? GlyphsBox.TextOf(_source[_start]) // one character: its one string, nothing cut
                    : _source.Substring(_start, _length));
                _length = 0;
                _lone = null;
            }

            return text;
        }

        /// <summary>The builder holding the run, what was read so far copied into it.</summary>
        private StringBuilder Copied()
        {
            if (_copied) return _builder;
            if (_lone is not null) _builder.Append(_lone);
            else _builder.Append(_source, _start, _length);
            _lone = null;
            _length = 0;
            _copied = true;
            return _builder;
        }
    }
}
