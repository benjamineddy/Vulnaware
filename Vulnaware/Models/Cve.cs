using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Vulnaware.Models
{
    public class Cve
    {
        public int CveID { get; set; }

        [Display(Name = "Given ID")]
        public string GivenID { get; set; }

        public string Description { get; set; }

        [Display(Name = "Vector String")]
        public string VectorString { get; set; }

        [Display(Name = "Access Vector")]
        public string AccessVector { get; set; }

        [Display(Name = "Access Complexity")]
        public string AccessComplexity { get; set; }

        public string Authentication { get; set; }

        [Display(Name = "Confidentiality Impact")]
        public string ConfidentialityImpact { get; set; }

        [Display(Name = "Integrity Impact")]
        public string IntegrityImpact { get; set; }

        [Display(Name = "Availability Impact")]
        public string AvailabilityImpact { get; set; }

        [Display(Name = "Base Score")]
        public double BaseScore { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyy}", ApplyFormatInEditMode = true)]
        [Display(Name = "Date Published")]
        public DateTime PublishedDate { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyy}", ApplyFormatInEditMode = true)]
        [Display(Name = "Date Last Modified")]
        public DateTime LastModifiedDate { get; set; }

        public ICollection<Reference> References { get; set; }
        public ICollection<Cwe> Cwes { get; set; }
        public ICollection<CveConfiguration> CveConfigurations { get; set; }
        public ICollection<TrackedCve> TrackedCves { get; set; }
        public ICollection<UserCveConfiguration> UserCveConfigurations { get; set; }
    }
}
