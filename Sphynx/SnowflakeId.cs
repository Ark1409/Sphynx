// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;

namespace Sphynx
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly partial struct SnowflakeId
    {
        public long Timestamp { get; }
        public short MachineId { get; }
        public short SequenceNumber { get; }

        public SnowflakeId(long timestamp, short machineId, short sequenceNumber)
        {
            Timestamp = timestamp;
            MachineId = machineId;
            SequenceNumber = sequenceNumber;
        }


    }
}

