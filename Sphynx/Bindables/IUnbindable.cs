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
    /// Interface for objects that support publicly unbinding events or <see cref="IBindable{T}"/>s.
    /// </summary>
    public interface IUnbindable
    {
        /// <summary>
        /// Unbinds all bound events.
        /// </summary>
        void UnbindEvents();

        /// <summary>
        /// Unbinds all bound <see cref="IBindable{T}"/>s.
        /// </summary>
        void UnbindBindings();

        /// <summary>
        /// Calls <see cref="UnbindEvents"/> and <see cref="UnbindBindings"/>
        /// </summary>
        void UnbindAll();

        /// <summary>
        /// Unbinds ourselves from another <see cref="IBindable{T}"/> such that we stop receiving updates it.
        /// The other <see cref="IBindable{T}"/> will also stop receiving any events from us.
        /// </summary>
        /// <param name="them">The other bindable.</param>
        void UnbindFrom(IUnbindable them);
    }
}
