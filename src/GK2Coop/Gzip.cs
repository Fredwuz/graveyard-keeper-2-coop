using System.IO;
using System.IO.Compression;

namespace GK2Coop
{
    /// <summary>
    /// Packs state for the network and the player profiles. Unpacking stops at
    /// <see cref="MaxBytes"/>: a few hundred KB from a peer can expand to gigabytes, and the
    /// game's heap runs out long before that.
    /// </summary>
    internal static class Gzip
    {
        private const long MaxBytes = 64L * 1024 * 1024;

        internal static byte[] Compress(byte[] raw)
        {
            using (var output = new MemoryStream())
            {
                using (var gzip = new GZipStream(output, CompressionLevel.Fastest, true))
                {
                    gzip.Write(raw, 0, raw.Length);
                }
                return output.ToArray();
            }
        }

        internal static byte[] Decompress(byte[] compressed)
        {
            using (var input = new GZipStream(new MemoryStream(compressed), CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                var buffer = new byte[16384];
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    output.Write(buffer, 0, read);
                    if (output.Length > MaxBytes)
                    {
                        throw new InvalidDataException("packed data expands beyond " + MaxBytes / (1024 * 1024) + " MB");
                    }
                }
                return output.ToArray();
            }
        }
    }
}
