// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Data;

/// <summary>A single XY sample consumed by
/// <see cref="StreamingSeriesExtensions.SubscribeTo(Models.Series.Streaming.IStreamingSeries, IObservable{StreamingPoint})"/>.
/// Each emitted <see cref="StreamingPoint"/> from the subscribed source is forwarded to
/// <see cref="Models.Series.Streaming.IStreamingSeries.AppendPoint"/>.</summary>
/// <param name="X">Data-space X coordinate of the sample.</param>
/// <param name="Y">Data-space Y coordinate of the sample.</param>
public readonly record struct StreamingPoint(double X, double Y);
