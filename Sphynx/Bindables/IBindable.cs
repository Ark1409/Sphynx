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
    /// An interface which can be bound to other <see cref="IBindable"/>s in order to watch for (and react to) <see cref="ICanBeDisabled.Disabled">Disabled</see> changes.
    /// </summary>
    public interface IBindable : ICanBeDisabled, IHasDefaultValue, IUnbindable
    {
        /// <summary>
        /// Binds ourselves to another bindable such that we receive any value limitations of the bindable we bind with.
        /// </summary>
        /// <param name="them">The foreign bindable. This should always be the most permanent end of the bind (ie. a ConfigManager)</param>
        void BindTo(IBindable them);

        /// <summary>
        /// An alias of <see cref="BindTo"/> provided for use in object initializer scenarios.
        /// Passes the provided value as the foreign (more permanent) bindable.
        /// </summary>
        sealed IBindable BindTarget
        {
            set => BindTo(value);
        }

        /// <summary>
        /// Retrieve a new bindable instance weakly bound to the configuration backing.
        /// If you are further binding to events of a bindable retrieved using this method, ensure to hold
        /// a local reference.
        /// </summary>
        /// <returns>A weakly bound copy of the specified bindable.</returns>
        /// <exception cref="InvalidOperationException">Thrown when attempting to instantiate a copy bindable that's not matching the original's type.</exception>
        IBindable GetBoundCopy();

        /// <summary>
        /// Creates a new instance of this <see cref="IBindable"/> for use in <see cref="GetBoundCopy"/>.
        /// The returned instance must have match the most derived type of the bindable class this method is implemented on.
        /// </summary>
        protected IBindable CreateInstance();

        /// <summary>
        /// Helper method which implements <see cref="GetBoundCopy"/> for use in final classes.
        /// </summary>
        /// <param name="source">The source <see cref="IBindable"/>.</param>
        /// <typeparam name="T">The bindable type.</typeparam>
        /// <returns>The bound copy.</returns>
        protected static T GetBoundCopyImplementation<T>(T source)
            where T : IBindable
        {
            var copy = source.CreateInstance();

            if (copy.GetType() != source.GetType())
            {
                throw new InvalidOperationException($"Attempted to create a copy of {source.GetType().Name}, but the returned instance type was {copy.GetType().Name}. "
                                                           + $"Override {source.GetType().Name}.{nameof(CreateInstance)}() for {nameof(GetBoundCopy)}() to function properly.");
            }

            copy.BindTo(source);
            return (T)copy;
        }
    }

    /// <summary>
    /// An interface which can be bound to other <see cref="IBindable{T}"/>s in order to watch for (and react to) <see cref="ICanBeDisabled.Disabled">Disabled</see> and <see cref="IBindable{T}.Value">Value</see> changes.
    /// </summary>
    /// <typeparam name="T">The type of value encapsulated by this <see cref="IBindable{T}"/>.</typeparam>
    public interface IBindable<T> : ICanBeDisabled, IHasDefaultValue, IUnbindable
    {
        /// <summary>
        /// An event which is raised when <see cref="Value"/> has changed.
        /// </summary>
        event Action<ValueChangedEvent<T>> ValueChanged;

        /// <summary>
        /// The current value of this bindable.
        /// </summary>
        T Value { get; }

        /// <summary>
        /// The default value of this bindable. Used when querying <see cref="IHasDefaultValue.IsDefault">IsDefault</see>.
        /// </summary>
        T Default { get; }

        /// <summary>
        /// Binds ourselves to another bindable such that we receive any values and value limitations of the bindable we bind width.
        /// </summary>
        /// <param name="them">The foreign bindable. This should always be the most permanent end of the bind.</param>
        void BindTo(IBindable<T> them);

        /// <summary>
        /// An alias of <see cref="BindTo"/> provided for use in object initializer scenarios.
        /// Passes the provided value as the foreign (more permanent) bindable.
        /// </summary>
        IBindable<T> BindTarget
        {
            set => BindTo(value);
        }

        /// <summary>
        /// Bind an action to <see cref="ValueChanged"/> with the option of running the bound action once immediately.
        /// </summary>
        /// <param name="onChange">The action to perform when <see cref="Value"/> changes.</param>
        /// <param name="runOnceImmediately">Whether the action provided in <paramref name="onChange"/> should be run once immediately.</param>
        void BindValueChanged(Action<ValueChangedEvent<T>> onChange, bool runOnceImmediately = false);

        /// <inheritdoc cref="IBindable.GetBoundCopy"/>
        IBindable<T> GetBoundCopy();

        /// <summary>
        /// Retrieves a weak reference to this bindable.
        /// </summary>
        internal WeakReference<Bindable<T>> GetWeakReference();
    }
}
