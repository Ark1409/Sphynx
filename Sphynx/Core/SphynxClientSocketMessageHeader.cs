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
    public class SphynxClientSocketMessageHeader : SphynxSocketMessageHeader
    {
        public enum MessageType : byte
        {
            Login = 1,
            Logout,

            GetUsername,
            ChangeUsername,
            ChangePassword,
            GetStatus,
            ChangeStatus,
            GetId,

            SendMessage,
            ListLobbies
        }

        public MessageType Type { get; set; }

        public override byte[] Serialize()
        {
            SphynxClientMessageHeaderData data;
            data.Version = Version.ToInt32();
            data.Type = (byte)Type;
            data.Timestamp = Timestamp.ToBinary();
            data.ContentLength = ContentLength;

            byte[] bytes = new byte[Marshal.SizeOf<SphynxClientMessageHeaderData>()];

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

        public static SphynxClientSocketMessageHeader Deserialize([NotNull] byte[] bytes)
        {
            IntPtr ptr = Marshal.AllocHGlobal(bytes.Length);
            SphynxClientMessageHeaderData? data;

            try
            {
                Marshal.Copy(bytes, 0, ptr, bytes.Length);
                data = Marshal.PtrToStructure<SphynxClientMessageHeaderData>(ptr);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            return Deserialize(in data);
        }

        public static SphynxClientSocketMessageHeader Deserialize(in SphynxClientMessageHeaderData? data)
        {
            var val = data ?? throw new ArgumentNullException(nameof(data));

            return new SphynxClientSocketMessageHeader
            {
                Version = Version.FromInt32(val.Version),
                Type = (MessageType)val.Type,
                Timestamp = DateTime.FromBinary(val.Timestamp),
                ContentLength = val.ContentLength
            };
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SphynxClientMessageHeaderData
    {
        
        public Int32 Version;
        public UInt8 Type;
        public Int64 Timestamp;
        public Int32 ContentLength;
    }
}
