using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Vulnaware.Data;
using Vulnaware.Models;
using Vulnaware.Models.VulnawareViewModels;

namespace Vulnaware.Controllers
{
    [Authorize]
    public class ConfigurationController : Controller
    {
        private readonly DatabaseContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ConfigurationController(DatabaseContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            // Get the logged in user
            var user = new ApplicationUser();
            try
            {
                user = _userManager.GetUserAsync(User).Result;
            }
            catch (AggregateException e)
            {
                TempData["Param"] = "user";
                TempData["Error"] = e.Message;
                throw e;
            }

            // Use the logged in user to get the context user and associated data
            var userEntity = new ApplicationUser();
            try
            {
                userEntity = _context.Users.Where(u => u.Id == user.Id)
                    .Include(u => u.Configurations)
                        .ThenInclude(c => c.UserCveConfigurations)
                        .ThenInclude(pc => pc.Product)
                    .FirstOrDefault();
            } catch (ArgumentNullException e)
            {
                TempData["Param"] = "userEntity";
                TempData["Error"] = e.Message;
                throw e;
            }

            // Return the user
            return View(userEntity);
        }

        // Returns the configuration related to the given ID
        public IActionResult Details(int configurationID)
        {
            // Get the existingConfiguration
            var existingConfiguration = new Configuration();

            try
            {
                existingConfiguration = _context.Configurations
                    .Where(e => e.ConfigurationID == configurationID)
                    .Include(e => e.UserCveConfigurations)
                        .ThenInclude(e => e.Cve)
                    .FirstOrDefault();
            } catch (ArgumentNullException e)
            {
                TempData["Param"] = "existingconfiguration";
                TempData["Error"] = e.Message;
                throw e;
            }

            // If the configuration does not exist
            if (existingConfiguration == null)
            {
                TempData["Param"] = "Configuration";
                TempData["Error"] = "Configuration does not exist";
                return RedirectToAction("Error", "Home");
            }

            // Check access rights to this configuration
            if (existingConfiguration.AspNetUserID != _userManager.GetUserId(User))
            {
                TempData["Param"] = "Access Denied";
                TempData["Error"] = "You do not have access to this resource";
                return RedirectToAction("Error", "Home");
            }

            // Get the product and vulnerability counts
            ViewBag.ProductCount = existingConfiguration.UserCveConfigurations.GroupBy(ec => ec.ProductID).Count();
            ViewBag.VulnerabilityCount = existingConfiguration.UserCveConfigurations.Count();

            // Get the userCveConfigurationID
            ViewBag.ConfigurationID = configurationID;

            // Get the average base score
            double averageBaseScore = 0;
            foreach(var userCveConfigurationItem in existingConfiguration.UserCveConfigurations)
            {
                averageBaseScore += userCveConfigurationItem.Cve.BaseScore;
            }
            averageBaseScore = averageBaseScore / existingConfiguration.UserCveConfigurations.Count;
            string averageBaseScoreString = string.Format("{0:0.00}", averageBaseScore);
            ViewBag.AverageBaseScore = averageBaseScoreString;

            return View();
        }

