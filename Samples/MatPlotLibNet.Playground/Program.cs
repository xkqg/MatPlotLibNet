// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MatPlotLibNet.Playground;

namespace MatPlotLibNet.Playground;

/// <summary>WebAssembly host entry point for the Playground sample. Sample-only code — not
/// shipped in any library package; excluded from the coverage gate.</summary>
[ExcludeFromCodeCoverage]
public static class Program
{
    /// <summary>Wires up the Blazor WebAssembly host and runs the Playground app.</summary>
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        await builder.Build().RunAsync();
    }
}
