using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vulnaware.Data;
using Vulnaware.Models;

namespace Vulnaware.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly DatabaseContext _context;

        public HomeController(DatabaseContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Get total vulnerabilities
            ViewBag.TotalVulnerabilityCount = _context.Cves.Count();

            // Get this weeks vulnerabilities
            ViewBag.ThisWeeksVulnerabilityCount = _context.Cves
                .Where(c => c.PublishedDate >= DateTime.Today.AddDays(-7)
                    && c.PublishedDate <= DateTime.Today).Count();

            // Get this year's average base score
            DateTime startOfYear = new DateTime(DateTime.Now.Year, 1, 1);
            ViewBag.ThisYearsAveragebaseScore = _context.Cves
                .DefaultIfEmpty()
                .Where(cl => cl.PublishedDate >= startOfYear)
                .Average(cl => cl.BaseScore).ToString("0.##");

            // Get total average base score
            ViewBag.TotalAverageBaseScore = _context.Cves
                .DefaultIfEmpty()
                .Average(cl => cl.BaseScore).ToString("0.##");

            return View();
        }

        public IActionResult GetRecentVulnerabilities()
        {
            // Get the last 10 added vulnerabilities
            var recentVulnerabilityList = new List<Cve>();

            try
            {
                recentVulnerabilityList = _context.Cves
                    .Where(c => c.PublishedDate >= DateTime.Today.AddDays(-7)
                        && c.PublishedDate <= DateTime.Today).Take(10).ToList();
            }
            catch (ArgumentNullException e)
            {
                TempData["Param"] = "recentVulnerabilityList";
                TempData["Error"] = e.Message;
                throw e;
            }

            return Json(new { data = recentVulnerabilityList });
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
