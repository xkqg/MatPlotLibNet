// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;

namespace MatPlotLibNet.Wiki.Tests;

/// <summary>
/// The names the packages published in the release before this one and no longer have: every type and member the package-validation
/// suppression files of <c>Src</c> record as removed (CP0001, CP0002) whose name no type of the packages as built declares, inherits or
/// adds by an extension any more. The suppression files are the list; this reads them, so a removal recorded there is known here
/// without a second list.
/// </summary>
internal static class RemovedSinceBaseline
{
    private static readonly Lazy<IReadOnlySet<string>> Read = new(Gone);

    /// <summary>The removed names, each as a document writes it: a type by its name, a member by its own name.</summary>
    public static IReadOnlySet<string> Names => Read.Value;

    private static IReadOnlySet<string> Gone()
    {
        var surface = PackageSurface.Loaded;

        // A removal the package validation records: a type or a member that is gone (CP0001, CP0002), its target `T:Name.Space.Type` or
        // `M:Name.Space.Type.Member(...)`.
        return RepositoryTree.Files("Src", "CompatibilitySuppressions.xml")
            .SelectMany(file => XDocument.Load(Path.Join(RepositoryRoot.Folder, file.Path)).Descendants("Suppression"))
            .Where(suppression => (string?)suppression.Element("DiagnosticId") is "CP0001" or "CP0002")
            .Select(suppression => ((string?)suppression.Element("Target") ?? string.Empty).Split('(')[0])
            .Where(target => target.Length > 2 && target[1] == ':')
            .Select(target => NameOf(target[..1], target[2..]))
            .OfType<string>()
            // A name the compiler makes (the marker of an extension block, written <M>$ and a hash) is no name a document writes.
            .Where(name => name.All(char.IsAsciiLetterOrDigit) && !surface.IsType(name) && surface.TypesHaving(name).Count == 0)
            .ToHashSet(StringComparer.Ordinal);
    }

    // The name a removal's target is written by: a type's own name without its arity, a member's name without its accessor's prefix; null
    // for a constructor (its type names it).
    private static string? NameOf(string kind, string target)
    {
        if (target.EndsWith(".#ctor", StringComparison.Ordinal) || target.EndsWith(".#cctor", StringComparison.Ordinal))
        {
            return null;
        }

        // A member a type implements explicitly is written after its interface's name and a #.
        var last = target[(target.LastIndexOf('.') + 1)..];
        last = last[(last.LastIndexOf('#') + 1)..].Split('`')[0];

        if (kind == "T")
        {
            return last;
        }

        return last.StartsWith("get_", StringComparison.Ordinal) || last.StartsWith("set_", StringComparison.Ordinal) ? last[4..]
            : last.StartsWith("add_", StringComparison.Ordinal) ? last[4..]
            : last.StartsWith("remove_", StringComparison.Ordinal) ? last[7..]
            : last;
    }
}
