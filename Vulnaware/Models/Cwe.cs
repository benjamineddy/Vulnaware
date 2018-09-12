using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Vulnaware.Models
{
    public class Cwe
    {
        public int CweID { get; set; }
        public string GivenId { get; set; }

        public Cve Cve { get; set; }
    }
}
