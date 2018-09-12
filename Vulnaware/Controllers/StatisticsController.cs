using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vulnaware.Data;

namespace Vulnaware.Controllers
{
    [AllowAnonymous]
    public class StatisticsController : Controller
    {
        private readonly DatabaseContext _context;

        public StatisticsController(DatabaseContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Get this week's vulnerability count
            ViewBag.ThisWeeksVulnerabilityCount = _context.Cves
                .Where(c => c.PublishedDate >= DateTime.Today.AddDays(-7)).Count();

            // Get this month's vulnerability count
            DateTime startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            ViewBag.ThisMonthsVulnerabilityCount = _context.Cves
                .Where(c => c.PublishedDate >= startOfMonth).Count();

            // Get this year's vulnerability count
            DateTime startOfYear = new DateTime(DateTime.Now.Year, 1, 1);
            ViewBag.ThisYearsVulnerabilityCount = _context.Cves
                .Where(c => c.PublishedDate >= startOfYear).Count();

            // Get total vulnerability count
            ViewBag.TotalVulnerabilityCount = _context.Cves.Count();

            return View();
        }

        [HttpPost]
        public List<GraphData> LoadYearlyCountTable()
        {
            // X and Y values
            List<GraphData> dataList = new List<GraphData>();

            // Get the count for each year
            // Coded with assistance from https://stackoverflow.com/questions/7285714/linq-with-groupby-and-count
            foreach (var yearGroup in _context.Cves
                .GroupBy(c => c.PublishedDate.Year)
                .Select(group => new {
                    Metric = group.Key,
                    Count = group.Count()
                }).OrderBy(c => c.Metric))
            {
                // Create a new graphdata item and add the current year and the total vulnerabilties for that year
                GraphData newGraphData = new GraphData()
                {
                    X = yearGroup.Metric.ToString(),
                    Item = yearGroup.Count.ToString()
                };

                // Add new graphdata item to the list
                dataList.Add(newGraphData);
            }

            return dataList;
        }

        [HttpPost]
        public List<GraphData> LoadYearlyBaseScoreAverageTable()
        {
            // X and Y values
            List<GraphData> dataList = new List<GraphData>();

            // Get the count for each year
            // Coded with assistance from https://stackoverflow.com/questions/7285714/linq-with-groupby-and-count
            foreach (var yearGroup in _context.Cves
                .GroupBy(c => c.PublishedDate.Year)
                .Select(group => new {
                    Metric = group.Key,
                    Average = group.Average(c => c.BaseScore)
                }).OrderBy(c => c.Metric))
            {
                // Create a new graphdata item and add the current year and the yearly average base score for that year
                GraphData newGraphData = new GraphData()
                {
                    X = yearGroup.Metric.ToString(),
                    Item = yearGroup.Average.ToString("0.##")
                };

                // Add new graphdata item to the list
                dataList.Add(newGraphData);
            }

            return dataList;
        }

        public class GraphData
        {
            public string X { get; set; }
            public string Item { get; set; }
        }

    }
}