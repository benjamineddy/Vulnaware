using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vulnaware.Models;

namespace Vulnaware.Data
{
    public static class DbInitializer
    {
        public static void Initialize(DatabaseContext context)
        {
            context.Database.EnsureCreated();
        }
    }
}
