using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using UInt8 = System.Byte;

namespace Sphynx.Core
{
    public class SphynxSeverSocketMessageHeader : SphynxSocketMessageHeader
    {
        public enum MessageType
        {
            SendMessage
        }

        public MessageType Type { get; set; }

        public override byte[] Serialize()
        {
            SphynxServerMessageHeaderData data;

            data.Version = Version.ToInt32();
            data.Type = (byte)Type;
            data.Timestamp = Timestamp.ToBinary();
            data.ContentLength = ContentLength;

            byte[] bytes = new byte[Marshal.SizeOf<SphynxServerMessageHeaderData>()];

            IntPtr ptr = Marshal.AllocHGlobal(bytes.Length);
            try
            {
                Marshal.StructureToPtr(data, ptr, false);
                Marshal.Copy(ptr, bytes, 0, bytes.Length);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            return bytes;
        }

        public static SphynxSeverSocketMessageHeader Deserialize([NotNull] byte[] bytes)
        {
            IntPtr ptr = Marshal.AllocHGlobal(bytes.Length);
            SphynxServerMessageHeaderData? data;

            try
            {
                Marshal.Copy(bytes, 0, ptr, bytes.Length);
                data = Marshal.PtrToStructure<SphynxServerMessageHeaderData>(ptr);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            return Deserialize(in data);
        }

        public static SphynxSeverSocketMessageHeader Deserialize(in SphynxServerMessageHeaderData? data)
        {
            var val = data ?? throw new ArgumentNullException(nameof(data));

            return new SphynxSeverSocketMessageHeader
            {
                Version = Version.FromInt32(val.Version),
                Type = (MessageType)val.Type,
                Timestamp = DateTime.FromBinary(val.Timestamp),
                ContentLength = val.ContentLength
            };
        }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SphynxServerMessageHeaderData
    {
        public Int32 Version;
        public UInt8 Type;
        public Int64 Timestamp;
        public Int32 ContentLength;
    }
}
