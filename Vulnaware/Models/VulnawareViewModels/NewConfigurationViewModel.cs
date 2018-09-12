using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Vulnaware.Models.VulnawareViewModels
{
    public class NewConfigurationViewModel
    {
        [Required]
        [Display(Name = "Configuration Name")]
        public string ConfigurationName { get; set; }

        [Required]
        [Display(Name = "Notes")]
        public string Notes { get; set; }
    }
}
