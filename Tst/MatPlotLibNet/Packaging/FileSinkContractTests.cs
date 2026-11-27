// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>The library and its packages write a file in one way only, <c>FileReplaceExtensions.ReplaceFileWith</c>: the whole
/// output is made first and only then replaces the file. A writer that opens the file itself can leave the tail of a longer file
/// behind it (<c>File.OpenWrite</c> does not truncate, which is how a GIF saved over a longer one came out corrupt) or empty the
/// file before its output exists. The behaviour of each writer has its own test; this one holds that no new writer, and no old
/// one put back, goes around the sink.</summary>
public class FileSinkContractTests
{
    /// <summary>The one file allowed to open a file for writing: the sink itself.</summary>
    private const string Sink = "Src/MatPlotLibNet/Extensions/FileReplaceExtensions.cs";

    private static readonly Regex DirectWrite = new(
        @"\bFile\.(WriteAll\w*|AppendAll\w*|Create|OpenWrite|Open)\s*\(|\bnew\s+FileStream\s*\(",
        RegexOptions.CultureInvariant);

    [Fact]
    public void EveryFileTheLibraryWrites_IsWrittenThroughTheOneSink()
    {
        var around = new List<string>();

        foreach (var file in RepositoryTree.SourceFiles().Where(f => !string.Equals(f.Path, Sink, StringComparison.OrdinalIgnoreCase)))
        {
            for (int i = 0; i < file.CodeLines.Count; i++)
            {
                if (DirectWrite.IsMatch(file.CodeLines[i]))
                {
                    around.Add($"{file.Path}:{i + 1}: {file.CodeLines[i].Trim()}");
                }
            }
        }

        Assert.True(around.Count == 0,
            $"{around.Count} line(s) write a file without going through ReplaceFileWith:\n" + string.Join("\n", around));
    }
}
