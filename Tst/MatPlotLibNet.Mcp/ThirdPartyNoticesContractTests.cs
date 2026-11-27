// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>The tool package carries its whole dependency closure, so it hands the recipient the licence and copyright notice of
/// every assembly and native library it carries (MIT and BSD-3-Clause ask for the notice in every binary copy, Apache-2.0 section
/// 4(a) for a copy of the licence) and of the DejaVu faces and map data inside the assemblies it carries. The notices are
/// one file the package packs. Its first part is written by hand (the licences whose packages ship no licence file, the fonts, the
/// map data); everything below the marker is the licence and notice files the bundled packages ship, copied in whole, and
/// is rewritten by running this class with <c>MPL_REGEN_NOTICES=1</c> after a package version moves.</summary>
public class ThirdPartyNoticesContractTests
{
    private const string NoticesFile = "THIRD-PARTY-NOTICES.md";
    private const string Marker = "<!-- generated: the licence and notice files the bundled packages ship; MPL_REGEN_NOTICES=1 rewrites everything below -->";

    /// <summary>A package of the tool's runtime closure the restore resolved: its id, folder and top-level files.</summary>
    private readonly record struct BundledPackage(string Id, string Version, string Folder, string[] Files);

    private static readonly string Root = RepositoryRoot.Folder;

    private static string NoticesPath => Path.Combine(Root, "Src", "MatPlotLibNet.Mcp", NoticesFile);

    private static string Normalised(string text) => Regex.Replace(text, @"\s+", " ").Trim();

