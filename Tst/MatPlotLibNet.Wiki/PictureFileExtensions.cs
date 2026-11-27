// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Wiki.Tests;

/// <summary>What the picture tests ask of a picture file: whether it holds the same picture as another.</summary>
internal static class PictureFileExtensions
{
    extension(string picture)
    {
        /// <summary>
        /// A picture is the same as another when its bytes are; a vector picture is text, so the line ending a checkout chose does not count.
        /// </summary>
        /// <param name="other">The path of the picture it is held to.</param>
        /// <returns><see langword="true"/> when both files hold the same picture.</returns>
        public bool IsTheSamePictureAs(string other) =>
            Path.GetExtension(picture) is ".svg"
                ? SourceFile.ReadFrom(picture).LineFeedText == SourceFile.ReadFrom(other).LineFeedText
                : File.ReadAllBytes(picture).AsSpan().SequenceEqual(File.ReadAllBytes(other));
    }
}
