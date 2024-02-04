namespace Sphynx.Server.Utils
{
    // Avoid enumeration so we don't need to use a semaphore... 
    internal sealed class SyncLinkedList<T>
    {
        private readonly LinkedList<SyncLinkedListNode<T>> _backing;

        public int Count
        {
            get
            {
                lock (_syncLock) return _backing.Count;
            }
        }

        public SyncLinkedListNode<T>? First
        {
            get
            {
                lock (_syncLock) return _backing.First?.Value;
            }
        }

        public SyncLinkedListNode<T>? Last
        {
            get
            {
                lock (_syncLock) return _backing.Last?.Value;
            }
        }

        public bool IsReadOnly => false;

        private readonly object _syncLock = new object();

        public SyncLinkedList()
        {
            _backing = new LinkedList<SyncLinkedListNode<T>>();
        }

        public SyncLinkedList(IEnumerable<T> enumerable) : this()
        {
            lock (_syncLock)
            {
                foreach (var item in enumerable)
                {
                    var syncNode = new SyncLinkedListNode<T>(this, _syncLock, item);
                    syncNode.UnderlyingNode = _backing.AddLast(syncNode);
                }
            }
        }

        public void ForAll(Action<T> action)
        {
            if (Count == 0) return;
            
            lock (_syncLock)
            {
                if (Count == 0) return;

                var node = _backing.First!;
                do
                {
                    action(node.Value.Value);
                    node = node.Next;
                } while (node is not null && node != _backing.First);
            }
        }

        public SyncLinkedListNode<T> AddAfter(SyncLinkedListNode<T> node, T value)
        {
            lock (_syncLock)
            {
                var newNode = new SyncLinkedListNode<T>(this, _syncLock, value);
                _backing.AddAfter(node.UnderlyingNode,
                    newNode.UnderlyingNode = new LinkedListNode<SyncLinkedListNode<T>>(newNode));
                return newNode;
            }
        }

        public void AddAfter(SyncLinkedListNode<T> node, SyncLinkedListNode<T> newNode)
        {
            lock (_syncLock) _backing.AddAfter(node.UnderlyingNode, newNode.UnderlyingNode);
        }

        public SyncLinkedListNode<T> AddBefore(LinkedListNode<SyncLinkedListNode<T>> node, SyncLinkedListNode<T> value)
        {
            lock (_syncLock) return _backing.AddBefore(node, value).Value;
        }

        public void AddBefore(SyncLinkedListNode<T> node, SyncLinkedListNode<T> newNode)
        {
            lock (_syncLock) _backing.AddBefore(node.UnderlyingNode, newNode.UnderlyingNode);
        }

        public SyncLinkedListNode<T> AddFirst(T value)
        {
            lock (_syncLock)
            {
                var node = new SyncLinkedListNode<T>(this, _syncLock, value);
                node.UnderlyingNode = _backing.AddFirst(node);
                return node;
            }
        }

        public SyncLinkedListNode<T> AddLast(T value)
        {
            lock (_syncLock)
            {
                var node = new SyncLinkedListNode<T>(this, _syncLock, value);
                node.UnderlyingNode = _backing.AddLast(node);
                return node;
            }
        }

        public void Clear()
        {
            if (Count == 0) return;

            lock (_syncLock)
            {
                if (Count == 0) return;
                _backing.Clear();
            }
        }

        public bool Contains(T value)
        {
            if (Count == 0) return false;

            lock (_syncLock)
            {
                return Count != 0 && Find(value) is not null;
            }
        }

        public bool Contains(SyncLinkedListNode<T> value)
        {
            if (Count == 0) return false;
            
            lock (_syncLock)
            {
                return Count != 0 && _backing.Contains(value);
            }
        }

        public SyncLinkedListNode<T>? Find(SyncLinkedListNode<T> value)
        {
            return Find(value.Value);
        }

        public SyncLinkedListNode<T>? Find(T value)
        {
            if (Count == 0) return null;
            
            lock (_syncLock)
            {
                var node = First;
                var cmp = EqualityComparer<T>.Default;

                if (node == null) return null;

                if (value != null)
                {
                    do
                    {
                        if (cmp.Equals(node!.UnderlyingNode.Value.Value, value))
                            return node;

                        node = node.UnderlyingNode.Value.Next;
                    } while (node != First);
                }
                else
                {
                    do
                    {
                        if (node!.UnderlyingNode.Value.Value == null)
                            return node;

                        node = node.UnderlyingNode.Value.Next;
                    } while (node != First);
                }

                return null;
            }
        }
    }

    internal sealed class SyncLinkedListNode<T>
    {
        private readonly object _syncLock;

        private T _value;

        internal LinkedListNode<SyncLinkedListNode<T>> UnderlyingNode { get; set; } = null!;

        public SyncLinkedList<T> List { get; }

        public T Value
        {
            get
            {
                lock (_syncLock) return _value;
            }
            set
            {
                lock (_syncLock) _value = value;
            }
        }

        public SyncLinkedListNode<T>? Next
        {
            get
            {
                lock (_syncLock) return UnderlyingNode.Next?.Value;
            }
        }

        public SyncLinkedListNode<T>? Prev
        {
            get
            {
                lock (_syncLock) return UnderlyingNode.Previous?.Value;
            }
        }

        internal SyncLinkedListNode(SyncLinkedList<T> list, object syncLock, T value)
        {
            List = list;
            _syncLock = syncLock;
            _value = value;
        }
    }
}