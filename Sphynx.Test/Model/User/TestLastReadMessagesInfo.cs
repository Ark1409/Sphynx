// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ModelV2.User;
using Sphynx.Test.Utils;

namespace Sphynx.Test.Model.User
{
    public class TestLastReadMessagesInfo : LastReadMessageInfo
    {
        public TestLastReadMessagesInfo()
        {
        }

        public TestLastReadMessagesInfo(params KeyValuePair<SnowflakeId, SnowflakeId>[] values) : base(values)
        {
            foreach (var value in values)
                this[value.Key] = value.Value;
        }

        public TestLastReadMessagesInfo(params string[] messages) : this()
        {
            foreach (string msg in messages)
                this[$"room-{msg}".AsSnowflakeId()] = msg.AsSnowflakeId();
        }
    }
}
