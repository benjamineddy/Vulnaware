using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Vulnaware.Models
{
    public class Reference
    {
        public int ReferenceID { get; set; }
        public string Url { get; set; }

        public Cve Cve { get; set; }
    }
}
