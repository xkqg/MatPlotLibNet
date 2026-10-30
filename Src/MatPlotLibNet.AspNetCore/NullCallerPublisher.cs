// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.AspNetCore;

/// <summary>The caller publisher of a registry that was given none: it drops the tooltips it is asked to send.</summary>
internal sealed class NullCallerPublisher : ICallerPublisher
{
    /// <summary>The one instance; it holds no state.</summary>
    internal static readonly NullCallerPublisher Instance = new();

    /// <inheritdoc />
    public Task SendTooltipAsync(string connectionId, string chartId, string html, CancellationToken ct = default) =>
        Task.CompletedTask;
}