        // Returns the datatable data
        [HttpPost]
        public IActionResult LoadUserCveConfigurationTable(int configurationID)
        {
            try
            {
                var draw = HttpContext.Request.Form["draw"].FirstOrDefault();

                // Skip number of rows count
                var start = Request.Form["start"].FirstOrDefault();

                // Paging length 10,20
                var length = Request.Form["length"].FirstOrDefault();

                // Sort column name
                var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"]
                    .FirstOrDefault() + "][name]"].FirstOrDefault();

                // Sort column direction (asc, desc)
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();

                // Search value from (search box)  
                var searchValue = Request.Form["search[value]"].FirstOrDefault();

                // Paging Size (10, 20, 50,100)  
                int pageSize = length != null ? Convert.ToInt32(length) : 0;

                int skip = start != null ? Convert.ToInt32(start) : 0;

                int recordsTotal = 0;

                // Get the configuration
                var userCveConfiguration = _context.Configurations
                    .Where(c => c.ConfigurationID == configurationID)
                    .Include(c => c.UserCveConfigurations)
                    .ThenInclude(ucc => ucc.Product)
                    .Include(c => c.UserCveConfigurations)
                    .ThenInclude(ucc => ucc.Cve)
                    .Include(c => c.UserCveConfigurations)
                    .ThenInclude(ucc => ucc.Status)
                    .FirstOrDefault();

                // Convert the configuration into the UserCveConfigurationTable JSON
                var UserCveConfigurationTableList = new List<UserCveConfigurationTable>();
                foreach(var userCveConfigurationItem in userCveConfiguration.UserCveConfigurations)
                {
                    UserCveConfigurationTable newUserCveConfigurationTable = new UserCveConfigurationTable
                    {
                        ConfigurationID = userCveConfiguration.ConfigurationID,
                        ProductID = userCveConfigurationItem.ProductID,
                        CveID = userCveConfigurationItem.CveID,
                        StatusID = userCveConfigurationItem.StatusID,
                        Vendor = userCveConfigurationItem.Product.Vendor,
                        Product = userCveConfigurationItem.Product.ProductName,
                        Version = userCveConfigurationItem.Product.Version,
                        Update = userCveConfigurationItem.Product.ProductUpdate,
                        Edition = userCveConfigurationItem.Product.Edition,
                        GivenCveID = userCveConfigurationItem.Cve.GivenID,
                        DateAdded = userCveConfigurationItem.DateAdded,
                        BaseScore = userCveConfigurationItem.Cve.BaseScore,
                    };

                    // Get the associated name of the status ID and add it to the newUserCveConfigurationTable
                    Status existingStatus = _context.Status
                        .Where(es => es.StatusID == newUserCveConfigurationTable.StatusID)
                        .FirstOrDefault();

                    newUserCveConfigurationTable.StatusName = existingStatus.StatusName;                       

                    UserCveConfigurationTableList.Add(newUserCveConfigurationTable);
                }

                // Create queryable version of the UserCveConfigurationTableList for sorting
                var UserCveConfigurationTableListQ = UserCveConfigurationTableList.AsQueryable();

                // Sorting
                if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                {
                    switch (sortColumn)
                    {
                        case "Vendor":
                            if (sortColumnDirection == "asc")
                            {
                                UserCveConfigurationTableListQ = UserCveConfigurationTableListQ
                                    .OrderBy(ucctl => ucctl.Vendor);
                                break;
                            }
                            else
                            {
                                UserCveConfigurationTableListQ = UserCveConfigurationTableListQ
                                    .OrderByDescending(ucctl => ucctl.Vendor);
                                break;
                            }
                        case "Product":
                            if (sortColumnDirection == "asc")
                            {
                                UserCveConfigurationTableListQ = UserCveConfigurationTableListQ
                                    .OrderBy(ucctl => ucctl.Product);
                                break;
                            }
                            else
                            {
                                UserCveConfigurationTableListQ = UserCveConfigurationTableListQ
                                    .OrderByDescending(ucctl => ucctl.Product);
                                break;
                            }
                        case "DateAdded":
                            if (sortColumnDirection == "asc")
                            {
                                UserCveConfigurationTableListQ = UserCveConfigurationTableListQ
                                    .OrderBy(ucctl => ucctl.DateAdded);
                                break;
                            }
                            else
                            {
                                UserCveConfigurationTableListQ = UserCveConfigurationTableListQ
                                    .OrderByDescending(ucctl => ucctl.DateAdded);
                                break;
                            }
                        case "BaseScore":
                            if (sortColumnDirection == "asc")
                            {
                                UserCveConfigurationTableListQ = UserCveConfigurationTableListQ
                                    .OrderBy(ucctl => ucctl.BaseScore);
                                break;
                            }
                            else
                            {
                                UserCveConfigurationTableListQ = UserCveConfigurationTableListQ
                                    .OrderByDescending(ucctl => ucctl.BaseScore);
                                break;
                            }
                    }
                }

                // Search
                if (!string.IsNullOrEmpty(searchValue))
                {
                    UserCveConfigurationTableListQ = UserCveConfigurationTableListQ
                        .Where(ucctl => ucctl.Vendor.Contains(searchValue)
                        || ucctl.Product.Contains(searchValue)
                        || ucctl.Version.Contains(searchValue)
                        || ucctl.Update.Contains(searchValue)
                        || ucctl.Edition.Contains(searchValue)
                        || ucctl.GivenCveID.Contains(searchValue));
                }

                // Set row count
                recordsTotal = UserCveConfigurationTableListQ.Count();

                // Set paging
                var data = UserCveConfigurationTableListQ.Skip(skip).Take(pageSize).ToList();

                // Return Json data
                return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

            }
            catch (Exception e)
            {
                TempData["Param"] = "LoadUserCveConfigurationTable";
                TempData["Error"] = e.Message;
                throw e;
            }
        }

