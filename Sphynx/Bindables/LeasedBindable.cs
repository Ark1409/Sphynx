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
    /// A bindable carrying a mutually exclusive lease on another bindable.
    /// Can only be retrieved via <see cref="Bindable{T}.BeginLease"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LeasedBindable<T> : Bindable<T>, ILeasedBindable<T>
    {
        private readonly Bindable<T> source;

        private readonly T valueBeforeLease;
        private readonly bool disabledBeforeLease;
        private readonly bool revertValueOnReturn;

        internal LeasedBindable(Bindable<T> source, bool revertValueOnReturn) : base(default!)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));

            BindTo(source);

            if (revertValueOnReturn)
            {
                this.revertValueOnReturn = true;
                valueBeforeLease = Value;
            }

            disabledBeforeLease = Disabled;

            Disabled = true;
        }

        private LeasedBindable(T defaultValue = default!)
            : base(defaultValue)
        {
            // used for GetBoundCopy/CreateInstance, where we don't want a source.
        }

        private bool hasBeenReturned;

        public bool Return()
        {
            if (hasBeenReturned)
                return false;

            if (source == null)
                throw new InvalidOperationException($"Must {nameof(Return)} from original leased source");

            UnbindAll();
            return true;
        }

        public override T Value
        {
            get => base.Value;
            set
            {
                if (source != null)
                    checkValid();

                if (EqualityComparer<T>.Default.Equals(Value, value)) return;

                SetValue(base.Value, value, true);
            }
        }

        public override T Default
        {
            get => base.Default;
            set
            {
                if (source != null)
                    checkValid();

                if (EqualityComparer<T>.Default.Equals(Default, value)) return;

                SetDefaultValue(base.Default, value, true);
            }
        }

        public override bool Disabled
        {
            get => base.Disabled;
            set
            {
                if (source != null)
                    checkValid();

                if (Disabled == value) return;

                SetDisabled(value, true);
            }
        }

        internal override void UnbindAllInternal()
        {
            if (source != null && !hasBeenReturned)
            {
                if (revertValueOnReturn)
                    Value = valueBeforeLease;

                Disabled = disabledBeforeLease;

                source.EndLease(this);
                hasBeenReturned = true;
            }

            base.UnbindAllInternal();
        }

        public new LeasedBindable<T> GetBoundCopy() => (LeasedBindable<T>)base.GetBoundCopy();
        protected override Bindable<T> CreateInstance() => new LeasedBindable<T>();

        private void checkValid()
        {
            if (source != null && hasBeenReturned)
                throw new InvalidOperationException($"Cannot perform operations on a {nameof(LeasedBindable<T>)} that has been {nameof(Return)}ed.");
        }
    }
}
