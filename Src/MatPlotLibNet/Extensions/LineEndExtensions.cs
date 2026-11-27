// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;

namespace MatPlotLibNet;

/// <summary>The line end of every text the library and its packages write (the SVG, the indented JSON, the texts of the MCP
/// server) is a line feed alone, <see cref="LineEnd"/>, on every operating system, so the same figure writes the same bytes on
/// Windows and on Linux. <see cref="Environment.NewLine"/> and <see cref="StringBuilder.AppendLine(string)"/>, which write the
/// operating system's own line end (CRLF on Windows), are not used by any writer; a writer that ends a line asks this class.
/// <para>A text the library reads (a caption on more than one line) breaks its lines at CRLF and at LF alike,
/// <see cref="LineBreaks"/>, whichever operating system the text was written on.</para></summary>
internal static class LineEndExtensions
{
    /// <summary>The line end every writer writes: a line feed alone.</summary>
    internal const string LineEnd = "\n";

    /// <summary>What a reader breaks a text's lines at: CRLF, then LF, so a CRLF leaves no carriage return behind.</summary>
    internal static readonly string[] LineBreaks = ["\r\n", "\n"];

    extension(StringBuilder text)
    {
        /// <summary>Appends <paramref name="value"/> and <see cref="LineEnd"/>: <see cref="StringBuilder.AppendLine(string)"/>
        /// with the library's line end instead of the operating system's.</summary>
        /// <param name="value">The text of the line.</param>
        /// <returns>The builder, for chaining.</returns>
        internal StringBuilder AppendLineLf(string? value) => text.Append(value).Append(LineEnd);

        /// <summary>Appends <see cref="LineEnd"/> alone.</summary>
        /// <returns>The builder, for chaining.</returns>
        internal StringBuilder AppendLineLf() => text.Append(LineEnd);
    }
}
