using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphynx.Core
{
    public class SphynxSocketMessage
    {
        public SphynxSocketMessageHeader? Header { get; set; }

        public string? Content { get; set; }
    }
    public abstract class SphynxSocketMessageHeader
    {
        public Version Version { get; set; }

        public DateTime Timestamp { get; set; }

        public int ContentLength { get; set; }

        public abstract byte[] Serialize();
    }
}
