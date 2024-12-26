// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Net;

namespace Sphynx
{
    //
    // Generation code for a SnowflakeId.
    //
    // The current implementation was profiled against many (functional) lockless alternatives,
    // and while most performed about the same or even better than, this one was ultimately
    // chosen given that it has proven to be the most optimal for high-contention scenarios,
    // while still offering similar performance to the others when contention is low.
    //

    public readonly partial struct SnowflakeId
    {
        private static long _lastTimestamp;
        private static ushort _lastSequence;
        private static readonly object _idLock = new object();

        private const ushort MAX_SEQUENCE = ushort.MaxValue;
        private static readonly ushort MACHINE_ID = GetMachineId();

        private static string MachineName
        {
            get
            {
                try
                {
                    return Dns.GetHostName();
                }
                catch
                {
                    try
                    {
                        return Environment.MachineName;
                    }
                    catch
                    {
                        return "";
                    }
                }
            }
        }

        /// <summary>
        /// Generates a new <see cref="SnowflakeId"/>
        /// </summary>
        /// <returns>A new <see cref="SnowflakeId"/>.</returns>
        public static SnowflakeId NewId()
        {
            while (true)
            {
                lock (_idLock)
                {
                    long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                    if (timestamp == _lastTimestamp)
                    {
                        if (_lastSequence == MAX_SEQUENCE)
                        {
                            continue;
                        }

                        _lastSequence++;
                    }
                    else
                    {
                        _lastSequence = 0;
                    }

                    _lastTimestamp = timestamp;

                    return new SnowflakeId(timestamp, MACHINE_ID, _lastSequence);
                }
            }
        }

        /// <summary>
        /// Returns a new <see cref="SnowflakeId"/> which only holds timestamp information.
        /// </summary>
        /// <returns>A new <see cref="SnowflakeId"/> where all bits are zeroed except the timestamp.</returns>
        public static SnowflakeId NewTimestamp()
        {
            return NewTimestamp(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        }

        /// <summary>
        /// Returns a new <see cref="SnowflakeId"/> which only holds timestamp information.
        /// </summary>
        /// <param name="timestamp">The timestamp for the <see cref="SnowflakeId"/>.</param>
        /// <returns>A new <see cref="SnowflakeId"/> where all bits are zeroed except the <paramref name="timestamp"/>.</returns>
        public static SnowflakeId NewTimestamp(long timestamp)
        {
            return new SnowflakeId(timestamp, 0, 0);
        }

        private static ushort GetMachineId()
        {
            int hash = HashCode.Combine(MachineName.GetHashCode(), Environment.ProcessId);
            return (ushort)(hash & 0xffff);
        }
    }
}
