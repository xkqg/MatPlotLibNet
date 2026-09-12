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
