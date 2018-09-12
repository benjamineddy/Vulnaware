using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Vulnaware.Data;
using Vulnaware.Models;

namespace Vulnaware.Controllers
{
    [AllowAnonymous]
    public class ProductController : Controller
    {
        private readonly DatabaseContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProductController(DatabaseContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            // If a user is logged in
            if (User.Identity.IsAuthenticated)
            {
                // Get their configurations
                var configurationList = _context.Configurations
                    .Where(c => c.AspNetUserID == _userManager.GetUserId(User)).ToList();

                ViewBag.ConfigurationObjectList = configurationList;
            }
            return View();
        }

        [HttpPost]
        public IActionResult LoadIndexTableData()
        {
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

                    // Get all product data
                    var productList = _context.Products.AsQueryable<Product>();

                    // Sorting
                    if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                    {
                        switch (sortColumn)
                        {
                            case "Vendor":
                                if (sortColumnDirection == "asc")
                                {
                                    productList = productList.OrderBy(pl => pl.Vendor);
                                    break;
                                }
                                else
                                {
                                    productList = productList.OrderByDescending(pl => pl.Vendor);
                                    break;
                                }
                            case "ProductName":
                                if (sortColumnDirection == "asc")
                                {
                                    productList = productList.OrderBy(pl => pl.ProductName);
                                    break;
                                }
                                else
                                {
                                    productList = productList.OrderByDescending(pl => pl.ProductName);
                                    break;
                                }
                            case "Added":
                                if (sortColumnDirection == "asc")
                                {
                                    productList = productList.OrderBy(pl => pl.Added);
                                    break;
                                }
                                else
                                {
                                    productList = productList.OrderByDescending(pl => pl.Added);
                                    break;
                                }
                        }
                    }

                    // Search
                    if (!string.IsNullOrEmpty(searchValue))
                    {
                        productList = productList.Where(vl => vl.Vendor.Contains(searchValue)
                            || vl.ProductName.Contains(searchValue)
                            || vl.Version.Contains(searchValue)
                            || vl.ProductUpdate.Contains(searchValue)
                            || vl.Edition.Contains(searchValue)
                            || vl.ProductLanguage.Contains(searchValue));
                    }

                    // Set row count
                    recordsTotal = productList.Count();

                    // Set paging
                    var data = productList.Skip(skip).Take(pageSize).ToList();

                    // Return Json data
                    return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public IActionResult Details(int id)
        {
            Product product = _context.Products
                .Where(p => p.ProductID == id)
                .Include(p => p.CveConfigurations)
                .ThenInclude(cc => cc.Cve).FirstOrDefault();

            if (product == null)
            {
                TempData["Param"] = "Product";
                TempData["Error"] = "Product does not exist";
                return RedirectToAction("Error", "Home");
            }

            return View(product);
        }

        [HttpPost]
        public IActionResult TrackProductExistingConfiguration(int existingID, int productID)
        {
            // Get the existing configuration
            var existingConfiguration = _context.Configurations
                .Where(c => c.ConfigurationID == existingID)
                    .Include(c => c.UserCveConfigurations)
                        .FirstOrDefault();

            // Get existing product
            var existingProduct = _context.Products
                .Where(p => p.ProductID== productID)
                    .Include(p => p.CveConfigurations)
                        .FirstOrDefault();

            var unresolvedStatus = _context.Status
                .Where(s => s.StatusName == "Unresolved")
                    .FirstOrDefault();

            if (existingConfiguration == null ||
                existingProduct == null ||
                unresolvedStatus == null)
            {
                TempData["Param"] = "Existing Data";
                TempData["Error"] = "The requested data does not exist";
                return RedirectToAction("Error", "Home");
            }

            // All all associated vulnerabilities to the existingConfiguration
            foreach (var currExistingConfiguration in existingProduct.CveConfigurations)
            {
                var newUserCveConfiguration = new UserCveConfiguration()
                {
                    ProductID = existingProduct.ProductID,
                    CveID = currExistingConfiguration.CveID,
                    StatusID = unresolvedStatus.StatusID,
                    Notes = "No user defined notes.",
                    DateAdded = DateTime.Now,
                    New = 'N'.ToString()
                };

                existingConfiguration.UserCveConfigurations.Add(newUserCveConfiguration);
            }

            // Save changes to existingConfiguration
            try
            {
                _context.Configurations.Update(existingConfiguration);
                _context.SaveChanges();
                return Ok();
            }
            catch (Exception e)
            {
                TempData["Param"] = "userCveConfigurationToUpdate change save";
                TempData["Error"] = e.Message;
                throw e;
            }

        }

        [HttpPost]
        public IActionResult TrackProductNewConfiguration(string newConfigurationName, string configurationNotes, int productId)
        {
            // Create newConfiguration
            var newConfiguration = new Configuration()
            {
                ConfigurationName = newConfigurationName,
                Notes = configurationNotes,
                DateAdded = DateTime.Now,
                AspNetUserID = _userManager.GetUserId(User),
                UserCveConfigurations = new List<UserCveConfiguration>()
            };

            // Get existingProduct
            var existingProduct = _context.Products
                .Where(p => p.ProductID == productId)
                    .Include(p => p.CveConfigurations)
                    .FirstOrDefault();

            // Get the unresolvedStatus
            var unresolvedStatus = _context.Status
                .Where(s => s.StatusName == "Unresolved")
                    .FirstOrDefault();

            // Check for errors
            if (existingProduct == null ||
                unresolvedStatus == null)
            {
                TempData["Param"] = "Product";
                TempData["Error"] = "Product does not exist";
                return RedirectToAction("Error", "Home");
            }

            // For each associated vulnerability, create a newUserCveConfiguration
            foreach (var currExistingConfiguration in existingProduct.CveConfigurations)
            {
                var newUserCveConfiguration = new UserCveConfiguration()
                {
                    ProductID = existingProduct.ProductID,
                    CveID = currExistingConfiguration.CveID,
                    StatusID = unresolvedStatus.StatusID,
                    Notes = "No user defined notes.",
                    DateAdded = DateTime.Now,
                    New = "N".ToString()
                };

                newConfiguration.UserCveConfigurations.Add(newUserCveConfiguration);
            }

            // Save the newConfiguration
            try
            {
                _context.Configurations.Add(newConfiguration);
                _context.SaveChanges();
                return Ok();
            }
            catch (Exception e)
            {
                TempData["Param"] = "Product";
                TempData["Error"] = e.Message;
                throw e;
            }

        }
    }
}