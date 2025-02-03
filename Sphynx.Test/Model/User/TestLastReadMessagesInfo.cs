// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections;
using Sphynx.Core;
using Sphynx.ModelV2.User;
using Sphynx.Test.Utils;

namespace Sphynx.Test.Model.User
{
    public class TestLastReadMessagesInfo : ILastReadMessageInfo
    {
        private readonly IDictionary<SnowflakeId, SnowflakeId> _lastReadMessages;

        public TestLastReadMessagesInfo() : this(new Dictionary<SnowflakeId, SnowflakeId>())
        {
        }

        public TestLastReadMessagesInfo(params ValueTuple<SnowflakeId, SnowflakeId>[] values) : this()
        {
            foreach (var value in values)
                this[value.Item1] = value.Item2;
        }

        public TestLastReadMessagesInfo(params string[] msgs) : this()
        {
            foreach (string msg in msgs)
                this[$"room+{msg}".AsSnowflakeId()] = msg.AsSnowflakeId();
        }

        public TestLastReadMessagesInfo(IDictionary<SnowflakeId, SnowflakeId> lastReadMessages)
        {
            _lastReadMessages = lastReadMessages;
        }

        public IEnumerator<KeyValuePair<SnowflakeId, SnowflakeId>> GetEnumerator() =>
            _lastReadMessages.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _lastReadMessages.GetEnumerator();

        public void Add(KeyValuePair<SnowflakeId, SnowflakeId> item) => _lastReadMessages.Add(item);

        public void Clear() => _lastReadMessages.Clear();

        public bool Contains(KeyValuePair<SnowflakeId, SnowflakeId> item) => _lastReadMessages.Contains(item);

        public void CopyTo(KeyValuePair<SnowflakeId, SnowflakeId>[] array, int arrayIndex) =>
            _lastReadMessages.CopyTo(array, arrayIndex);

        public bool Remove(KeyValuePair<SnowflakeId, SnowflakeId> item) => _lastReadMessages.Remove(item);

        public int Count => _lastReadMessages.Count;

        public bool IsReadOnly => _lastReadMessages.IsReadOnly;

        public void Add(SnowflakeId key, SnowflakeId value) => _lastReadMessages.Add(key, value);

        public bool ContainsKey(SnowflakeId key) => _lastReadMessages.ContainsKey(key);

        public bool Remove(SnowflakeId key) => _lastReadMessages.Remove(key);

        public bool TryGetValue(SnowflakeId key, out SnowflakeId value) =>
            _lastReadMessages.TryGetValue(key, out value);

        public SnowflakeId this[SnowflakeId key]
        {
            get => _lastReadMessages[key];
            set => _lastReadMessages[key] = value;
        }

        public ICollection<SnowflakeId> Keys => _lastReadMessages.Keys;

        public ICollection<SnowflakeId> Values => _lastReadMessages.Values;
    }
}
