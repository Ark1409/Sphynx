// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Text;

namespace Sphynx.Client.Utils
{
    internal static class Encodings
    {
        public static readonly UTF32Encoding UTF32LE = new UTF32Encoding(false, false);
        public static readonly UTF32Encoding UTF32BE = new UTF32Encoding(true, false);
        public static readonly UTF32Encoding UTF32 = UTF32LE;
        public static readonly UTF8Encoding UTF8 = new (false);
    }
}
