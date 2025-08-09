// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Nerdbank.Streams;
using Sphynx.Storage;

namespace Sphynx.Test.Network.Serialization
{
    public abstract class SerializerTest
    {
        [NotNull] protected SequencePool.Rental? SequenceRental;
        protected Sequence<byte> Sequence => SequenceRental.Value.Value;

        [SetUp]
        public void SetUp()
        {
            Debug.Assert(SequenceRental == null);

            SequenceRental = SequencePool.Shared.Rent();
        }

        [TearDown]
        public void TearDown()
        {
            Debug.Assert(SequenceRental != null);

            SequenceRental.Value.Dispose();
            SequenceRental = null!;
        }
    }
}
