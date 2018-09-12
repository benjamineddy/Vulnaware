using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vulnaware.Data;
using Vulnaware.Models;
using Vulnaware.Models.VulnawareViewModels;

namespace Vulnaware.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class ConfigurationReportingController : Controller
    {
        private readonly DatabaseContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ConfigurationReportingController(DatabaseContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Index()
        {
            // Setup the search model
            ConfigurationReportingViewModel model = new ConfigurationReportingViewModel();
            model.ConfigurationList = new List<SelectListItem>();
            SelectListItem anySelectListItem = new SelectListItem()
            {
                Value = "0",
                Text = "Any"
            };
            model.ConfigurationList.Add(anySelectListItem);
            foreach (var item in _context.Configurations
                .Where(c => c.AspNetUserID == _userManager.GetUserId(User))
                .ToList())
            {
                SelectListItem newSelectListItem = new SelectListItem()
                {
                    Value = item.ConfigurationID.ToString(),
                    Text = item.ConfigurationName
                };
                model.ConfigurationList.Add(newSelectListItem);
            }
            return View(model);
        }

        [HttpPost]
        public IActionResult Index(ConfigurationReportingViewModel model)
        {
            // Return search model as previous state
            model.ConfigurationList = new List<SelectListItem>();
            if (model.SelectedItemID == 0)
            {
                SelectListItem anySelectListItem = new SelectListItem()
                {
                    Value = "0",
                    Text = "Any",
                    Selected = true
                };
                model.ConfigurationList.Add(anySelectListItem);
            }
            else
            {
                SelectListItem anySelectListItem = new SelectListItem()
                {
                    Value = "0",
                    Text = "Any",
                    Selected = true
                };
                model.ConfigurationList.Add(anySelectListItem);
            }
            foreach (var item in _context.Configurations
                .Where(c => c.AspNetUserID == _userManager.GetUserId(User))
                .ToList())
            {
                if (item.ConfigurationID == model.SelectedItemID)
                {
                    SelectListItem selectListItem = new SelectListItem()
                    {
                        Value = item.ConfigurationID.ToString(),
                        Text = item.ConfigurationName,
                        Selected = true
                    };
                    model.ConfigurationList.Add(selectListItem);
                }
                else
                {
                    SelectListItem selectListItem = new SelectListItem()
                    {
                        Value = item.ConfigurationID.ToString(),
                        Text = item.ConfigurationName
                    };
                    model.ConfigurationList.Add(selectListItem);
                }
            }

            // Get configurations filtered based on view model
            model.ConfigurationObjectList = new List<Configuration>();

            // If only one configuration has been requested
            if (model.SelectedItemID != 0)
            {
                // Get the configuration with the matching ID
                Configuration existingConfiguration = _context.Configurations
                    .Where(c => c.ConfigurationID == model.SelectedItemID)
                    .Include(c => c.UserCveConfigurations)
                    .ThenInclude(ucc => ucc.Product)
                    .Include(c => c.UserCveConfigurations)
                    .ThenInclude(ucc => ucc.Cve)
                    .Include(c => c.UserCveConfigurations)
                    .ThenInclude(ucc => ucc.Status)
                    .FirstOrDefault();
                model.ConfigurationObjectList.Add(existingConfiguration);
            }
            else
            {
                // Get all configurations matching that user
                model.ConfigurationObjectList = _context.Configurations
                    .Where(c => c.AspNetUserID == _userManager.GetUserId(User))
                    .Include(c => c.UserCveConfigurations)
                    .ThenInclude(ucc => ucc.Product)
                    .Include(c => c.UserCveConfigurations)
                    .ThenInclude(ucc => ucc.Cve)
                    .Include(c => c.UserCveConfigurations)
                    .ThenInclude(ucc => ucc.Status)
                    .ToList();
            }

            return View(model);
        }

        [HttpPost]
        public FileContentResult ExportCsv(ConfigurationReportingViewModel model)
        {
            model.ConfigurationObjectList = new List<Configuration>();

            // If only one configuration has been requested
            if (model.SelectedItemID != 0)
            {
                // Get the configuration with the matching ID
                Configuration existingConfiguration = _context.Configurations
                    .Where(c => c.ConfigurationID == model.SelectedItemID)
                    .Include(c => c.UserCveConfigurations)
                    .ThenInclude(ucc => ucc.Product)
                    .Include(c => c.UserCveConfigurations)
                    .ThenInclude(ucc => ucc.Cve)
                    .Include(c => c.UserCveConfigurations)
                    .ThenInclude(ucc => ucc.Status)
                    .FirstOrDefault();
                model.ConfigurationObjectList.Add(existingConfiguration);
            }
            else
            {
                // Get all configurations matching that user
                model.ConfigurationObjectList = _context.Configurations
                    .Where(c => c.AspNetUserID == _userManager.GetUserId(User))
                    .Include(c => c.UserCveConfigurations)
                    .ThenInclude(ucc => ucc.Product)
                    .Include(c => c.UserCveConfigurations)
                    .ThenInclude(ucc => ucc.Cve)
                    .Include(c => c.UserCveConfigurations)
                    .ThenInclude(ucc => ucc.Status)
                    .ToList();
            }

            // Build StringBuilder rows
            StringBuilder sb = new StringBuilder();
            sb.Append("Configuration Name" + "," + "Vendor" + "," + "Product" + "," + "Version" + "," + "Update"
                 + "," + "Edition" + "," + "CVE ID" + "," + "Date Added" + "," + "Base Score" + "," + "Status" + Environment.NewLine);
            foreach (var configurationItem in model.ConfigurationObjectList)
            {
                foreach(var groupedUserCveconfigurationItem in configurationItem.UserCveConfigurations.GroupBy(ucc => ucc.ProductID)
                                           .Select(g => g.First()).ToList())
                {
                    foreach(var userCveConfigurationItem in groupedUserCveconfigurationItem.Product.UserCveConfigurations)
                    {
                        if(model.MinScore != 0 && model.MaxScore != 0)
                        {
                            if (userCveConfigurationItem.Cve.BaseScore >= model.MinScore
                                    && userCveConfigurationItem.Cve.BaseScore <= model.MaxScore)
                            {
                                // Configuration name
                                if (configurationItem.ConfigurationName.IndexOfAny(new char[] { '"', ',' }) != -1)
                                {
                                    sb.AppendFormat("\"{0}\"", configurationItem.ConfigurationName.Replace("\"", "\"\""));
                                }
                                else
                                {
                                    sb.Append(configurationItem.ConfigurationName);
                                }
                                sb.Append(",");

                                // Product vendor
                                if (userCveConfigurationItem.Product.Vendor.IndexOfAny(new char[] { '"', ',' }) != -1)
                                {
                                    sb.AppendFormat("\"{0}\"", userCveConfigurationItem.Product.Vendor.Replace("\"", "\"\""));
                                }
                                else
                                {
                                    sb.Append(userCveConfigurationItem.Product.Vendor);
                                }
                                sb.Append(",");

                                // Product name
                                if (userCveConfigurationItem.Product.ProductName.IndexOfAny(new char[] { '"', ',' }) != -1)
                                {
                                    sb.AppendFormat("\"{0}\"", userCveConfigurationItem.Product.ProductName.Replace("\"", "\"\""));
                                }
                                else
                                {
                                    sb.Append(userCveConfigurationItem.Product.ProductName);
                                }
                                sb.Append(",");

                                // Product version
                                if (userCveConfigurationItem.Product.Version.IndexOfAny(new char[] { '"', ',' }) != -1)
                                {
                                    sb.AppendFormat("\"{0}\"", userCveConfigurationItem.Product.Version.Replace("\"", "\"\""));
                                }
                                else
                                {
                                    sb.Append(userCveConfigurationItem.Product.Version);
                                }
                                sb.Append(",");

                                // Product update
                                if (userCveConfigurationItem.Product.ProductUpdate.IndexOfAny(new char[] { '"', ',' }) != -1)
                                {
                                    sb.AppendFormat("\"{0}\"", userCveConfigurationItem.Product.Edition.Replace("\"", "\"\""));
                                }
                                else
                                {
                                    sb.Append(userCveConfigurationItem.Product.ProductUpdate);
                                }
                                sb.Append(",");

                                // Product edition
                                if (userCveConfigurationItem.Product.Edition.IndexOfAny(new char[] { '"', ',' }) != -1)
                                {
                                    sb.AppendFormat("\"{0}\"", userCveConfigurationItem.Product.Edition.Replace("\"", "\"\""));
                                }
                                else
                                {
                                    sb.Append(userCveConfigurationItem.Product.Edition);
                                }
                                sb.Append(",");

                                // CVE ID
                                if (userCveConfigurationItem.Cve.GivenID.IndexOfAny(new char[] { '"', ',' }) != -1)
                                {
                                    sb.AppendFormat("\"{0}\"", userCveConfigurationItem.Cve.GivenID.Replace("\"", "\"\""));
                                }
                                else
                                {
                                    sb.Append(userCveConfigurationItem.Cve.GivenID);
                                }
                                sb.Append(",");

                                // Date added
                                if (userCveConfigurationItem.DateAdded.ToShortDateString().IndexOfAny(new char[] { '"', ',' }) != -1)
                                {
                                    sb.AppendFormat("\"{0}\"", userCveConfigurationItem.DateAdded.ToShortDateString().Replace("\"", "\"\""));
                                }
                                else
                                {
                                    sb.Append(userCveConfigurationItem.DateAdded.ToShortDateString());
                                }
                                sb.Append(",");

                                // Base score
                                if (userCveConfigurationItem.Cve.BaseScore.ToString().IndexOfAny(new char[] { '"', ',' }) != -1)
                                {
                                    sb.AppendFormat("\"{0}\"", userCveConfigurationItem.Cve.ToString().Replace("\"", "\"\""));
                                }
                                else
                                {
                                    sb.Append(userCveConfigurationItem.Cve.BaseScore);
                                }
                                sb.Append(",");

                                // Status
                                if (userCveConfigurationItem.Status.StatusName.IndexOfAny(new char[] { '"', ',' }) != -1)
                                {
                                    sb.AppendFormat("\"{0}\"", userCveConfigurationItem.Status.StatusName.Replace("\"", "\"\""));
                                }
                                else
                                {
                                    sb.Append(userCveConfigurationItem.Status.StatusName);
                                }
                                sb.Append(",");

                                // New line
                                sb.Append(Environment.NewLine);
                            }
                        }
                        else
                        {
                            // Configuration name
                            if (configurationItem.ConfigurationName.IndexOfAny(new char[] { '"', ',' }) != -1)
                            {
                                sb.AppendFormat("\"{0}\"", configurationItem.ConfigurationName.Replace("\"", "\"\""));
                            }
                            else
                            {
                                sb.Append(configurationItem.ConfigurationName);
                            }
                            sb.Append(",");

                            // Product vendor
                            if (userCveConfigurationItem.Product.Vendor.IndexOfAny(new char[] { '"', ',' }) != -1)
                            {
                                sb.AppendFormat("\"{0}\"", userCveConfigurationItem.Product.Vendor.Replace("\"", "\"\""));
                            }
                            else
                            {
                                sb.Append(userCveConfigurationItem.Product.Vendor);
                            }
                            sb.Append(",");

                            // Product name
                            if (userCveConfigurationItem.Product.ProductName.IndexOfAny(new char[] { '"', ',' }) != -1)
                            {
                                sb.AppendFormat("\"{0}\"", userCveConfigurationItem.Product.ProductName.Replace("\"", "\"\""));
                            }
                            else
                            {
                                sb.Append(userCveConfigurationItem.Product.ProductName);
                            }
                            sb.Append(",");

                            // Product version
                            if (userCveConfigurationItem.Product.Version.IndexOfAny(new char[] { '"', ',' }) != -1)
                            {
                                sb.AppendFormat("\"{0}\"", userCveConfigurationItem.Product.Version.Replace("\"", "\"\""));
                            }
                            else
                            {
                                sb.Append(userCveConfigurationItem.Product.Version);
                            }
                            sb.Append(",");

                            // Product update
                            if (userCveConfigurationItem.Product.ProductUpdate.IndexOfAny(new char[] { '"', ',' }) != -1)
                            {
                                sb.AppendFormat("\"{0}\"", userCveConfigurationItem.Product.Edition.Replace("\"", "\"\""));
                            }
                            else
                            {
                                sb.Append(userCveConfigurationItem.Product.ProductUpdate);
                            }
                            sb.Append(",");

                            // Product edition
                            if (userCveConfigurationItem.Product.Edition.IndexOfAny(new char[] { '"', ',' }) != -1)
                            {
                                sb.AppendFormat("\"{0}\"", userCveConfigurationItem.Product.Edition.Replace("\"", "\"\""));
                            }
                            else
                            {
                                sb.Append(userCveConfigurationItem.Product.Edition);
                            }
                            sb.Append(",");

                            // CVE ID
                            if (userCveConfigurationItem.Cve.GivenID.IndexOfAny(new char[] { '"', ',' }) != -1)
                            {
                                sb.AppendFormat("\"{0}\"", userCveConfigurationItem.Cve.GivenID.Replace("\"", "\"\""));
                            }
                            else
                            {
                                sb.Append(userCveConfigurationItem.Cve.GivenID);
                            }
                            sb.Append(",");

                            // Date added
                            if (userCveConfigurationItem.DateAdded.ToShortDateString().IndexOfAny(new char[] { '"', ',' }) != -1)
                            {
                                sb.AppendFormat("\"{0}\"", userCveConfigurationItem.DateAdded.ToShortDateString().Replace("\"", "\"\""));
                            }
                            else
                            {
                                sb.Append(userCveConfigurationItem.DateAdded.ToShortDateString());
                            }
                            sb.Append(",");

                            // Base score
                            if (userCveConfigurationItem.Cve.BaseScore.ToString().IndexOfAny(new char[] { '"', ',' }) != -1)
                            {
                                sb.AppendFormat("\"{0}\"", userCveConfigurationItem.Cve.ToString().Replace("\"", "\"\""));
                            }
                            else
                            {
                                sb.Append(userCveConfigurationItem.Cve.BaseScore);
                            }
                            sb.Append(",");

                            // Status
                            if (userCveConfigurationItem.Status.StatusName.IndexOfAny(new char[] { '"', ',' }) != -1)
                            {
                                sb.AppendFormat("\"{0}\"", userCveConfigurationItem.Status.StatusName.Replace("\"", "\"\""));
                            }
                            else
                            {
                                sb.Append(userCveConfigurationItem.Status.StatusName);
                            }
                            sb.Append(",");

                            // New line
                            sb.Append(Environment.NewLine);
                        }
                    }
                }
            }

            byte[] byteArray = Encoding.ASCII.GetBytes(sb.ToString());
            var result = new FileContentResult(byteArray, "application/octet-stream");
            result.FileDownloadName = "my-csv-file.csv";
            return result;
        }
    }
}