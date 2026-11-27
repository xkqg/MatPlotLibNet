// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet;

/// <summary>The one refusal of a value that is no member of its enum, in the sentence the site gives it. With
/// <c>ArgumentRangeExtensions</c> (a number outside a range) the only place that makes an <see cref="ArgumentOutOfRangeException"/>.</summary>
internal static class EnumMemberExtensions
{
    extension<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        /// <summary>This value, refused when it is no member of its enum.</summary>
        /// <param name="name">The name of the parameter, for the error.</param>
        /// <param name="sentence">What the members are, in the site's words (<c>The scale is Linear or Decibel.</c>).</param>
        internal TEnum ThrowIfNotDefined(string name, string sentence) => Enum.IsDefined(value) ? value : throw value.NotAMember(name, sentence);

        /// <summary>The refusal of this value as no member, for a place that decides it by another test (a switch's last arm, a set
        /// of flags with a stray bit): the caller throws it.</summary>
        /// <param name="name">The name of the parameter, for the error.</param>
        /// <param name="sentence">What the members are, in the site's words.</param>
        internal ArgumentOutOfRangeException NotAMember(string name, string sentence) => new ArgumentOutOfRangeException(name, value, sentence);
    }
}
