using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Sphynx.Server
{
    internal static class StructExtensions
    {
        public static byte[] ToByteArray<T>(this T structure) where T : struct
        {
            var bufferSize = Marshal.SizeOf(structure);
            var byteArray = new byte[bufferSize];

            IntPtr handle = Marshal.AllocHGlobal(bufferSize);

            try
            {
                Marshal.StructureToPtr(structure, handle, true);
                Marshal.Copy(handle, byteArray, 0, bufferSize);
            }
            finally
            {
                Marshal.FreeHGlobal(handle);
            }

            return byteArray;
        }

        public static T? ToStructure<T>(this byte[] byteArray) where T : struct
        {
            var packet = new T();
            var bufferSize = Marshal.SizeOf(packet);
            IntPtr handle = Marshal.AllocHGlobal(bufferSize);
            T? structure;

            try
            {
                Marshal.Copy(byteArray, 0, handle, bufferSize);
                structure = Marshal.PtrToStructure<T>(handle);
            }
            finally
            {
                Marshal.FreeHGlobal(handle);
            }

            return structure;
        }
    }
}
