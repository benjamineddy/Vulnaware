using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Vulnaware.Models
{
    public class Product
    {
        //id, concatenated, part, vendor, product, version, update, edition, language, added
        public int ProductID { get; set; }

        public string Concatenated { get; set; }
        public string Part { get; set; }
        public string Vendor { get; set; }

        [Display(Name = "Name")]
        public string ProductName { get; set; }

        public string Version { get; set; }

        [Display(Name = "Update")]
        public string ProductUpdate { get; set; }

        public string Edition { get; set; }

        [Display(Name = "Language")]
        public string ProductLanguage { get; set; }

        [Display(Name = "Date Added")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyy}", ApplyFormatInEditMode = true)]
        public DateTime Added { get; set; }

        public ICollection<CveConfiguration> CveConfigurations { get; set; }
        public ICollection<UserCveConfiguration> UserCveConfigurations { get; set; }
    }
}
