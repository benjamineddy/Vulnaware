using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vulnaware.Data;
using Vulnaware.Models;

namespace Vulnaware.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class UserManagementController : Controller
    {
        private readonly DatabaseContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserManagementController(DatabaseContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context; 
            _userManager = userManager;
    }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult LoadUserManagementTable()
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

                // Get the list of users
                var userList = new List<UserManagementTable>();

                foreach(var userItem in _context.Users.ToList())
                {
                    UserManagementTable userManagementTable = new UserManagementTable()
                    {
                        AspUserID = userItem.Id,
                        FirstName = userItem.FirstName,
                        LastName = userItem.LastName,
                        Email = userItem.Email,
                        Role = _userManager.GetRolesAsync(userItem).Result.ToList().First()
                    };

                    userList.Add(userManagementTable);
                }



                var userListQ = userList.AsQueryable();

                // Sorting
                if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                {
                    switch (sortColumn)
                    {
                        case "FirstName":
                            if (sortColumnDirection == "asc")
                            {
                                userListQ = userListQ.OrderBy(ul => ul.FirstName);
                                break;
                            }
                            else
                            {
                                userListQ = userListQ.OrderByDescending(ul => ul.FirstName);
                                break;
                            }
                        case "LastName":
                            if (sortColumnDirection == "asc")
                            {
                                userListQ = userListQ.OrderBy(ul => ul.LastName);
                                break;
                            }
                            else
                            {
                                userListQ = userListQ.OrderByDescending(ul => ul.LastName);
                                break;
                            }
                        case "Email":
                            if (sortColumnDirection == "asc")
                            {
                                userListQ = userListQ.OrderBy(ul => ul.Email);
                                break;
                            }
                            else
                            {
                                userListQ = userListQ.OrderByDescending(ul => ul.Email);
                                break;
                            }
                    }
                }

                // Search
                if (!string.IsNullOrEmpty(searchValue))
                {
                    userListQ = userListQ.Where(ul => ul.FirstName.Contains(searchValue)
                    || ul.LastName.Contains(searchValue)
                    || ul.Email.Contains(searchValue));
                }

                // Set row count
                recordsTotal = userListQ.Count();

                // Set paging
                var data = userListQ.Skip(skip).Take(pageSize).ToList();

                // Return Json data
                return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        public IActionResult PromoteUser(string aspUserID)
        {
            // Get the user based on the ID
            var userToPromote = _userManager.FindByIdAsync(aspUserID).Result;

            if (userToPromote == null)
            {
                return NotFound();
            }

            // Add admin role
            Task<IdentityResult> addAdminRole = _userManager.AddToRoleAsync(userToPromote, "Admin");
            addAdminRole.Wait();

            // Remove Member role
            Task<IdentityResult> removeMemberRole = _userManager.RemoveFromRoleAsync(userToPromote, "Member");
            removeMemberRole.Wait();

            return Ok();
        }

        [HttpPost]
        public IActionResult DemoteUser(string aspUserID)
        {
            // Get the user based on the ID
            var userToDemote = _userManager.FindByIdAsync(aspUserID).Result;

            if (userToDemote == null)
            {
                return NotFound();
            }

            // Add member role
            Task<IdentityResult> addAdminRole = _userManager.AddToRoleAsync(userToDemote, "Member");
            addAdminRole.Wait();

            // Remove admin role
            Task<IdentityResult> removeMemberRole = _userManager.RemoveFromRoleAsync(userToDemote, "Admin");
            removeMemberRole.Wait();

            return Ok();
        }
    }
}