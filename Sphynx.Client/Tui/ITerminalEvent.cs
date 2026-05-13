// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Concurrent;
using System.Diagnostics;
using Sphynx.Utils;

namespace Sphynx.Client.Tui
{
    public interface ITerminalEvent
    {
        internal Terminal Terminal { get; }
        TerminalEvent AsEvent { get; }
    }

    public interface ITerminalMouseEvent : ITerminalEvent
    {
        (int X, int Y) Position { get; }
    }

    public readonly record struct TerminalKeyEvent(Terminal Terminal, TerminalKey Key) : ITerminalEvent
    {
        public TerminalEvent AsEvent => (TerminalEvent)this;
        public static implicit operator TerminalEvent(TerminalKeyEvent e) => new() { KeyEvent = e };
    }

    public readonly record struct TerminalWindowEvent(Terminal Terminal, (int Lines, int Columns) OldSize, (int Lines, int Columns) NewSize) : ITerminalEvent
    {
        public TerminalEvent AsEvent => (TerminalEvent)this;
        public static implicit operator TerminalEvent(TerminalWindowEvent e) => new() { WindowEvent = e };
    }

    public readonly record struct TerminalMouseMoveEvent(Terminal Terminal, (int X, int Y) OldPos, (int X, int Y) NewPos,
            TerminalKeyModifiers Mods = TerminalKeyModifiers.None) : ITerminalMouseEvent
    {
        public TerminalEvent AsEvent => (TerminalEvent)this;
        public (int X, int Y) Position => NewPos;

        public static implicit operator TerminalEvent(TerminalMouseMoveEvent e) => new() { MouseMoveEvent = e };
    }

    public readonly record struct TerminalMouseClickEvent(Terminal Terminal, (int X, int Y) Position,
            TerminalMouseButtons Buttons, TerminalKeyModifiers Mods = TerminalKeyModifiers.None, int ClickCount = 1) : ITerminalMouseEvent
    {
        public TerminalEvent AsEvent => (TerminalEvent)this;
        public static implicit operator TerminalEvent(TerminalMouseClickEvent e) => new() { MouseClickEvent = e };
    }

    public readonly record struct TerminalMouseScrollEvent(Terminal Terminal, (int X, int Y) Position, int Delta, TerminalScrollDirection Direction,
            TerminalKeyModifiers Mods = TerminalKeyModifiers.None) : ITerminalMouseEvent
    {
        public TerminalEvent AsEvent => (TerminalEvent)this;
        public static implicit operator TerminalEvent(TerminalMouseScrollEvent e) => new() { MouseScrollEvent = e };
    }

    public readonly struct TerminalEvent
    {
        public readonly TerminalKeyEvent? KeyEvent { get; init; }
        public readonly TerminalWindowEvent? WindowEvent { get; init; }
        public readonly TerminalMouseMoveEvent? MouseMoveEvent { get; init; }
        public readonly TerminalMouseClickEvent? MouseClickEvent { get; init; }
        public readonly TerminalMouseScrollEvent? MouseScrollEvent { get; init; }

        public Type? EventType
        {
            get
            {
                if (KeyEvent is not null) return typeof(TerminalKeyEvent);
                if (WindowEvent is not null) return typeof(TerminalWindowEvent);
                if (MouseMoveEvent is not null) return typeof(TerminalMouseMoveEvent);
                if (MouseClickEvent is not null) return typeof(TerminalMouseClickEvent);
                if (MouseScrollEvent is not null) return typeof(TerminalMouseScrollEvent);
                return null;
            }
        }

        public bool IsEmpty => EventType is null;
    }

    public sealed class TerminalEventPoller
    {
        private readonly Terminal _term;
        private readonly BlockingCollection<TerminalEvent> _pendingEvents = new(new ConcurrentQueue<TerminalEvent>());

        private static readonly Type[] _eventTypes
            = [typeof(TerminalKeyEvent), typeof(TerminalWindowEvent), typeof(TerminalMouseMoveEvent), typeof(TerminalMouseClickEvent), typeof(TerminalMouseScrollEvent)];
        private List<Type> _currentTypes = new(_eventTypes.Length);

        /// <summary>
        /// Poll rate for events which must be polled manually.
        /// </summary>
        public TimeSpan PollRate { get; set; } = TimeSpan.FromMilliseconds(1000.0 / 60);

        public TerminalEventPoller(Terminal term)
        {
            _term = term;
            RefreshPollableTypes();
        }

        /// <summary>
        /// Updates the list of tasks which the provided Terminal may be unable to handle.
        /// </summary>
        public void RefreshPollableTypes()
        {
            lock (_currentTypes)
            {
                _currentTypes.Clear();
                _currentTypes.AddRange(_eventTypes);

                using (var remover = _currentTypes.CreateRemover())
                {
                    foreach (var eventType in _eventTypes)
                    {
                        var isMouseEvent = eventType.IsAssignableTo(typeof(ITerminalMouseEvent));
                        if (_term.CanPollEvent(eventType))
                        {
                            if (isMouseEvent)
                                Debug.Assert(_term.HasMouseSupport);
                            remover.Enqueue(eventType);
                        }
                        else
                        {
                            if (isMouseEvent && !_term.HasMouseSupport)
                            {
                                remover.Enqueue(eventType);
                            }
                        }
                    }
                }
            }
        }

        public TerminalEvent PollEvent() => PollEvent(Timeout.InfiniteTimeSpan);

        public TerminalEvent PollEvent(TimeSpan timeout)
        {
            if (timeout != Timeout.InfiniteTimeSpan) timeout = timeout < TimeSpan.Zero ? TimeSpan.Zero : timeout;
            TerminalEvent ev = default;
            var sp = new SpinWait();
            while (ev.IsEmpty)
            {
                PollCurrentTypes();
                if (_pendingEvents.TryTake(out var item)) return item;

                var waitTime = PollRate;
                if (timeout != Timeout.InfiniteTimeSpan)
                    waitTime = waitTime < timeout ? waitTime : timeout;

                ev = _term.PollEvent(waitTime);

                if (timeout != Timeout.InfiniteTimeSpan)
                {
                    if (timeout <= TimeSpan.Zero) break;
                    timeout = waitTime >= timeout ? TimeSpan.Zero : timeout - waitTime;
                }

                sp.SpinOnce();
            }
            return ev;
        }

        private void PollCurrentTypes()
        {
            lock (_currentTypes)
            {
                foreach (var type in _currentTypes)
                {
                    if (type == typeof(TerminalWindowEvent))
                    {
                        PollWindowSize();
                    }
                }
            }
        }
        private (int Lines, int Columns)? _currentWindowSize;
        private void PollWindowSize()
        {
            (int Lines, int Columns) newSize = (_term.Lines, _term.Columns);

            _currentWindowSize ??= newSize;

            if (_currentWindowSize != newSize)
            {
                var oldSize = _currentWindowSize.Value;
                _currentWindowSize = newSize;
                _pendingEvents.Add(new TerminalWindowEvent(_term, oldSize, newSize));
            }
        }
    }
}
