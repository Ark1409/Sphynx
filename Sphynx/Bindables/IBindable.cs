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
    /// Provides a read-only view over any bindable object.
    /// Bindables are objects which can be listened to for statful changes.
    /// </summary>
    public interface IReadOnlyBindable : ICanBeDisabled, IHasDefaultValue, IFormattable
    {
        /// <summary>
        /// Retrieve a new bindable instance weakly bound to the configuration backing.
        /// If you are further binding to events of a bindable retrieved using this method, ensure to hold
        /// a local reference.
        /// </summary>
        /// <returns>A weakly bound copy of the specified bindable.</returns>
        /// <exception cref="InvalidOperationException">Thrown when attempting to instantiate a copy bindable that's not matching the original's type.</exception>
        IReadOnlyBindable GetBoundCopy();

        /// <summary>
        /// Retrieves a weak reference to this bindable.
        /// </summary>
        protected internal WeakReference<IReadOnlyBindable> WeakReference { get; }

        /// <summary>
        /// Binds an <see cref="Action{IReadOnlyBindable}"/> to occur when the bindable object changes in any way.
        /// </summary>
        /// <param name="action">The action to bind.</param>
        /// <param name="runOnceImmediately">Whether to run the action once before this function returns.</param>
        void BindObjectChanged(Action<IReadOnlyBindable> action, bool runOnceImmediately = false);
    }

    /// <summary>
    /// A covariant read-only representation of a bindable object which holds a singular value.
    /// </summary>
    /// <typeparam name="T">The type of value encapsulated by this <see cref="IBindable{T}"/>.</typeparam>
    /// <seealso cref="IReadOnlyBindable{T}"/>
    public interface IReadOnlyValuedBindable<out T> : IReadOnlyBindable
    {
        /// <summary>
        /// The current value of this bindable.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// The default value of this bindable. Used when querying <see cref="IHasDefaultValue.IsDefault">IsDefault</see>.
        /// </summary>
        public T Default { get; }

        /// <inheritdoc cref="IReadOnlyBindable.GetBoundCopy"/>
        new IReadOnlyValuedBindable<T> GetBoundCopy();

        /// <summary>
        /// Binds an <see cref="Action{ValueChangedEvent}"/> to occur when the bindable object's value changes.
        /// </summary>
        /// <param name="onChange">The action to bind.</param>
        /// <param name="runOnceImmediately">Whether to run the action once before this function returns.</param>
        void BindValueChanged(Action<ValueChangedEvent<object?>> onChange, bool runOnceImmediately = false);

        IReadOnlyBindable IReadOnlyBindable.GetBoundCopy() => GetBoundCopy();

        void IReadOnlyBindable.BindObjectChanged(Action<IReadOnlyBindable> action, bool runOnceImmediately)
            => BindObjectChanged((Action<IReadOnlyValuedBindable<T>>)action, runOnceImmediately);

        /// <inheritdoc cref="IReadOnlyBindable.BindObjectChanged(Action{IReadOnlyBindable}, bool)"/>
        void BindObjectChanged(Action<IReadOnlyValuedBindable<T>> action, bool runOnceImmediately = false)
            => BindValueChanged(e => action(this), runOnceImmediately);
    }

    /// <summary>
    /// Represents a read-only view over bindable which holds a singular value.
    /// </summary>
    /// <typeparam name="T">The type of value encapsulated by this <see cref="IBindable{T}"/>.</typeparam>
    /// <seealso cref="IBindable{T}"/>
    public interface IReadOnlyBindable<T> : IReadOnlyValuedBindable<T>, IEquatable<T>
    {
        /// <summary>
        /// An event which is raised when <see cref="IReadOnlyValuedBindable{T}.Value"/> has changed.
        /// </summary>
        event Action<ValueChangedEvent<T>> ValueChanged;

        /// <inheritdoc cref="IReadOnlyValuedBindable{T}.BindValueChanged(Action{ValueChangedEvent{object?}}, bool)"/>
        void BindValueChanged(Action<ValueChangedEvent<T>> onChange, bool runOnceImmediately = false);

        /// <inheritdoc cref="IReadOnlyValuedBindable{T}.GetBoundCopy"/>
        new IReadOnlyBindable<T> GetBoundCopy();

        IReadOnlyValuedBindable<T> IReadOnlyValuedBindable<T>.GetBoundCopy() => GetBoundCopy();

        void IReadOnlyValuedBindable<T>.BindObjectChanged(Action<IReadOnlyValuedBindable<T>> action, bool runOnceImmediately)
            => BindObjectChanged((Action<IReadOnlyValuedBindable<T>>)action, runOnceImmediately);

        /// <inheritdoc cref="IReadOnlyValuedBindable{T}.BindValueChanged(Action{ValueChangedEvent{object?}}, bool)"/>
        void BindObjectChanged(Action<IReadOnlyBindable<T>> action, bool runOnceImmediately = false)
            => BindValueChanged(e => action(this), runOnceImmediately);

        void IReadOnlyValuedBindable<T>.BindValueChanged(Action<ValueChangedEvent<object?>> onChange, bool runOnceImmediately)
            => BindValueChanged(e => onChange(new(e.OldValue, e.NewValue)), runOnceImmediately);

        bool IEquatable<T>.Equals(T? other) => other != null && EqualityComparer<T>.Default.Equals(other);
    }

    /// <summary>
    /// Represents a bindable which holds a singular value.
    /// </summary>
    /// <typeparam name="T">The type of value encapsulated by this <see cref="IBindable{T}"/>.</typeparam>
    public interface IBindable<T> : IReadOnlyBindable<T>, IUnbindable
    {
        /// <inheritdoc cref="IReadOnlyValuedBindable{T}.Value"/>
        new T Value { get; set; }

        /// <inheritdoc cref="IReadOnlyBindable{T}.GetBoundCopy"/>
        new IBindable<T> GetBoundCopy();

        T IReadOnlyValuedBindable<T>.Value => Value;

        IReadOnlyBindable<T> IReadOnlyBindable<T>.GetBoundCopy() => GetBoundCopy();

        void IReadOnlyBindable<T>.BindObjectChanged(Action<IReadOnlyBindable<T>> action, bool runOnceImmediately)
            => BindObjectChanged((Action<IBindable<T>>)action, runOnceImmediately);

        /// <inheritdoc cref="IReadOnlyBindable{T}.BindObjectChanged(Action{IReadOnlyBindable{T}}, bool)"/>
        void BindObjectChanged(Action<IBindable<T>> action, bool runOnceImmediately = false)
            => BindValueChanged(e => action(this), runOnceImmediately);
    }
}