        [HttpPost]
        public IActionResult GetStatusList()
        {
            var Statuslist = _context.Status.ToList();

            var json = JsonConvert.SerializeObject(Statuslist);

            return Content(json);
        }

        // Returns a UserCveConfiguration based on the given ID's
        [HttpPost]
        public IActionResult VulnerabilityDetails(int configurationID, int productID, int cveID)
        {
            // Get existing userCveConfiguration
            var userCveConfiguration = new UserCveConfiguration();

            try
            {
                userCveConfiguration = _context.UserCveConfigurations
                    .Where(ucc => ucc.ConfigurationID == configurationID
                        && ucc.ProductID == productID
                        && ucc.CveID == cveID)
                    .Include(ucc => ucc.Status)
                    .Include(ucc => ucc.Cve)
                        .ThenInclude(c => c.References)
                    .Include(ucc => ucc.Cve)
                        .ThenInclude(ucc => ucc.Cwes)
                    .Include(ucc => ucc.Product)
                    .FirstOrDefault();
            }
            catch (ArgumentNullException e)
            {
                TempData["Param"] = "userCveConfiguration";
                TempData["Error"] = e.Message;
                throw e;
            }

            // Add all statuses to the viewBag for the status dropdown
            var statusList = _context.Status.ToList();
            ViewBag.StatusObjectList = statusList;

            return View(userCveConfiguration);
        }

        // Update UserCveConfiguration
        [HttpPost, ActionName("UpdateUserVulnerability")]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateUserVulnerability(UserCveConfiguration model)
        {
            if (model == null)
            {
                ArgumentNullException e = new ArgumentNullException();
                TempData["Param"] = "UpdateUserVulnerability";
                TempData["Error"] = e.Message;
                throw e;
            }

            var userCveConfigurationToUpdate = new UserCveConfiguration();

            try
            {
                // Get the existing userCveConfiguration
                userCveConfigurationToUpdate = _context.UserCveConfigurations
                    .Where(ucc => ucc.ConfigurationID == model.ConfigurationID
                        && ucc.ProductID == model.ProductID
                        && ucc.CveID == model.CveID).FirstOrDefault();
            }
            catch (ArgumentNullException e)
            {
                TempData["Param"] = "userCveConfigurationToUpdate";
                TempData["Error"] = e.Message;
                throw e;
            }


            // If the Notes have changed, update them
            if (userCveConfigurationToUpdate.Notes != model.Notes)
            {
                userCveConfigurationToUpdate.Notes = model.Notes;
            }

            // If the StatusID has changed, update it.
            if (userCveConfigurationToUpdate.StatusID != model.StatusID)
            {
                userCveConfigurationToUpdate.StatusID = model.StatusID;
            }

            // Update userCveConfiguration in database
            try
            {
                _context.UserCveConfigurations.Update(userCveConfigurationToUpdate);
                _context.SaveChanges();
                return RedirectToAction("Details", new { model.ConfigurationID });
            }
            catch (DbUpdateException e)
            {
                TempData["Param"] = "userCveConfigurationToUpdate change save";
                TempData["Error"] = e.Message;
                throw e;
            }
        }

        // Returns the CreatewithFile view
        [HttpGet]
        public IActionResult CreateWithFile()
        {
            return View();
        }

