// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace MatPlotLibNet.Models.Series;

/// <summary>A series that labels its values and says what format its kind writes them in when it was given none: what the library adds to
/// <see cref="ILabelable"/>, which stays as it was. The text of a value is <c>LabelOf</c>.</summary>
internal interface ILabelFormatted : ILabelable
{
    /// <summary>The .NET format string the labels are written in while <see cref="ILabelable.LabelFormat"/> is <see langword="null"/>.</summary>
    string DefaultLabelFormat { get; }
}

/// <summary>The text of a value a series labels.</summary>
internal static class LabelFormatExtensions
{
    extension(ILabelFormatted series)
    {
        /// <summary>The text of <paramref name="value"/> as the series writes a label: in the format it was given, else the default of its kind,
        /// in the invariant culture.</summary>
        /// <param name="value">The value being labelled.</param>
        /// <returns>The label.</returns>
        internal string LabelOf(double value) =>
            value.InvariantText(series.LabelFormat ?? series.DefaultLabelFormat);
    }
}
