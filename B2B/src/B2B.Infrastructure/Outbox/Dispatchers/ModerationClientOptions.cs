using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Infrastructure.Outbox.Dispatchers
{
    public sealed class ModerationClientOptions
    {
        public string BaseUrl { get; set; } = null!;
        public string ServiceKey { get; set; } = null!;
    }

    public sealed class B2cClientOptions
    {
        public string BaseUrl { get; set; } = null!;
        public string ServiceKey { get; set; } = null!;
    }
}
