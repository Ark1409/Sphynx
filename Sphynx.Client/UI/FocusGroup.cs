using System.Collections;

namespace Sphynx.Client.UI
{
    public class FocusGroup<T> : IEnumerable<T>, IFocusable where T : IFocusable
    {
        private List<T> Objects { get; }

        public int TargetIndex
        {
            get => _index;
            set
            {
                if (value >= Objects.Count || (value < 0 && Objects.Count > 0)) throw new ArgumentOutOfRangeException(nameof(value));
                _index = value;
            }
        }

        public T Target
        {
            get => GetFocusTarget();
            set
            {
                int index = Objects.IndexOf(value);
                if (index != -1) TargetIndex = index;
                else
                {
                    Objects.Add(value);
                    TargetIndex = Objects.Count - 1;
                }
            }
        }

        public int Count => Objects.Count;

        public static readonly Func<ConsoleKeyInfo, int?> DefaultSwapPredicate
            = (info => info.KeyChar == '\t' ? ((info.Modifiers & ConsoleModifiers.Shift) == ConsoleModifiers.Shift ? -1 : 1) : null);

        public Func<ConsoleKeyInfo, int?>? SwapPredicate { get; set; } = DefaultSwapPredicate;

        private int _index = -1;

        public FocusGroup()
        {
            Objects = new();
        }

        public FocusGroup(IEnumerable<T> list, int focusIndex = -1)
        {
            Objects = new List<T>(list ?? throw new ArgumentNullException(nameof(list)));
            if (focusIndex >= Objects.Count) throw new ArgumentOutOfRangeException(nameof(focusIndex));
            if (Objects.Count > 0) focusIndex = Math.Max(0, focusIndex);
            _index = Math.Max(-1, focusIndex);
        }

        public T GetFocusTarget() => _index < 0 ? throw new ArgumentOutOfRangeException(nameof(_index)) : Objects[_index];

        public FocusGroup<T> ShiftFocus(int count = 1)
        {
            switch (Objects.Count)
            {
                case 0:
                    _index = -1;
                    return this;
                case 1:
                    return this;
                default:
                    if (count == 0) return this;
                    if (TargetIndex != -1 && Target is IFocusable f) { f.OnLeave(); }
                    int val = (_index + count) % Objects.Count;
                    _index = val + -Math.Clamp(val, -1, 0) * Objects.Count;
                    if (Target is IFocusable f1) { f1.OnFocus(); }
                    return this;
            }
        }

        public FocusGroup<T> SwapToTarget(int index)
        {
            TargetIndex = index;
            return this;
        }

        public FocusGroup<T> AddObject(T obj)
        {
            Objects.Add(obj);
            _index = Math.Max(0, _index);
            return this;
        }

        public T GetObject(int index) => index >= Objects.Count ? throw new ArgumentOutOfRangeException(nameof(index)) : Objects[index];

        public IEnumerator<T> GetEnumerator() => Objects.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public bool HandleKey(in ConsoleKeyInfo key)
        {
            int? swapCount = SwapPredicate?.Invoke(key);

            if (swapCount is not null)
            {
                ShiftFocus(swapCount.Value);
                return true;
            }

            if (_index >= 0 && Target is IFocusable f)
            {
                return f.HandleKey(key);
            }
            return false;
        }

        public void OnFocus()
        {
            if (_index >= 0 && Target is IFocusable f)
            {
                f.OnFocus();
            }
        }

        public void OnLeave()
        {
            if (_index >= 0 && Target is IFocusable f)
            {
                f.OnLeave();
            }
        }
    }
}
