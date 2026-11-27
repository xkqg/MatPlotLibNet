// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet;

/// <summary>The one way the library writes a file: the whole output is made first, in memory, and only once it is complete
/// does it replace the file at the path. A save that fails leaves the file that was there as it was, and a save over a longer
/// file leaves nothing of that file behind (a stream opened without truncating kept its tail after the new end). Every
/// <c>Save*</c> of the library and its packages writes through here.</summary>
internal static class FileReplaceExtensions
{
    extension(string path)
    {
        /// <summary>Replaces the file at this path with what <paramref name="write"/> writes, once it has written all of it.</summary>
        /// <param name="write">Writes the whole output to the stream it is given.</param>
        internal void ReplaceFileWith(Action<Stream> write)
        {
            using var buffer = new MemoryStream();
            write(buffer);
            using var file = File.Create(path);
            buffer.WriteTo(file);
        }

        /// <summary>Replaces the file at this path with <paramref name="text"/>, as UTF-8 without a byte order mark.</summary>
        /// <param name="text">The whole text of the file.</param>
        internal void ReplaceFileWith(string text) => File.WriteAllText(path, text);

        /// <summary>Replaces the file at this path with <paramref name="bytes"/>.</summary>
        /// <param name="bytes">The whole content of the file.</param>
        internal void ReplaceFileWith(byte[] bytes) => File.WriteAllBytes(path, bytes);
    }
}
