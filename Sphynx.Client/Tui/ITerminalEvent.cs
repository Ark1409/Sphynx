// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Threading.Channels;
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
        private readonly BlockingCollection<TerminalEvent> _pendingEvents = new(new ConcurrentQueue<TerminalEvent>());

        private Task? _pollTask;
        private Task? _regularPollTask;
        private bool _shouldRunPollTask = false;
        private volatile bool _shouldRunRegularPollTask = false;
        private readonly object _needEventCountLock = new();
        private volatile int _needEventCount = 0;

        private static readonly Type[] _eventTypes
            = [typeof(TerminalKeyEvent), typeof(TerminalWindowEvent), typeof(TerminalMouseMoveEvent), typeof(TerminalMouseClickEvent), typeof(TerminalMouseScrollEvent)];
        private List<Type> _currentTypes = new(_eventTypes.Length);
        private readonly object _currentTypesLock = new();

        public bool AutoRefresh { get; set; } = false;

        public TerminalEventPoller(Terminal term)
        {
            _term = term;
        }

        /// <summary>
        /// Begin polling for those tasks which the provided Terminal may be unable to handle.
        /// </summary>
        public void Start()
        {
            Refresh();
        }

        /// <summary>
        /// Updates the list of tasks which the provided Terminal may be unable to handle.
        /// </summary>
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

                _shouldRunPollTask = _currentTypes.Count > 0;
            }

            if (_shouldRunPollTask)
            {
                if (_pollTask == null || _pollTask.IsCompleted)
                    _pollTask = Task.Factory.StartNew(PollRunner, TaskCreationOptions.LongRunning);
            }
            else
                _pollTask = AutoRefresh ? null : Task.CompletedTask;

            _shouldRunRegularPollTask = _shouldRunPollTask;
            if (_shouldRunRegularPollTask)
            {
                if (_regularPollTask == null || _regularPollTask.IsCompleted)
                    _regularPollTask = Task.Factory.StartNew(RegularPollRunner, TaskCreationOptions.LongRunning);
            }
            else
                _regularPollTask = AutoRefresh ? null : Task.CompletedTask;

            if (!_shouldRunPollTask || !_shouldRunRegularPollTask)
            {
                lock (_needEventCountLock)
                    Monitor.PulseAll(_needEventCountLock);
            }
        }

        public TerminalEvent PollEvent()
        {
            if (_pollTask == null || _regularPollTask == null) Refresh();

            if (!_shouldRunRegularPollTask && !_shouldRunPollTask)
            {
                if (_pendingEvents.TryTake(out var v))
                {
                    lock (_needEventCountLock)
                        _needEventCount++;
                    return v;
                }
                return _term.PollEvent();
            }

            lock (_needEventCountLock)
            {
                _needEventCount++;
                if (_needEventCount > 0)
                    Monitor.PulseAll(_needEventCountLock);
            }

            return _pendingEvents.Take();
        }

        private void RegularPollRunner()
        {
            while (_shouldRunRegularPollTask)
            {
                lock (_needEventCountLock)
                {
                    while (_needEventCount <= 0 && _shouldRunRegularPollTask)
                    {
                        Monitor.Wait(_needEventCountLock);
                    }
                }

                if (!_shouldRunRegularPollTask) break;

                var ev = _term.PollEvent();
                _pendingEvents.Add(ev);

                lock (_needEventCountLock)
                {
                    _needEventCount--;
                }
            }
        }

        private void PollRunner()
        {
            while (_shouldRunPollTask)
            {
                lock (_needEventCountLock)
                {
                    while (_needEventCount <= 0 && _shouldRunPollTask)
                    {
                        Monitor.Wait(_needEventCountLock);
                    }
                }

                if (!_shouldRunPollTask) break;

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

                Thread.Sleep(1000 / 120);
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
                lock (_needEventCountLock)
                {
                    _needEventCount--;
                }
            }
        }
    }
}
