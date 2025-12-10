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
    /// Interface for objects that have a disabled state.
    /// </summary>
    public interface ICanBeDisabled
    {
        /// <summary>
        /// An event which is raised when <see cref="Disabled"/>'s state has changed.
        /// </summary>
        event Action<bool> DisabledChanged;

        /// <summary>
        /// Bind an action to <see cref="DisabledChanged"/> with the option of running the bound action once immediately.
        /// </summary>
        /// <param name="onChange">The action to perform when <see cref="Disabled"/> changes.</param>
        /// <param name="runOnceImmediately">Whether the action provided in <paramref name="onChange"/> should be run once immediately.</param>
        void BindDisabledChanged(Action<bool> onChange, bool runOnceImmediately = false);

        /// <summary>
        /// Whether this object has been disabled.
        /// </summary>
        bool Disabled { get; }
    }
}
