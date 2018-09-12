using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Vulnaware.Models
{
    public class TrackedCve
    {
        public string Notes { get; set; }

        [ForeignKey("AspNetUser")]
        public string AspNetUserID { get; set; }
        public ApplicationUser ApplicationUser { get; set; }

        [ForeignKey("Cve")]
        public int CveID { get; set; }
        public Cve Cve { get; set; }
    }
}
