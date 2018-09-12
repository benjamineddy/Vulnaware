using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Vulnaware.Models
{
    public class CveConfiguration
    {
        [ForeignKey("Cve")]
        public int CveID { get; set; }
        public Cve Cve { get; set; }

        [ForeignKey("Product")]
        public int ProductID { get; set; }
        public Product Product { get; set; }
    }
}
