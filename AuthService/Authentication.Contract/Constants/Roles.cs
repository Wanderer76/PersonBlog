using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Authentication.Contract.Constants
{
    public static class Roles
    {
        public static readonly Guid AdminRole = Guid.Parse("57a2b99b-b6ee-4c98-a1f0-b18fe96dae60");
        public static readonly Guid SuperAdminRole = Guid.Parse("accbc12f-6ff1-4343-a26f-13b99e64abb6");
        public static readonly Guid User = Guid.Parse("d95ca3d6-0f63-4b48-a54f-1202f3d6bf2c");
        public static readonly Guid Blogger = Guid.Parse("68816c1b-fdfb-45d0-bfea-dceb04277a57");
        public static readonly Guid Artist = Guid.Parse("72c4bbed-5375-47fc-864f-1490ca82aced");
    }
}