        // Creates a new configuration and returns the data for the report view
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateWithFile(NewConfigurationFileViewModel model)
        {
            // Check file type
            string fileExtension = Path.GetExtension(model.ProductFile.FileName);
            if (fileExtension != ".csv")
            {
                ViewBag.CsvError = "Only .CSV filetype accepted, please try again.";
                return View(model);
            }

            // Determine template being used and get all the products only if the format is correct
            var productList = new List<string>();
            using(var reader = new StreamReader(model.ProductFile.OpenReadStream()))
            {
                var headerLine = reader.ReadLine();
                string nextProductLine;
                if (headerLine == "Concatenated CPE Names Below")
                {
                    while ((nextProductLine = reader.ReadLine()) != null)
                    {
                        productList.Add(nextProductLine);
                    }
                }
                else if (headerLine == "Type,Vendor,Product Name,Version,Update,Edition,Language")
                {
                    while ((nextProductLine = reader.ReadLine()) != null)
                    {
                        List<string> productPartList = nextProductLine.Split(',').ToList();
                        StringBuilder builder = new StringBuilder();
                        builder.Append("cpe:2.3:");
                        foreach (var productPart in productPartList)
                        {
                            if (String.IsNullOrEmpty(productPart) || productPart == "*")
                            {
                                builder.Append("*:");
                            }  else
                            {
                                builder.Append(productPart + ":");
                            }
                        }
                        builder.Append("*:*:*:*");
                        productList.Add(builder.ToString());
                    }
                } else
                {
                    ViewBag.CsvError = "Incorrect Template format, please download the correct template and try again.";
                    return View(model);
                }
            }

            // Create a new configuration based off user input
            Configuration newConfiguration = new Configuration
            {
                AspNetUserID = _userManager.GetUserId(User),
                ConfigurationName = model.ConfigurationName,
                Notes = model.Notes,
                DateAdded = DateTime.Now,
                UserCveConfigurations = new List<UserCveConfiguration>()
            };

            if (newConfiguration == null)
            {
                Exception e = new ArgumentNullException();
                TempData["Param"] = "userCveConfigurationToUpdate save changes";
                TempData["Error"] = e.Message;
                throw e;
            }

            List<string> successList = new List<string>();
            List<string> failedList = new List<string>();
            // Foreach product add it to a new ProductConfiguration and add that to the newConfiguration
            foreach (var productItem in productList)
            {
                Product existingProduct = _context.Products
                    .Where(p => p.Concatenated == productItem)
                    .Include(p => p.CveConfigurations)
                    .FirstOrDefault();

                // If the product exists
                if (existingProduct != null)
                {
                    // Add CveConfiguration to UserCveConfiguration for customisable notes etc.
                    Status unresolvedStatus = _context.Status
                        .Where(s => s.StatusName == "Unresolved")
                        .FirstOrDefault();
                    foreach (var CveConfigurationItem in existingProduct.CveConfigurations)
                    {
                        UserCveConfiguration newUserCveConfiguration = new UserCveConfiguration
                        {
                            ProductID = existingProduct.ProductID,
                            CveID = CveConfigurationItem.CveID,
                            StatusID = unresolvedStatus.StatusID,
                            Notes = "No user defined notes.",
                            DateAdded = DateTime.Now,
                            New = 'N'.ToString()
                        };

                        newConfiguration.UserCveConfigurations.Add(newUserCveConfiguration);
                    }
                    successList.Add(productItem);
                }
                else
                {
                    failedList.Add(productItem);
                }
            }

            // Save the new configuration if there any successful products
            if (successList.Any())
            {
                try
                {
                    _context.Configurations.Add(newConfiguration);
                    _context.SaveChanges();
                }
                catch (Exception e)
                {
                    TempData["Param"] = "newConfiguration save changes";
                    TempData["Error"] = e.Message;
                    throw e;
                }
            }

            // Determine success and add result to ViewBag
            if (productList == null)
            {
                ViewBag.CsvError = "The CSV file does not contain any products or is invalid, please check and upload again.";
                return View(model);
            }

            if (!failedList.Any())
            {
                ViewBag.UploadStatus = "success";
            }
            else if (failedList.Any() && successList.Any())
            {
                ViewBag.UploadStatus = "partial";
            }
            else if(!successList.Any())
            {
                ViewBag.UploadStatus = "failed";
            }

            ViewBag.SuccessList = successList;
            ViewBag.FailedList = failedList;

            return View(model);
        }

        // Returns the CreateWithFileWindows view
        [HttpGet]
        public IActionResult CreateWithFileWindows()
        {
            return View();
        }

