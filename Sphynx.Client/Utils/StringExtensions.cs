// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Utils;

namespace Sphynx.Client.Utils
{
    public static class StringExtensions
    {
        public static int TerminalLineCount(this string str) => str.LineCount("\n");
    }
}
