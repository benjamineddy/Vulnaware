using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Vulnaware.Models
{
    public class UserCveConfiguration
    {
        public string Notes { get; set; }

        [Display(Name = "Date Added")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyy}", ApplyFormatInEditMode = true)]
        public DateTime DateAdded { get; set; }

        public string New { get; set; }

        public int ConfigurationID { get; set; }
        public Configuration Configuration { get; set; }

        public int ProductID { get; set; }
        public Product Product { get; set; }

        public int CveID { get; set; }
        public Cve Cve { get; set; }

        public int StatusID { get; set; }
        public Status Status { get; set; }
    }
}
