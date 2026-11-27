// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>The clients the repository ships in JavaScript and TypeScript (the client package the React, Vue and Angular
/// packages share, the Blazor package's page script) speak the chart hub and the SVG a chart carries by the names the C# side
/// owns, but cannot read a C# constant. This holds every name they spell to its owner: the hub methods to
/// <see cref="HubMethodNames"/>, the part marker to <see cref="SvgPartMarker"/>, the busy attribute to
/// <see cref="SvgAttributes.Busy"/>. It also holds the client to one owner: the framework packages take it from
/// <c>@matplotlibnet/client</c> and keep no copy of their own.</summary>
public partial class ScriptClientsContractTests
{
    private const string Client = "matplotlibnet-client";

    private static readonly string[] Bindings = ["matplotlibnet-react", "matplotlibnet-vue", "matplotlibnet-angular"];

    public static TheoryData<string> BindingNames { get; } = new(Bindings);

    private static string Read(params string[] path) => File.ReadAllText(Folder(path));

    private static string Folder(params string[] path) => Path.Join([RepositoryRoot.Folder, "Src", .. path]);

    [GeneratedRegex(@"\.(?:on|invoke)\('(?<name>[A-Za-z]+)'")]
    private static partial Regex HubCall();

    [GeneratedRegex(@"const PART_START = '(?<marker>[^']+)'")]
    private static partial Regex PartStart();

    [GeneratedRegex(@"data-mpl-[a-z-]*busy")]
    private static partial Regex BusyName();

    /// <summary>What only the client defines: its hub client, its SVG mount and its parts document.</summary>
    [GeneratedRegex(@"\bclass\s+(?:ChartSubscriptionClient|SvgPartsDocument)\b|\bfunction\s+mountSvg\b")]
    private static partial Regex ClientDefinition();

    private static HashSet<string> HubNames() =>
    [
        .. typeof(HubMethodNames).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string)).Select(f => (string)f.GetRawConstantValue()!),
    ];

    private static JsonElement Manifest(string package) => JsonDocument.Parse(Read(package, "package.json")).RootElement;

    [Fact]
    public void TheClient_CallsAndHearsTheHubByItsNames()
    {
        var names = HubCall().Matches(Read(Client, "src", "lib", "chart-subscription.client.ts")).Select(m => m.Groups["name"].Value).ToList();

        Assert.NotEmpty(names);
        Assert.All(names, name => Assert.Contains(name, HubNames()));
    }

    [Fact]
    public void TheClientsPartsDocument_FindsAPartByItsMarker() =>
        Assert.Equal(SvgPartMarker.Opening, PartStart().Match(Read(Client, "src", "lib", "svg-parts-document.ts")).Groups["marker"].Value);

    [Fact]
    public void TheClientsMount_WaitsOnTheBusyAttribute()
    {
        var names = BusyName().Matches(Read(Client, "src", "lib", "mount-svg.ts"));

        Assert.NotEmpty(names);
        Assert.All(names, m => Assert.Equal(SvgAttributes.Busy, m.Value));
    }

    [Theory]
    [MemberData(nameof(BindingNames))]
    public void ABinding_KeepsNoCopyOfTheClient(string binding)
    {
        var copies = RepositoryTree.Files($"Src/{binding}/src", "*.ts*")
            .Where(file => ClientDefinition().IsMatch(file.Text))
            .Select(file => file.Path)
            .ToList();

        Assert.Empty(copies);
    }

    [Theory]
    [MemberData(nameof(BindingNames))]
    public void ABinding_DependsOnTheClientAtTheClientsOwnVersion(string binding)
    {
        var version = Manifest(Client).GetProperty("version").GetString();

        Assert.Equal("@matplotlibnet/client", Manifest(Client).GetProperty("name").GetString());
        Assert.Equal($"^{version}", Manifest(binding).GetProperty("dependencies").GetProperty("@matplotlibnet/client").GetString());
    }

    [Fact]
    public void TheBlazorPageScript_WaitsOnTheBusyAttribute()
    {
        var names = BusyName().Matches(Read("MatPlotLibNet.Blazor", "wwwroot", "mplLiveChart.js"));

        Assert.NotEmpty(names);
        Assert.All(names, m => Assert.Equal(SvgAttributes.Busy, m.Value));
    }
}
