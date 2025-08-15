// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Bindables
{
    // Copyright (c) 2024 ppy Pty Ltd <contact@ppy.sh>.
    //
    // Permission is hereby granted, free of charge, to any person obtaining a copy
    // of this software and associated documentation files (the "Software"), to deal
    // in the Software without restriction, including without limitation the rights
    // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    // copies of the Software, and to permit persons to whom the Software is
    // furnished to do so, subject to the following conditions:
    //
    // The above copyright notice and this permission notice shall be included in
    // all copies or substantial portions of the Software.

    /// <summary>
    /// An interface that represents a leased bindable.
    /// </summary>
    public interface ILeasedBindable : IReadOnlyBindable, IUnbindable
    {
        /// <summary>
        /// End the lease on the source <see cref="IReadOnlyBindable"/>.
        /// </summary>
        /// <returns>
        /// Whether the lease was returned by this call. Will be <c>false</c> if already returned.
        /// </returns>
        bool Return();
    }

    /// <summary>
    /// An interface that representes a leased bindable.
    /// </summary>
    /// <typeparam name="T">The value type of the bindable.</typeparam>
    public interface ILeasedBindable<T> : ILeasedBindable, IBindable<T>
    {
    }
}
