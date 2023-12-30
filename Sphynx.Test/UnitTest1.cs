using MongoDB.Driver;
using Sphynx.Server.Database;

namespace Sphynx.Test
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            Assert.DoesNotThrow(() => new MongoClient("mongodb+srv://admin:UvG1TxcHOZJ0VjBB@sphynxcluster.vpdimph.mongodb.net/"));
        }
    }
}