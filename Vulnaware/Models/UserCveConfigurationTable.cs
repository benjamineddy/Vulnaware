using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Vulnaware.Models
{
    public class UserCveConfigurationTable
    {
        public int ConfigurationID { get; set; }
        public int ProductID { get; set; }
        public int CveID { get; set; }
        public int StatusID { get; set; }
        public string Vendor { get; set; }
        public string Product { get; set; }
        public string Version { get; set; }
        public string Update { get; set; }
        public string Edition { get; set; }
        public string GivenCveID { get; set; }
        public DateTime DateAdded { get; set; }
        public double BaseScore { get; set; }
        public string StatusName { get; set; }
    }
}
