using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Vulnaware.Models
{
    public class Configuration
    {
        public int ConfigurationID { get; set; }
        public string ConfigurationName { get; set; }

        [Display(Name = "Date Added")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyy}", ApplyFormatInEditMode = true)]
        public DateTime DateAdded { get; set; }

        public string Notes { get; set; }

        [ForeignKey("AspNetUser")]
        public string AspNetUserID { get; set; }
        public ApplicationUser ApplicationUser { get; set; }

        public ICollection<UserCveConfiguration> UserCveConfigurations { get; set; }
    }
}
