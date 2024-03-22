using Sphynx.Storage;
using Sphynx.Utils;

namespace Sphynx.Server.Storage
{
    public sealed class FileStore<TLookup, TValue> : ISphynxStore<TLookup, TValue> where TLookup : notnull
    {
        public TValue Get(TLookup key)
        {
            throw new NotImplementedException();
        }

        public bool Put(TLookup key, TValue data)
        {
            throw new NotImplementedException();
        }

        public bool Delete(TLookup key)
        {
            throw new NotImplementedException();
        }
    }
}