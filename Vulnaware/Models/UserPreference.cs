using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Vulnaware.Models
{
    public class UserPreference
    {
        public int UserPreferenceId { get; set; }
        public string EmailNotification { get; set; }

        public ApplicationUser ApplicationUser { get; set; }
    }
}
