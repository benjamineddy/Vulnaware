using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Vulnaware.Models.VulnawareViewModels
{
    public class NewConfigurationFileViewModel
    {
        [Required]
        [Display(Name = "Configuration Name")]
        public string ConfigurationName { get; set; }

        [Required]
        [Display(Name = "Notes")]
        public string Notes { get; set; }

        [Required]
        [Display(Name = "Products File")]
        public IFormFile ProductFile { get; set; }
    }
}
