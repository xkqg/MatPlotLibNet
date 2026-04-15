// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.AspNetCore;

/// <summary>Wire DTO for <see cref="ChartHub.OnHover"/>. Carries the four fields a client can
/// supply — chart id, axes index, and the hovered data-space point — without the
/// <c>CallerConnectionId</c> field of <see cref="Interaction.HoverEvent"/>. The hub stamps the
/// real connection ID in server-side from <c>Context.ConnectionId</c> before publishing,
/// which prevents a malicious client from spoofing another user's connection.</summary>
public sealed record HoverEventPayload(string ChartId, int AxesIndex, double X, double Y);
