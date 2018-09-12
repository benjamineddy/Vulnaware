using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Vulnaware.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public ICollection<Configuration> Configurations { get; set; } = new List<Configuration>();
        public ICollection<UserPreference> UserPreferences { get; set; }
        public ICollection<TrackedCve> TrackedCves { get; set; }
    }
}
