// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>The one test that drives the real process over the real transport. stdout carries the protocol and
/// nothing else — a single stray line from a logger, a dependency or a forgotten <c>Console.WriteLine</c> corrupts
/// the stream, and the failure is invisible: the host sees a server that answers nothing. Nothing inside the
/// library prints (measured: zero console writes anywhere in Src/), so the risk lives entirely in this package's
/// own entry point, which is exactly what this drives.</summary>
[Collection("McpStdio")]
public class McpStdioTests
{
    private static readonly TimeSpan Budget = TimeSpan.FromSeconds(90);

    private static string ServerDll()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "CHANGELOG.md")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var candidates = new DirectoryInfo(Path.Combine(dir!.FullName, "Src", "MatPlotLibNet.Mcp", "bin"))
            .EnumerateFiles("MatPlotLibNet.Mcp.dll", SearchOption.AllDirectories)
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .ToArray();

        Assert.True(candidates.Length > 0, "The server has not been built.");
        return candidates[0].FullName;
    }

    /// <summary>Runs the server, feeds it the frames, and collects the response frames. stdin stays OPEN until the
    /// answers are in: the transport stops the host the moment the pipe closes, and a server told to shut down
    /// mid-flight never writes the replies it had already computed. A host holds the pipe; so does this.</summary>
    private static JsonDocument[] Converse(int expectedResponses, params string[] frames)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add("exec");
        start.ArgumentList.Add(ServerDll());

        using var server = Process.Start(start)!;
        var lines = new List<string>();
        var reader = Task.Run(() =>
        {
            while (server.StandardOutput.ReadLine() is { } line)
            {
                if (line.Length > 0)
                {
                    lock (lines)
                    {
                        lines.Add(line);
                    }
                }
            }
        });
        var errors = Task.Run(() => server.StandardError.ReadToEnd());

        foreach (var frame in frames)
        {
            server.StandardInput.WriteLine(frame);
        }
        server.StandardInput.Flush();

        var deadline = DateTime.UtcNow + Budget;
        while (DateTime.UtcNow < deadline)
        {
            lock (lines)
            {
                if (lines.Count >= expectedResponses)
                {
                    break;
                }
            }
            Thread.Sleep(25);
        }

        server.StandardInput.Close();
        Assert.True(server.WaitForExit((int)Budget.TotalMilliseconds), "The server did not exit when stdin closed.");
        reader.Wait(Budget);

        lock (lines)
        {
            Assert.True(lines.Count >= expectedResponses,
                $"Expected {expectedResponses} frames, got {lines.Count}. stderr: {errors.Result}");
            return [.. lines.Select(line => JsonDocument.Parse(line))];
        }
    }

    private const string Initialize =
        """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"matplotlibnet-tests","version":"1.0"}}}""";

    private const string Initialized = """{"jsonrpc":"2.0","method":"notifications/initialized"}""";

    [Fact]
    public void ServerStdoutContainsOnlyJsonRpcFrames()
    {
        var stdout = Converse(2, Initialize, Initialized, """{"jsonrpc":"2.0","id":2,"method":"tools/list"}""");

        Assert.NotEmpty(stdout);
        foreach (var frame in stdout)
        {
            // Every line parsed as JSON to get here; each must also BE a protocol message.
            Assert.Equal("2.0", frame.RootElement.GetProperty("jsonrpc").GetString());
        }
    }

    [Fact]
    public void TheServerAnnouncesItsFiveTools()
    {
        var stdout = Converse(2, Initialize, Initialized, """{"jsonrpc":"2.0","id":2,"method":"tools/list"}""");

        var names = stdout
            .Where(f => f.RootElement.TryGetProperty("result", out var r) && r.TryGetProperty("tools", out _))
            .SelectMany(f => f.RootElement.GetProperty("result").GetProperty("tools").EnumerateArray())
            .Select(t => t.GetProperty("name").GetString() ?? "")
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["chart_data_table", "describe_chart_schema", "list_chart_types", "render_chart", "save_chart"], names);
    }

    [Theory]
    //          tool                     title                            read-only  destructive  idempotent  open world
    [InlineData("render_chart",          "Render a chart as an image",    true,      false,       true,       false)]
    [InlineData("save_chart",            "Save a chart to a file",        false,     true,        true,       false)]
    [InlineData("chart_data_table",      "Read a chart's data as a table", true,     false,       true,       false)]
    [InlineData("list_chart_types",      "List the chart types",          true,      false,       true,       false)]
    [InlineData("describe_chart_schema", "Describe the chart spec",       true,      false,       true,       false)]
    public void TheToolList_CarriesTheTitleAndTheHintsAHostReads(
        string tool, string title, bool readOnly, bool destructive, bool idempotent, bool openWorld)
    {
        // The attributes are one thing; what a host actually receives is another. Two of the four hints default
        // the wrong way round in the protocol — a tool nobody annotates is assumed destructive and assumed to
        // reach an open world — so this reads them off the wire rather than off the attribute.
        var stdout = Converse(2, Initialize, Initialized, """{"jsonrpc":"2.0","id":2,"method":"tools/list"}""");

        var entry = stdout
            .Where(f => f.RootElement.TryGetProperty("result", out var r) && r.TryGetProperty("tools", out _))
            .SelectMany(f => f.RootElement.GetProperty("result").GetProperty("tools").EnumerateArray())
            .Single(t => t.GetProperty("name").GetString() == tool);

        Assert.Equal(title, entry.GetProperty("title").GetString());

        var hints = entry.GetProperty("annotations");
        Assert.Equal(title, hints.GetProperty("title").GetString());
        Assert.Equal(readOnly, hints.GetProperty("readOnlyHint").GetBoolean());
        Assert.Equal(destructive, hints.GetProperty("destructiveHint").GetBoolean());
        Assert.Equal(idempotent, hints.GetProperty("idempotentHint").GetBoolean());
        Assert.Equal(openWorld, hints.GetProperty("openWorldHint").GetBoolean());
    }

    [Fact]
    public void ARenderOverTheWire_ComesBackAsAnImageAndASummary()
    {
        var stdout = Converse(2, Initialize, Initialized,
            """{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"render_chart","arguments":{"spec":{"width":400,"height":300,"title":"Over the wire","subPlots":[{"series":[{"type":"line","xData":[1,2,3],"yData":[2,4,3]}]}]}}}}""");

        var content = stdout
            .Single(f => f.RootElement.TryGetProperty("id", out var id) && id.GetInt32() == 3)
            .RootElement.GetProperty("result").GetProperty("content").EnumerateArray().ToArray();

        Assert.Equal("image", content[0].GetProperty("type").GetString());
        Assert.Equal("image/png", content[0].GetProperty("mimeType").GetString());
        Assert.NotEmpty(content[0].GetProperty("data").GetString()!);
        Assert.Contains("Over the wire", content[1].GetProperty("text").GetString()!);
    }

    [Fact]
    public void TheDataTableOverTheWire_CarriesTheMarkdownAndTheValues()
    {
        var stdout = Converse(2, Initialize, Initialized,
            """{"jsonrpc":"2.0","id":5,"method":"tools/call","params":{"name":"chart_data_table","arguments":{"spec":{"width":400,"height":300,"title":"Revenue","subPlots":[{"xAxis":{"label":"Quarter"},"series":[{"type":"line","xData":[1,2,3],"yData":[12,18,15],"label":"2026"}]}]}}}}""");

        var result = stdout
            .Single(f => f.RootElement.TryGetProperty("id", out var id) && id.GetInt32() == 5)
            .RootElement.GetProperty("result");

        // The text a model reads is the markdown, exactly as before.
        string markdown = result.GetProperty("content")[0].GetProperty("text").GetString()!;
        Assert.Contains("| Quarter | 2026 |", markdown);
        Assert.Contains("| 3 | 15 |", markdown);

        // Beside it, the same values, as values.
        var table = result.GetProperty("structuredContent").GetProperty("tables")[0];
        Assert.Equal("Revenue", table.GetProperty("caption").GetString());
        Assert.Equal(["Quarter", "2026"],
            table.GetProperty("columns").EnumerateArray().Select(c => c.GetProperty("header").GetString()));
        Assert.Equal(15.0, table.GetProperty("rows")[2][1].GetDouble());
    }

    [Fact]
    public void TheDataTableTool_AdvertisesTheShapeOfThoseValues()
    {
        // A client is allowed to check structured content against the schema the tool publishes, and a tool that
        // returns structured content without one leaves it with nothing to check.
        var stdout = Converse(2, Initialize, Initialized, """{"jsonrpc":"2.0","id":2,"method":"tools/list"}""");

        var entry = stdout
            .Where(f => f.RootElement.TryGetProperty("result", out var r) && r.TryGetProperty("tools", out _))
            .SelectMany(f => f.RootElement.GetProperty("result").GetProperty("tools").EnumerateArray())
            .Single(t => t.GetProperty("name").GetString() == "chart_data_table");

        var schema = entry.GetProperty("outputSchema");
        Assert.Equal("object", schema.GetProperty("type").GetString());
        Assert.True(schema.GetProperty("properties").TryGetProperty("tables", out _));
    }

    [Fact]
    public void ARefusalOverTheWire_CarriesTheMessageTheModelNeeds()
    {
        var stdout = Converse(2, Initialize, Initialized,
            """{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"render_chart","arguments":{"spec":{"width":400,"height":300,"subPlots":[{"series":[{"type":"lien","xData":[1],"yData":[1]}]}]}}}}""");

        var result = stdout
            .Single(f => f.RootElement.TryGetProperty("id", out var id) && id.GetInt32() == 4)
            .RootElement.GetProperty("result");

        Assert.True(result.GetProperty("isError").GetBoolean());
        string text = result.GetProperty("content")[0].GetProperty("text").GetString()!;
        Assert.Contains("'lien'", text);
        Assert.Contains("'line'", text);
    }
}

/// <summary>The stdio tests each start a process; running them one at a time keeps a slow machine from timing out
/// four servers at once.</summary>
[CollectionDefinition("McpStdio", DisableParallelization = true)]
public class McpStdioCollection;
