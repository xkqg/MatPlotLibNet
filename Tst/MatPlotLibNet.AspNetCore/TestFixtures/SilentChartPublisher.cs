// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>The publisher a test hands a session or a registry that has nothing to say about what is published: every member does
/// nothing. It declares exactly the members of <see cref="IChartPublisher"/> that have no default body, so the interface's own default
/// (the view change) still runs through it. A test that wants the calls derives <see cref="RecordingChartPublisher"/>.</summary>
internal class SilentChartPublisher : IChartPublisher
{
    /// <inheritdoc />
    public virtual Task PublishAsync(string chartId, Figure figure, CancellationToken ct = default) => Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task PublishSvgAsync(string chartId, Figure figure, CancellationToken ct = default) => Task.CompletedTask;
}
