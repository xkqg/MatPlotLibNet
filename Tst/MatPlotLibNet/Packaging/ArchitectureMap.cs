// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>How the architecture document names the files and folders of the library, and which of those names the repository does
/// not hold. The document draws the source tree in fenced blocks: a line that starts with <c>Name/</c> is a folder, a line that starts
/// with <c>Name.cs</c> is a file of the folder line above it (the nearest one indented less). Outside those two places a file is
/// named in prose, in a table or after the first file of a line (<c>A.cs, B.cs</c>), alone or with folders before it
/// (<c>Folder/Name.cs</c>), and a type is named by its folder (<c>Folder/Name</c>, <c>Folder/Name.Member</c>).</summary>
internal static class ArchitectureMap
{
    private static readonly Regex FolderLine = new(@"^(?<indent> *)(?<name>[A-Za-z][\w.]*)/(?:\s+(?:—|--)|\s*$)", RegexOptions.CultureInvariant);

    private static readonly Regex FileLine = new(@"^(?<indent> *)(?<name>[A-Za-z][\w.]*\.cs)(?!\w)", RegexOptions.CultureInvariant);

    /// <summary>A file name anywhere in a line, with the folders written before it. A name that follows <c>&lt;</c>, <c>&gt;</c> or
    /// <c>*</c> is the tail of a pattern (<c>AxesBuilder&lt;Family&gt;Verbs.cs</c>, <c>*.g.cs</c>), not a file.</summary>
    private static readonly Regex FileName = new(@"(?<![\w/.*<>-])(?<path>(?:[A-Za-z][\w.]*/)*[A-Za-z_][\w.]*\.cs)(?!\w)", RegexOptions.CultureInvariant);

    private static readonly Regex NameInFolder = new(@"(?<![\w/.])(?<folder>[A-Z]\w*)/(?<name>[A-Z]\w*)(?![\w/])", RegexOptions.CultureInvariant);

    /// <summary>Every name of a file or folder in <paramref name="document"/> that the repository does not hold where the document
    /// puts it.</summary>
    /// <param name="document">The architecture document.</param>
    /// <param name="source">Every file of the library's source tree, each path from the repository root. A folder is one that holds
    /// a file: an empty folder left in a working copy is no folder of the repository.</param>
    /// <param name="repository">Every file of the repository, each path from the root: a file named outside the tree may be a test, a
    /// tool or a document.</param>
    /// <returns>One finding per name, with the line it stands on; empty when every name is held.</returns>
    internal static IReadOnlyList<string> Misplaced(SourceFile document, IReadOnlyCollection<string> source, IReadOnlyCollection<string> repository)
    {
        string[] folders = [.. source.SelectMany(FoldersOf).Distinct(StringComparer.Ordinal)];
        string[] sourcePaths = [.. source.Select(path => "/" + path), .. folders];
        string[] everyFile = [.. repository.Select(path => "/" + path)];
        var folderNames = new HashSet<string>(folders.Select(path => path[(path.LastIndexOf('/') + 1)..]), StringComparer.Ordinal);
        foreach (string line in document.Lines)
        {
            if (FolderLine.Match(line) is { Success: true } named) folderNames.Add(named.Groups["name"].Value);
        }

        var findings = new List<string>();
        var above = new Stack<(int Indent, string Name)>();
        int block = -1;
        foreach (var (number, line, held) in SourceFile.MarkdownLinesOf(document.Text))
        {
            string where = $"{document.Path}:{number}";
            bool inTree = held >= 0;
            if (held != block)
            {
                block = held;
                above.Clear();
            }

            int read = 0;
            var entry = inTree ? FolderLine.Match(line) : Match.Empty;
            if (inTree && !entry.Success) entry = FileLine.Match(line);
            if (entry.Success)
            {
                int indent = entry.Groups["indent"].Length;
                string name = entry.Groups["name"].Value;
                while (above.Count > 0 && above.Peek().Indent >= indent) above.Pop();
                string placed = above.Count > 0 ? $"/{above.Peek().Name}/{name}" : "/" + name;
                bool isFolder = !name.EndsWith(".cs", StringComparison.Ordinal);
                if (!(isFolder ? folders : sourcePaths).Any(path => path.EndsWith(placed, StringComparison.Ordinal)))
                {
                    findings.Add($"{where}: the map puts {(isFolder ? "the folder" : "the file")} {placed[1..]} here, and Src holds none: {line.Trim()}");
                }

                if (isFolder) above.Push((indent, name));
                read = entry.Index + entry.Length;
            }

            foreach (Match file in FileName.Matches(line, read))
            {
                string path = "/" + file.Groups["path"].Value;
                if (!everyFile.Any(held => held.EndsWith(path, StringComparison.Ordinal)))
                {
                    findings.Add($"{where}: names the file {path[1..]}, and the repository holds none: {line.Trim()}");
                }
            }

            foreach (Match type in NameInFolder.Matches(line, read))
            {
                if (!folderNames.Contains(type.Groups["folder"].Value)) continue;
                string start = $"/{type.Groups["folder"].Value}/{type.Groups["name"].Value}";
                if (!sourcePaths.Any(path => Holds(path, start)))
                {
                    findings.Add($"{where}: names {start[1..]}, and Src holds no file or folder of that name in that folder: {line.Trim()}");
                }
            }
        }

        return findings;
    }

    /// <summary>The folders above <paramref name="file"/>, each from the root with a stroke before it.</summary>
    private static IEnumerable<string> FoldersOf(string file)
    {
        for (int at = file.IndexOf('/'); at >= 0; at = file.IndexOf('/', at + 1)) yield return "/" + file[..at];
    }

    /// <summary>Whether <paramref name="path"/> has a part that is <paramref name="start"/> whole: the name ends there, or its
    /// extension or the rest of a split file's name (<c>Name.Part.cs</c>, <c>Name-Bold.ttf</c>) follows.</summary>
    private static bool Holds(string path, string start)
    {
        int at = path.IndexOf(start, StringComparison.Ordinal);
        return at >= 0 && (at + start.Length == path.Length || !(char.IsLetterOrDigit(path[at + start.Length]) || path[at + start.Length] == '_'));
    }
}
