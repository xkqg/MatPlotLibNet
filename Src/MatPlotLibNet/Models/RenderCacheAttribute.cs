// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>Marks a field of a model object that holds what a draw made from its data (the faces of a surface), not data
/// of its own: a <see cref="DataSnapshot"/> leaves it out, since it changes only because the data did.</summary>
[AttributeUsage(AttributeTargets.Field)]
internal sealed class RenderCacheAttribute : Attribute;
