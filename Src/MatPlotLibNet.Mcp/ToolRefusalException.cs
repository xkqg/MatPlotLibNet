// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Mcp;

/// <summary>A refusal the tool boundary turns into an MCP tool error the model can act on: the message names the
/// field and the rule, never a stack frame. Everything the server refuses on purpose is one of these.</summary>
internal sealed class ToolRefusalException(string reason) : Exception(reason);
