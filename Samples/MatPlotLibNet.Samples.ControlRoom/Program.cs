// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.AspNetCore;
using MatPlotLibNet.Samples.ControlRoom.Components;
using MatPlotLibNet.Samples.ControlRoom.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMatPlotLibNetSignalR();

// The simulated federation: ONE instance, registered twice on purpose — the page injects it to read the
// current state, and the host runs it as a background service so it keeps evolving whether or not a browser
// is looking. Two registrations of two different objects would give the wall a fleet nobody is advancing.
builder.Services.AddSingleton<BusTelemetrySimulator>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<BusTelemetrySimulator>());

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

// The live panels stream rendered SVG over this hub, and the simulator only renders them while a tab has
// them open. Without the hub the descent still works — the two panels are simply never fed.
app.MapChartHub();
app.Run();
