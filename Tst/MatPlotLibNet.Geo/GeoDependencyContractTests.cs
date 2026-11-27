// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Xml.Linq;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>Every package the Geo package brings in beyond the core's (the H3 port and what it needs) is a managed assembly under a
/// permissive licence: the package ships no native assets (nothing under <c>runtimes/</c>), and its licence is an MIT, Apache-2.0 or
/// BSD expression, or a licence file that is one of them. Read from what the restore resolved, so a later version that changes either
/// turns this red.</summary>
public class GeoDependencyContractTests
{
    private static readonly string[] PermissiveExpressions = ["MIT", "Apache-2.0", "BSD-2-Clause", "BSD-3-Clause"];
    private static readonly string[] PermissiveFileOpenings = ["Apache License", "MIT License", "The MIT License", "BSD"];

    /// <summary>A package the restore resolved: its id, its folder and its files.</summary>
    private readonly record struct ResolvedPackage(string Id, string Folder, string[] Files);

    private static Dictionary<string, ResolvedPackage> Packages(string project)
    {
        using var assets = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepositoryRoot.Folder, "Src", project, "obj", "project.assets.json")));
        var root = assets.RootElement;
        string folder = root.GetProperty("packageFolders").EnumerateObject().First().Name;
        var packages = new Dictionary<string, ResolvedPackage>(StringComparer.OrdinalIgnoreCase);
        foreach (var library in root.GetProperty("libraries").EnumerateObject())
        {
            if (library.Value.GetProperty("type").GetString() != "package") continue;
            string id = library.Name[..library.Name.IndexOf('/')];
            packages[id] = new ResolvedPackage(id, Path.Combine(folder, library.Value.GetProperty("path").GetString()!),
                [.. library.Value.GetProperty("files").EnumerateArray().Select(file => file.GetString()!)]);
        }

        return packages;
    }

    private static ResolvedPackage[] BroughtInByGeo()
    {
        var core = Packages("MatPlotLibNet");
        return [.. Packages("MatPlotLibNet.Geo").Values.Where(package => !core.ContainsKey(package.Id))];
    }

    [Fact]
    public void TheH3PortAndWhatItNeeds_AreWhatGeoBringsIn()
    {
        Assert.Equal(["NetTopologySuite", "pocketken.H3"], BroughtInByGeo().Select(package => package.Id).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void APackageGeoBringsIn_ShipsNoNativeAssets()
    {
        foreach (var package in BroughtInByGeo())
            Assert.DoesNotContain(package.Files, file => file.StartsWith("runtimes/", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void APackageGeoBringsIn_IsUnderAPermissiveLicence()
    {
        foreach (var package in BroughtInByGeo())
        {
            string nuspec = Path.Combine(package.Folder, package.Files.Single(file => file.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase)));
            var license = XDocument.Load(nuspec).Descendants().Single(element => element.Name.LocalName == "license");
            if ((string?)license.Attribute("type") == "file")
            {
                string text = File.ReadAllText(Path.Combine(package.Folder, license.Value)).TrimStart();
                Assert.True(PermissiveFileOpenings.Any(opening => text.StartsWith(opening, StringComparison.Ordinal)),
                    $"{package.Id} carries a licence file that is not MIT, Apache-2.0 or BSD");
            }
            else
            {
                Assert.Contains(license.Value, PermissiveExpressions);
            }
        }
    }
}