    /// <summary>The NuGet packages whose managed or native assets the tool carries, read from what the restore resolved (the
    /// project references are the library's own assemblies).</summary>
    private static BundledPackage[] Bundled()
    {
        using var assets = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "Src", "MatPlotLibNet.Mcp", "obj", "project.assets.json")));
        var root = assets.RootElement;
        string folder = root.GetProperty("packageFolders").EnumerateObject().First().Name;
        var target = root.GetProperty("targets").EnumerateObject().Single().Value;
        var bundled = new List<BundledPackage>();
        foreach (var entry in target.EnumerateObject())
        {
            if (entry.Value.GetProperty("type").GetString() != "package") continue;
            bool carries = new[] { "runtime", "native", "runtimeTargets" }.Any(kind => entry.Value.TryGetProperty(kind, out var kindAssets) && kindAssets.EnumerateObject().Any());
            if (!carries) continue;
            var library = root.GetProperty("libraries").GetProperty(entry.Name);
            int slash = entry.Name.IndexOf('/');
            bundled.Add(new BundledPackage(entry.Name[..slash], entry.Name[(slash + 1)..],
                Path.Combine(folder, library.GetProperty("path").GetString()!),
                [.. library.GetProperty("files").EnumerateArray().Select(file => file.GetString()!)]));
        }

        return [.. bundled.OrderBy(package => package.Id, StringComparer.Ordinal)];
    }

    private static bool IsNoticeFile(string file) =>
        !file.Contains('/') && Regex.IsMatch(file, @"^(licen[sc]e|third-party-notices|notice)(\.(txt|md))?$", RegexOptions.IgnoreCase);

    /// <summary>The licence a package's nuspec states: the SPDX expression, or the file it points at.</summary>
    private static string LicenceOf(BundledPackage package)
    {
        string nuspec = package.Files.Single(file => file.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
        var license = XDocument.Load(Path.Combine(package.Folder, nuspec)).Descendants().Single(element => element.Name.LocalName == "license");
        return (string?)license.Attribute("type") == "file" ? "file " + license.Value : license.Value;
    }

    /// <summary>The generated part: the list of the packages the tool carries with their versions and licences, then each
    /// distinct licence or notice text they ship, once, headed by the packages it came from.</summary>
    private static string Appendix()
    {
        var byText = new Dictionary<string, (string Text, SortedSet<string> From)>(StringComparer.Ordinal);
        foreach (var package in Bundled())
        {
            foreach (string file in package.Files.Where(IsNoticeFile).Order(StringComparer.Ordinal))
            {
                string text = SourceFile.ReadFrom(Path.Combine(package.Folder, file)).LineFeedText.TrimEnd();
                string key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))) + file.ToLowerInvariant();
                if (!byText.TryGetValue(key, out var seen)) byText[key] = seen = (text, []);
                seen.From.Add(package.Id);
            }
        }

        var appendix = new StringBuilder("\n## Packages the tool carries\n\n| Package | Version | Licence |\n|---|---|---|\n");
        foreach (var package in Bundled())
            appendix.Append("| ").Append(package.Id).Append(" | ").Append(package.Version).Append(" | ").Append(LicenceOf(package)).Append(" |\n");

        appendix.Append("\n## Licence and notice files they ship\n");
        foreach (var (text, origins) in byText.Values.OrderBy(v => v.From.First(), StringComparer.Ordinal).ThenBy(v => v.Text, StringComparer.Ordinal))
        {
            appendix.Append("\n### Shipped with ").Append(string.Join(", ", origins)).Append("\n\n~~~~text\n").Append(text).Append("\n~~~~\n");
        }

        return appendix.ToString();
    }

    private static string Notices() => SourceFile.ReadFrom(NoticesPath).LineFeedText;

    [Fact]
    public void TheToolPackage_PacksItsNotices()
    {
        var csproj = XDocument.Load(Path.Combine(Root, "Src", "MatPlotLibNet.Mcp", "MatPlotLibNet.Mcp.csproj"));
        var packed = csproj.Descendants("None").Where(n => (string?)n.Attribute("Include") == NoticesFile).ToArray();

        Assert.Single(packed);
        Assert.Equal("true", (string?)packed[0].Attribute("Pack"));
        Assert.Equal("\\", (string?)packed[0].Attribute("PackagePath"));
    }

    [Fact]
    public void TheNotices_NameEveryPackageTheToolCarries()
    {
        string notices = Notices();

        var missing = Bundled().Select(package => package.Id).Where(id => !notices.Contains("| " + id + " | ", StringComparison.Ordinal)).ToArray();

        Assert.True(missing.Length == 0, "packages the tool carries and the notices do not name: " + string.Join(", ", missing));
    }

    [Fact]
    public void TheNotices_CarryTheLicenceTextOfEveryPackageThatShipsNoLicenceFile()
    {
        string notices = Normalised(Notices());

        // The copyright line of each licence that the bundled package itself does not carry as a file.
        Assert.Contains("Copyright (c) .NET Foundation and Contributors", notices);
        Assert.Contains("Copyright (c) .NET Foundation. All rights reserved.", notices);
        Assert.Contains("Copyright (c) 2024-2026 Model Context Protocol a Series of LF Projects, LLC.", notices);
        Assert.Contains("Copyright (C) 2005-2018 NetTopologySuite team", notices);
        Assert.Contains("Redistributions in binary form must reproduce the above copyright notice", notices);
        Assert.Contains("Apache License Version 2.0, January 2004", notices);
    }

    [Fact]
    public void TheNotices_CarryTheFontLicencesAndNameEveryBundledFontAndTheMapData()
    {
        string notices = Normalised(Notices());
        string fonts = Path.Combine(Root, "Src", "MatPlotLibNet.Skia", "Fonts");

        Assert.Contains(Normalised(File.ReadAllText(Path.Combine(fonts, "LICENSE_DEJAVU"))), notices);
        Assert.Contains(Normalised(File.ReadAllText(Path.Combine(fonts, "LICENSE_BAKOMA"))), notices);
        Assert.Contains(Normalised(File.ReadAllText(Path.Combine(fonts, "LICENSE_STIX"))), notices);
        // Every font MatPlotLibNet.Skia.dll embeds, read from its project, so a face packed later cannot go unnamed here.
        var embedded = System.Xml.Linq.XDocument.Load(Path.Combine(Root, "Src", "MatPlotLibNet.Skia", "MatPlotLibNet.Skia.csproj"))
            .Descendants("EmbeddedResource").Select(e => (string)e.Attribute("Include")!)
            .Where(include => include.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase))
            .Select(include => Path.GetFileName(include.Replace('\\', '/')))
            .ToArray();
        Assert.Equal(21, embedded.Length);
        foreach (string face in embedded)
            Assert.Contains(face, notices);
        Assert.Contains("Natural Earth", notices);
        Assert.Contains("public domain", notices);
    }

    [Fact]
    public void TheAppendix_IsWhatTheBundledPackagesShip()
    {
        string notices = Notices();
        int at = notices.IndexOf(Marker, StringComparison.Ordinal);
        Assert.True(at >= 0, "the marker line is missing");
        string expected = notices[..(at + Marker.Length)] + "\n" + Appendix();

        if (Environment.GetEnvironmentVariable("MPL_REGEN_NOTICES") == "1")
        {
            File.WriteAllText(NoticesPath, expected, new UTF8Encoding(false));
            return;
        }

        Assert.True(notices == expected, $"the appendix of {NoticesFile} is not what the bundled packages ship now: run this class with MPL_REGEN_NOTICES=1");
    }
}
