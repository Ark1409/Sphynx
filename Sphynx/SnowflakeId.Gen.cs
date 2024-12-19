// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Sphynx
{
    public readonly partial struct SnowflakeId
    {
        private static long _lastTimestamp;
        private static int _lastSequence;
        private static int _incrementReservation;

        private const ushort MAX_SEQUENCE = ushort.MaxValue;
        private const short MACHINE_ID = 12345;

        public static SnowflakeId NewId()
        {
            while (true)
            {
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                if (!TryStartSequence(timestamp, out int? sequenceNumber))
                {
                    if (!TryIncrementSequence(timestamp, out sequenceNumber))
                    {
                        continue;
                    }
                }

                return new SnowflakeId(timestamp, MACHINE_ID, (short)(sequenceNumber.Value & 0xFFFF));
            }
        }

        private static bool TryStartSequence(long timestamp, [NotNullWhen(true)] out int? sequenceStart)
        {
            // I think we require an Interlocked read here; we cant misread it and (incorrectly) assume that someone else
            // already started when they havent actually
            long lastTimestamp = Interlocked.Read(ref _lastTimestamp);

            if (timestamp <= lastTimestamp)
            {
                sequenceStart = null;
                return false;
            }

            if (Interlocked.CompareExchange(ref _lastTimestamp, timestamp, lastTimestamp) == lastTimestamp)
            {
                Interlocked.Exchange(ref _lastSequence, 0);
                sequenceStart = 0;
                return true;
            }

            sequenceStart = null;
            return false;
        }

        private static bool TryIncrementSequence(long timestamp, [NotNullWhen(true)] out int? newSequence)
        {
            while (true)
            {
                int oldSequence = Volatile.Read(ref _lastSequence);
                if (oldSequence > MAX_SEQUENCE)
                {
                    SpinUntilNextMillis(timestamp);
                    newSequence = null;
                    return false;
                }

                if (!TryReserveIncrement(timestamp))
                {
                    newSequence = null;
                    return false;
                }

                newSequence = oldSequence + 1;

                // What we can do is instead of locking, is compare_xchg a bool (or rather an int)
                // and if we get got it, try to compare_xchg the incremented one into _lastSequence (we still need to do
                // this even though we've basically reserved a spot for ourselves because of the TryStartSequence, they
                // also work with (i.e. xchg) _lastSequence); else, spinwait will checking
                // if timestamp != DateTimeOffset.UtcNow.ToUnitTimeMilliseconds(), and ret false in that case

                if (Interlocked.CompareExchange(ref _lastSequence, newSequence.Value, oldSequence) == oldSequence)
                {
                    Interlocked.Exchange(ref _incrementReservation, 0);
                    break;
                }
                else
                {
                    Interlocked.Exchange(ref _incrementReservation, 0);
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryReserveIncrement(long currentTimestamp)
        {
            SpinWait spinWait = default;
            do
            {
                // Allow threads which are spinning to. Acts as sort of timeout
                if (currentTimestamp != DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                {
                    return false;
                }

                spinWait.SpinOnce();
            } while (Interlocked.CompareExchange(ref _incrementReservation, 1, 0) == 0);

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SpinUntilNextMillis(long currentTimestamp)
        {
            SpinWait spinWait = default;
            while (currentTimestamp != DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            {
                spinWait.SpinOnce();
            }
        }
    }
}