        // Creates a new windows configuration and returns the results
        [HttpPost]
        public IActionResult CreateWithFileWindows(NewConfigurationFileViewModel model)
        {
            // Check file type
            string fileExtension = Path.GetExtension(model.ProductFile.FileName);
            if (fileExtension != ".csv")
            {
                ViewBag.CsvError = "Only .CSV filetype accepted, please try again.";
                return View(model);
            }

            List<WindowsProduct> productList = new List<WindowsProduct>();
            using (var reader = new StreamReader(model.ProductFile.OpenReadStream()))
            {

                // Check the correct data is present
                var headerLine = reader.ReadLine();
                reader.ReadLine();
                string nextProductLine;
                if (headerLine == "#TYPE Selected.System.Management.Automation.PSCustomObject")
                {
                    // While there is another line
                    while ((nextProductLine = reader.ReadLine()) != null)
                    {
                        // Cleanse and split the line
                        nextProductLine = nextProductLine.Replace("\\","");
                        nextProductLine = nextProductLine.Replace("\"", "");
                        nextProductLine = nextProductLine.Replace("(", "");
                        nextProductLine = nextProductLine.Replace(")", "");
                        nextProductLine = nextProductLine.Replace("-bit", "");
                        nextProductLine = nextProductLine.ToLower();
                        var values = nextProductLine.Split(',');

                        // Create newWindowsProduct object only if the DisplayName is not empty
                        if (values[0] != "")
                        {
                            var newWindowsProduct = new WindowsProduct()
                            {
                                DisplayName = values[0],
                                DisplayVersion = values[1]
                            };

                            productList.Add(newWindowsProduct);
                        }
                    }
                }
                else
                {
                    //TODO return error because it's not the right content
                }
            }

            // Attempt matching
            var cveList = _context.Products.ToList();
            // foreach import product
            foreach (var currImportProduct in productList)
            {
                var matches = new List<MatchedProduct>();
                // Split into words
                var displayNameSplit = currImportProduct.DisplayName.Split(" ");
                var matchedList = new List<MatchedProduct>();

                foreach (var currDatabaseProduct in cveList)
                {
                    var wordMatchCount = 0;
                    foreach (var currWord in displayNameSplit)
                    {
                        if (currDatabaseProduct.Concatenated.Contains(currWord))
                        {
                            wordMatchCount++;
                        }
                    }

                    var newMatchedProduct = new MatchedProduct()
                    {
                        ImportProduct = currImportProduct.DisplayName,
                        DatabaseProduct = currDatabaseProduct.Concatenated,
                        Score = wordMatchCount
                    };

                    matchedList.Add(newMatchedProduct);

                    matchedList = matchedList.OrderBy(ml => ml.Score).ToList();
                }
                System.Diagnostics.Debug.WriteLine("STOP");
            }

            return null;
        }

        // Deletes the configuration related to the given ID and redirects to the configuration index
        public IActionResult DeleteConfiguration(int? id)
        {
            if (id == null)
            {
                Exception e = new ArgumentNullException();
                TempData["Param"] = "id";
                TempData["Error"] = e.Message;
                throw e;
            }

            Configuration configurationToRemove = _context.Configurations
                .Where(c => c.ConfigurationID == id)
                .Include(c => c.UserCveConfigurations)
                .FirstOrDefault();

            if (configurationToRemove == null)
            {
                Exception e = new ArgumentNullException();
                TempData["Param"] = "configurationToRemove";
                TempData["Error"] = e.Message;
                throw e;
            }

            // If this configuration doesn't belong to the user
            if (configurationToRemove.AspNetUserID != _userManager.GetUserId(User))
            {
                return Unauthorized();
            }

            // Mark the configuration for deletion 
            _context.Entry(configurationToRemove).State = EntityState.Deleted;

            // Delete
            try
            {
                _context.SaveChanges();
            }
            catch (Exception e)
            {
                TempData["Param"] = "configurationToRemove save changes";
                TempData["Error"] = e.Message;
                throw e;
            }

            return RedirectToAction(nameof(Index));
        }

        private class WindowsProduct
        {
            public string DisplayName { get; set; }
            public string DisplayVersion { get; set; }
        }

        private class MatchedProduct
        {
            public string ImportProduct { get; set; }
            public string DatabaseProduct { get; set; }
            public int Score { get; set; }
        }

        private bool ConfigurationExists(int id)
        {
            return _context.Configurations.Any(e => e.ConfigurationID == id);
        }
    }
}
