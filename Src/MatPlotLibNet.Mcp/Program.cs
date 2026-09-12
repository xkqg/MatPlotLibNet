// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MatPlotLibNet.Mcp;

/// <summary>Process entry point of the MCP stdio server: host, services, transport, tools. Straight-line wiring
/// only — every decision lives in a measured class, so this one is excluded from the coverage gate the way the
/// Playground host and <c>BrowserLauncher</c> are. The moment it grows an <c>if</c>, that branch belongs in a
/// class a test can reach.</summary>
[ExcludeFromCodeCoverage]
public static class Program
{
    /// <summary>Builds the host and serves the protocol on stdin/stdout until the host closes the pipe.</summary>
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // stdout carries the protocol frames and nothing else: the stdio transport specification says a server
        // MUST NOT write anything there that is not a message, and a single stray line kills the session.
        builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

        builder.Services.AddSingleton<ChartTypeCatalog>();
        builder.Services.AddSingleton<ChartSummarizer>();
        builder.Services.AddSingleton<ChartSchemaDescription>();
        builder.Services.AddSingleton(sp => new ChartSpecReader(sp.GetRequiredService<ChartTypeCatalog>(), RenderLimits.Default));
        builder.Services.AddSingleton<ChartRendering>();
        builder.Services.AddSingleton(sp => new ChartTabulation(sp.GetRequiredService<ChartSpecReader>(), RenderLimits.Default));
        builder.Services.AddSingleton(_ =>
            OutputPathResolver.FromEnvironment(Environment.GetEnvironmentVariable(OutputPathResolver.RootVariable)));

        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            // Named explicitly rather than scanned: assembly scanning is marked as unsafe for trimming and Native
            // AOT in the SDK itself, and this package ships as a tool that a host may publish either way.
            .WithTools<ChartTools>();

        await builder.Build().RunAsync();
    }
}
