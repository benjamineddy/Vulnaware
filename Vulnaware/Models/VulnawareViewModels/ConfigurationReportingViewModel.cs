using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Vulnaware.Models.VulnawareViewModels
{
    public class ConfigurationReportingViewModel
    {
        [Display(Name = "Configuration(s)")]
        public List<SelectListItem> ConfigurationList { get; set; }

        public List<Configuration> ConfigurationObjectList { get; set; }
        public int SelectedItemID { get; set; }

        [Display(Name = "Minimum Score")]
        public double MinScore { get; set; }

        [Display(Name ="Maximum Score")]
        public double MaxScore { get; set; }
    }
}
