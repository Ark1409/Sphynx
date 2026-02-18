// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Concurrent;
using Sphynx.Utils;

namespace Sphynx.Client.Tui
{
    public interface ITerminalEvent
    {
        internal Terminal Terminal { get; }
    }

    public readonly record struct TerminalKeyEvent(Terminal Terminal, TerminalKey Key) : ITerminalEvent
    {
        public static implicit operator TerminalEvent(TerminalKeyEvent e) => new() { KeyEvent = e };
    }

    public readonly record struct TerminalWindowEvent(Terminal Terminal, (int Lines, int Columns) OldSize, (int Lines, int Columns) NewSize) : ITerminalEvent
    {
        public static implicit operator TerminalEvent(TerminalWindowEvent e) => new() { WindowEvent = e };
    }

    public readonly record struct TerminalMouseMoveEvent(Terminal Terminal, (int X, int Y) OldPos, (int X, int Y) NewPos,
            TerminalKeyModifiers Mods = TerminalKeyModifiers.None) : ITerminalEvent
    {
        public static implicit operator TerminalEvent(TerminalMouseMoveEvent e) => new() { MouseMoveEvent = e };
    }

    public readonly record struct TerminalMouseClickEvent(Terminal Terminal, (int X, int Y) Position,
            TerminalMouseButtons Buttons, TerminalKeyModifiers Mods = TerminalKeyModifiers.None, int ClickCount = 1) : ITerminalEvent
    {
        public static implicit operator TerminalEvent(TerminalMouseClickEvent e) => new() { MouseClickEvent = e };
    }

    public readonly record struct TerminalMouseScrollEvent(Terminal Terminal, (int X, int Y) Position, int Delta, TerminalScrollDirection Direction,
            TerminalKeyModifiers Mods = TerminalKeyModifiers.None) : ITerminalEvent
    {
        public static implicit operator TerminalEvent(TerminalMouseScrollEvent e) => new() { MouseScrollEvent = e };
    }

    public readonly struct TerminalEvent
    {
        public readonly TerminalKeyEvent? KeyEvent { get; init; }
        public readonly TerminalWindowEvent? WindowEvent { get; init; }
        public readonly TerminalMouseMoveEvent? MouseMoveEvent { get; init; }
        public readonly TerminalMouseClickEvent? MouseClickEvent { get; init; }
        public readonly TerminalMouseScrollEvent? MouseScrollEvent { get; init; }
    }

    public class TerminalEventPoller
    {
        private readonly Terminal _term;
        private ConcurrentQueue<TerminalEvent> _pendingEvents = new();
        private Task? _task;
        private bool _shouldRunTask = false;
        private static readonly Type[] _eventTypes
            = [typeof(TerminalKeyEvent), typeof(TerminalWindowEvent), typeof(TerminalMouseMoveEvent), typeof(TerminalMouseClickEvent), typeof(TerminalMouseScrollEvent)];
        private List<Type> _currentTypes = new(_eventTypes.Length);
        private readonly object _currentTypesLock = new();

        public TerminalEventPoller(Terminal term)
        {
            _term = term;
        }

        public void Start()
        {
            Refresh();
        }

        public void Refresh()
        {
            lock (_currentTypesLock)
            {
                _currentTypes.Clear();
                _currentTypes.AddRange(_eventTypes);

                using (var remover = _currentTypes.CreateRemover())
                {
                    foreach (var eventType in _eventTypes)
                    {
                        if (_term.CanPollEvent(eventType))
                        {
                            remover.Enqueue(eventType);
                        }
                    }
                }

                _shouldRunTask = _currentTypes.Count > 0;
            }

            if (_shouldRunTask)
            {
                if (_task == null || _task.IsCompleted)
                    _task = Task.Factory.StartNew(PollRunner, TaskCreationOptions.LongRunning);
            }
            else
                _task = null;
        }

        public TerminalEvent PollEvent()
        {
            if (_task == null) Refresh();
            if (_pendingEvents.TryDequeue(out var ev))
            {
                return ev;
            }

            return _term.PollEvent();
        }

        private void PollRunner()
        {
            while (_shouldRunTask)
            {
                lock (_currentTypesLock)
                {
                    foreach (var type in _currentTypes)
                    {
                        if (type == typeof(TerminalWindowEvent))
                        {
                            PollWindowSize();
                        }
                    }
                }
                Thread.Sleep(1000 / 60);
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
                _pendingEvents.Enqueue(new TerminalWindowEvent(_term, oldSize, newSize));
            }
        }
    }
}
