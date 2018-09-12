using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Xml;
using Vulnaware.Data;
using Vulnaware.Models;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Vulnaware.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    [Route("[controller]/[action]")]
    public class UploadController : Controller
    {
        private readonly DatabaseContext _context;
        private List<Email> emailRecipientList = new List<Email>();

        public UploadController(DatabaseContext context)
        {
            _context = context;
        }

        [TempData]
        public string StatusMessage { get; set; }

        // GET: /<controller>/
        public IActionResult Index()
        {
            ViewBag.FileCount = Directory.GetFiles(
                "wwwroot\\CVE Data", "*").Length -1;

            ViewBag.StatusMessage = StatusMessage;
            return View();
        }

        public IActionResult InitialDataImport()
        {
            // Get all file paths
            IList<string> pathList = new List<string>();

            // Foreach file
            foreach (string currFile in Directory.EnumerateFiles(
                "wwwroot\\CVE Data", "*",
                SearchOption.AllDirectories))
            {
                string jsonString = System.IO.File.ReadAllText(currFile);
                JObject jsonObject = JObject.Parse(jsonString);


                IList<JToken> jsonCveList = jsonObject["CVE_Items"].ToList();

                //Collect values from each Cve and create Cve object
                IList<Cve> newCveList = new List<Cve>();
                foreach (JToken currCve in jsonCveList)
                {
                    // Only accept if CVE description does not contain ** REJECT ** and has a score
                    if (!currCve["cve"]["description"]["description_data"].First["value"].ToString().Contains("** REJECT **"))
                    {
                        JToken cveMeta = currCve["cve"]["CVE_data_meta"];

                        // Detect errornous score information and restart
                        JToken cvss;
                        try
                        {
                            cvss = currCve["impact"]["baseMetricV2"]["cvssV2"];
                        }
                        catch (Exception e)
                        {
                            continue;
                        }

                        // Create new CveObject
                        Cve newCve = new Cve
                        {
                            PublishedDate = Convert.ToDateTime(currCve["publishedDate"].ToString()),
                            LastModifiedDate = Convert.ToDateTime(currCve["lastModifiedDate"].ToString()),
                            GivenID = cveMeta["ID"].ToString(),
                            Description = currCve["cve"]["description"]["description_data"].First["value"].ToString(),
                            VectorString = cvss["vectorString"].ToString(),
                            AccessVector = cvss["accessVector"].ToString(),
                            AccessComplexity = cvss["accessComplexity"].ToString(),
                            Authentication = cvss["authentication"].ToString(),
                            ConfidentialityImpact = cvss["confidentialityImpact"].ToString(),
                            IntegrityImpact = cvss["availabilityImpact"].ToString(),
                            AvailabilityImpact = cvss["availabilityImpact"].ToString(),
                            BaseScore = Convert.ToDouble(cvss["baseScore"].ToString()),
                            References = new List<Reference>(),
                            CveConfigurations = new List<CveConfiguration>()
                        };

                        // Get references
                        JToken jsonReferenceList = currCve["cve"]["references"]["reference_data"];
                        foreach (JToken currReference in jsonReferenceList)
                        {
                            Reference newReference = new Reference
                            {
                                Url = currReference["url"].ToString()
                            };
                            newCve.References.Add(newReference);
                        }

                        // Get products from CVE
                        JObject currCveObject = JObject.Parse(currCve.ToString());
                        IList<JToken> jsonProductList = currCveObject.Descendants()
                            .Where(t => t.Type == JTokenType.Property && ((JProperty)t).Name == "cpe23Uri")
                            .Select(p => ((JProperty)p).Value).ToList();

                        jsonProductList = jsonProductList.Distinct().ToList();

                        // Foreach product
                        foreach (JToken currJsonProduct in jsonProductList)
                        {

                            // Get existing product if it exists
                            string currJsonProductString = currJsonProduct.ToString();
                            Product existingProduct = _context.Products
                                .Where(p => p.Concatenated == currJsonProductString)
                                .FirstOrDefault();

                            // If existing product does exist in the database
                            if (existingProduct != null)
                            {
                                CveConfiguration newConfiguration = new CveConfiguration
                                {
                                    Product = existingProduct
                                };

                                newCve.CveConfigurations.Add(newConfiguration);
                            }
                            else // If the existing product doesn't exist in the database
                            {
                                // Break down the currJsonProductString into its components
                                IList<string> productPartList = new List<string>();
                                productPartList = currJsonProductString.Split(":");

                                // Create new product
                                Product newProduct = new Product
                                {
                                    Concatenated = currJsonProductString,
                                    Part = productPartList[2],
                                    Vendor = productPartList[3],
                                    ProductName = productPartList[4],
                                    Version = productPartList[5],
                                    ProductUpdate = productPartList[6],
                                    Edition = productPartList[7],
                                    ProductLanguage = productPartList[8],
                                    Added = DateTime.Now
                                };

                                // Add the new product to newCveConfiguration
                                CveConfiguration newCveConfiguration = new CveConfiguration
                                {
                                    Product = newProduct
                                };

                                // Add the newCveConfiguration to the newCve
                                newCve.CveConfigurations.Add(newCveConfiguration);
                            }
                        }

                        // Add new CVE to database
                        try
                        {
                            _context.Cves.Add(newCve);
                            _context.SaveChanges();
                        }
                        catch (Exception e)
                        {
                            TempData["Param"] = "newCve";
                            TempData["Error"] = e.Message;
                            throw e;
                        }
                    }

                }
            }

            StatusMessage = "Successfully Seeded Database.";
            return RedirectToAction(nameof(Index));
        }

        // Called by hosting service, only to return status codes
        [AllowAnonymous]
        public IActionResult ImportModifiedCveData(string guid)
        {
            // Only run if URL calls with the below GUID for security
            if (guid == "3777706d-0db3-4a6c-9f34-2cbd672fbd89")
            {
                // Clear directories https://stackoverflow.com/questions/1288718/how-to-delete-all-files-and-folders-in-a-directory

                Uri uri = new Uri("https://static.nvd.nist.gov/feeds/json/cve/1.0/nvdcve-1.0-recent.json.zip");
                string zipPath = "wwwroot\\CVE Data\\recent.zip";
                string extractPath = "wwwroot\\CVE Data\\";
                string filePath;

                // Download the file
                using (var client = new WebClient())
                {
                    try
                    {
                        client.DownloadFile(uri, zipPath);
                    }
                    catch (Exception)
                    {
                        return StatusCode(500);
                    }
                }

                // Extract the file
                using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                {
                    ZipArchiveEntry entry = archive.Entries.Where(e => e.FullName.EndsWith(".json")).FirstOrDefault();
                    filePath = Path.Combine(extractPath, entry.FullName);
                    entry.ExtractToFile(filePath, true);
                }

                // Get the CVE items
                string jsonString = System.IO.File.ReadAllText(filePath);
                JObject jsonObject = JObject.Parse(jsonString);
                IList<JToken> jsonCveList = jsonObject["CVE_Items"].ToList();

                // Foreach CVE
                foreach (JToken currCve in jsonCveList)
                {
                    // Ignore rejected CVE's
                    if (!currCve["cve"]["description"]["description_data"].First["value"].ToString().Contains("** REJECT **"))
                    {
                        JToken cveMeta = currCve["cve"]["CVE_data_meta"];

                        // Get existing cve
                        string existingCveGivenId = cveMeta["ID"].ToString();
                        Cve existingCve = _context.Cves
                            .Where(c => c.GivenID == existingCveGivenId)
                            .Include(c => c.CveConfigurations)
                            .ThenInclude(cc => cc.Product)
                            .Include(c => c.References)
                            .FirstOrDefault();

                        // If the CVE already exists
                        if (existingCve != null)
                        {
                            // Update existing CVE
                            //UpdateExistingCve(existingCve, currCve);

                        }
                        else // If the CVE doesn't exist
                        {
                            // Add new CVE
                            AddNewCve(currCve);
                        }
                    }
                }

                // Send emails
                SendEmails();

                // Return success code
                return StatusCode(200);
            }
            else
            {
                return StatusCode(401);
            }
        }
        private void UpdateExistingCve(Cve existingCve, JToken currCve)
        {
            JToken cveMeta = currCve["cve"]["CVE_data_meta"];
            JToken cvss;

            // Detect errornous score information and restart
            try
            {
                cvss = currCve["impact"]["baseMetricV2"]["cvssV2"];
            }
            catch (Exception)
            {
                return;
            }

            // Update the CVE itself
            existingCve.PublishedDate = Convert.ToDateTime(currCve["publishedDate"].ToString());
            existingCve.LastModifiedDate = Convert.ToDateTime(currCve["lastModifiedDate"].ToString());
            existingCve.GivenID = cveMeta["ID"].ToString();
            existingCve.Description = currCve["cve"]["description"]["description_data"].First["value"].ToString();
            existingCve.VectorString = cvss["vectorString"].ToString();
            existingCve.AccessVector = cvss["accessVector"].ToString();
            existingCve.AccessComplexity = cvss["accessComplexity"].ToString();
            existingCve.Authentication = cvss["authentication"].ToString();
            existingCve.ConfidentialityImpact = cvss["confidentialityImpact"].ToString();
            existingCve.IntegrityImpact = cvss["availabilityImpact"].ToString();
            existingCve.AvailabilityImpact = cvss["availabilityImpact"].ToString();
            existingCve.BaseScore = Convert.ToDouble(cvss["baseScore"].ToString());

            existingCve.References.Clear();
            JToken jsonReferenceList = currCve["cve"]["references"]["reference_data"];
            foreach (JToken currReference in jsonReferenceList)
            {
                Reference newReference = new Reference
                {
                    Url = currReference["url"].ToString()
                };
                existingCve.References.Add(newReference);
            }

            // Add any new products
            JObject currCveObject = JObject.Parse(currCve.ToString());
            IList<JToken> jsonProductList = currCveObject.Descendants()
                .Where(t => t.Type == JTokenType.Property && ((JProperty)t).Name == "cpe23Uri")
                .Select(p => ((JProperty)p).Value).ToList();

            jsonProductList = jsonProductList.Distinct().ToList();

            bool alreadyContained = false;
            foreach (JToken currJsonProduct in jsonProductList)
            {
                // Check for existing product
                foreach (CveConfiguration currCveConfiguration in existingCve.CveConfigurations)
                {
                    if (currCveConfiguration.Product.Concatenated == currJsonProduct.ToString())
                    {
                        alreadyContained = true;
                        break;
                    }
                }

                // If not already contained add the new product
                if (alreadyContained == false)
                {
                    // Get existing product if it exists
                    string currJsonProductString = currJsonProduct.ToString();
                    Product existingProduct = _context.Products
                        .Where(p => p.Concatenated == currJsonProductString)
                        .Include(p => p.CveConfigurations)
                        .FirstOrDefault();

                    // If existing product does exist in the database
                    if (existingProduct != null)
                    {
                        CveConfiguration newConfiguration = new CveConfiguration
                        {
                            Product = existingProduct
                        };

                        existingCve.CveConfigurations.Add(newConfiguration);

                        // Update tracked userCveConfigurations where this product is already tracked
                        Status status = _context.Status.Where(s => s.StatusName == "Unresolved").FirstOrDefault();
                        foreach (var item in _context.UserCveConfigurations
                            .Where(ucc => ucc.ProductID == existingProduct.ProductID)
                            .GroupBy(ucc => ucc.ProductID)
                            .Select(g => g.First()).ToList())
                        {
                            UserCveConfiguration newUserCveConfiguration = new UserCveConfiguration()
                            {
                                ProductID = existingProduct.ProductID,
                                CveID = existingCve.CveID,
                                ConfigurationID = item.CveID,
                                StatusID = status.StatusID,
                                Notes = "No user defined notes.",
                                DateAdded = DateTime.Now
                            };

                            try
                            {
                                if (ModelState.IsValid)
                                {
                                    _context.UserCveConfigurations.Add(newUserCveConfiguration);
                                    _context.SaveChanges();
                                }
                            }
                            catch (Exception)
                            {
                                return;
                            }
                        }
                    }
                    else // If the existing product doesn't exist in the database
                    {
                        // Break down the currJsonProductString into its components
                        IList<string> productPartList = new List<string>();
                        productPartList = currJsonProductString.Split(":");

                        // Create new product
                        Product newProduct = new Product
                        {
                            Concatenated = currJsonProductString,
                            Part = productPartList[2],
                            Vendor = productPartList[3],
                            ProductName = productPartList[4],
                            Version = productPartList[5],
                            ProductUpdate = productPartList[6],
                            Edition = productPartList[7],
                            ProductLanguage = productPartList[8]
                        };

                        // Add the new product to newCveConfiguration
                        CveConfiguration newCveConfiguration = new CveConfiguration
                        {
                            Product = newProduct
                        };

                        // Add the newCveConfiguration to the newCve
                        existingCve.CveConfigurations.Add(newCveConfiguration);
                    }
                }
            }
            // save updated existingCve
            try
            {
                if (ModelState.IsValid)
                {
                    _context.Entry(existingCve).State = EntityState.Modified;
                    _context.SaveChanges();
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        private void AddNewCve(JToken currCve)
        {
            // Only accept if CVE description does not contain ** REJECT ** and build data
            if (!currCve["cve"]["description"]["description_data"].First["value"].ToString().Contains("** REJECT **"))
            {
                JToken cveMeta = currCve["cve"]["CVE_data_meta"];

                JToken cvss;

                if (currCve["impact"].Children().Count() == 0)
                {
                    return;
                }

                cvss = currCve["impact"]["baseMetricV2"]["cvssV2"];

                // Create new CveObject
                Cve newCve = new Cve
                {
                    PublishedDate = Convert.ToDateTime(currCve["publishedDate"].ToString()),
                    LastModifiedDate = Convert.ToDateTime(currCve["lastModifiedDate"].ToString()),
                    GivenID = cveMeta["ID"].ToString(),
                    Description = currCve["cve"]["description"]["description_data"].First["value"].ToString(),
                    VectorString = cvss["vectorString"].ToString(),
                    AccessVector = cvss["accessVector"].ToString(),
                    AccessComplexity = cvss["accessComplexity"].ToString(),
                    Authentication = cvss["authentication"].ToString(),
                    ConfidentialityImpact = cvss["confidentialityImpact"].ToString(),
                    IntegrityImpact = cvss["availabilityImpact"].ToString(),
                    AvailabilityImpact = cvss["availabilityImpact"].ToString(),
                    BaseScore = Convert.ToDouble(cvss["baseScore"].ToString()),
                    References = new List<Reference>(),
                    CveConfigurations = new List<CveConfiguration>(),
                    UserCveConfigurations = new List<UserCveConfiguration>()
                };

                // Get references
                JToken jsonReferenceList = currCve["cve"]["references"]["reference_data"];
                foreach (JToken currReference in jsonReferenceList)
                {
                    Reference newReference = new Reference
                    {
                        Url = currReference["url"].ToString()
                    };
                    newCve.References.Add(newReference);
                }

                // Get products from CVE
                JObject currCveObject = JObject.Parse(currCve.ToString());
                IList<JToken> jsonProductList = currCveObject.Descendants()
                    .Where(t => t.Type == JTokenType.Property && ((JProperty)t).Name == "cpe23Uri")
                    .Select(p => ((JProperty)p).Value).ToList();

                jsonProductList = jsonProductList.Distinct().ToList();

                // Foreach product
                foreach (JToken currJsonProduct in jsonProductList)
                {

                    // Get existing product if it exists
                    string currJsonProductString = currJsonProduct.ToString();
                    Product existingProduct = _context.Products
                        .Where(p => p.Concatenated == currJsonProductString)
                        .FirstOrDefault();

                    // If existing product does exist in the database
                    if (existingProduct != null)
                    {
                        CveConfiguration newConfiguration = new CveConfiguration
                        {
                            Product = existingProduct
                        };

                        newCve.CveConfigurations.Add(newConfiguration);

                        // Update tracked userCveConfigurations where this product is already tracked
                        Status status = _context.Status.Where(s => s.StatusName == "Unresolved").FirstOrDefault();
                        foreach (var item in _context.UserCveConfigurations
                            .Where(ucc => ucc.ProductID == existingProduct.ProductID)
                            .GroupBy(g => g.ConfigurationID).ToList())
                        {
                            UserCveConfiguration newUserCveConfiguration = new UserCveConfiguration()
                            {
                                ProductID = existingProduct.ProductID,
                                ConfigurationID = item.Key,
                                StatusID = status.StatusID,
                                Notes = "No user defined notes.",
                                DateAdded = DateTime.Now,
                                Cve = newCve,
                                New = 'Y'.ToString()
                            };

                            // Add email notification if not already added
                            var existingConfiguration = _context.Configurations
                                .Where(c => c.ConfigurationID == item.Key)
                                .FirstOrDefault();

                            var userToEmail = _context.Users
                                .Where(u => u.Id == existingConfiguration.AspNetUserID)
                                .FirstOrDefault();

                            // Check if an email already exists
                            bool emailAlreadyExists = emailRecipientList
                                .Where(erl => erl.EmailAddress == userToEmail.Email).Count() > 0;

                            // If the email does exist
                            if (emailAlreadyExists == true)
                            {
                                // Add configuration Name
                                if (!emailRecipientList.Where(erl => erl.EmailAddress == userToEmail.Email).First()
                                        .ConfigurationIdList.Contains(existingConfiguration.ConfigurationID))
                                {
                                    emailRecipientList.Where(erl => erl.EmailAddress == userToEmail.Email).First()
                                        .ConfigurationIdList.Add(existingConfiguration.ConfigurationID);
                                }
                            } else // If the email does not exist
                            {
                                // Create new email and add it to the recipient list
                                Email newEmail = new Email()
                                {
                                    EmailAddress = userToEmail.Email,
                                    RecipientName = userToEmail.FirstName,
                                };
                                newEmail.ConfigurationIdList.Add(existingConfiguration.ConfigurationID);
                                emailRecipientList.Add(newEmail);
                            }

                            newCve.UserCveConfigurations.Add(newUserCveConfiguration);
                        }
                    }
                    else // If the existing product doesn't exist in the database
                    {
                        // Break down the currJsonProductString into its components
                        IList<string> productPartList = new List<string>();
                        productPartList = currJsonProductString.Split(":");

                        // Create new product
                        Product newProduct = new Product
                        {
                            Concatenated = currJsonProductString,
                            Part = productPartList[2],
                            Vendor = productPartList[3],
                            ProductName = productPartList[4],
                            Version = productPartList[5],
                            ProductUpdate = productPartList[6],
                            Edition = productPartList[7],
                            ProductLanguage = productPartList[8],
                            Added = DateTime.Now
                        };

                        // Add the new product to newCveConfiguration
                        CveConfiguration newCveConfiguration = new CveConfiguration
                        {
                            Product = newProduct
                        };

                        // Add the newCveConfiguration to the newCve
                        newCve.CveConfigurations.Add(newCveConfiguration);

                    }
                }

                // Add new CVE to database
                try
                {
                    _context.Cves.Add(newCve);
                    _context.SaveChanges();
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("Error: " + e.Message);
                }
            }
        }

        private void SendEmails()
        {
            // Send email notifications
            SmtpClient emailClient = new SmtpClient("mail.vulnaware.co.uk");
            emailClient.UseDefaultCredentials = false;
            emailClient.Credentials = new NetworkCredential("notifications@vulnaware.co.uk", "Leakymoose12.v");

            foreach (var emailRecipient in emailRecipientList)
            {
                MailMessage mailMessage = new MailMessage();
                mailMessage.From = new MailAddress("notifications@vulnaware.co.uk");
                mailMessage.To.Add(emailRecipient.EmailAddress);
                string bodyConfigurationList = "";
                foreach (var configurationIdItem in emailRecipient.ConfigurationIdList)
                {
                    string configurationName = _context.Configurations
                        .Where(c => c.ConfigurationID == configurationIdItem)
                        .FirstOrDefault().ConfigurationName;
                    bodyConfigurationList += "<br /><a href='https://vulnaware.co.uk/Configuration/Details?configurationId="
                        + configurationIdItem + "'>" + configurationName + "</a>";
                }
                mailMessage.Body = "Hi " + emailRecipient.RecipientName + ",<br /><br />"
                    + "New vulnerabilities have been detected in the following configurations:"
                    + bodyConfigurationList
                    + "<br /><br />Regards <br />"
                    + "The Vulnaware Team";
                mailMessage.Subject = "Vulnaware - New Vulnerabilities Detected";
                mailMessage.IsBodyHtml = true;
                emailClient.Send(mailMessage);
            }
        }

        // Only in place to remove test data
        public IActionResult RemoveTestData()
        {
            List<Cve> cveListToRemove = new List<Cve>();
            cveListToRemove = _context.Cves
                .Where(c => c.GivenID.Contains("CVE-2019"))
                .Include(c => c.References)
                .Include(c => c.CveConfigurations)
                .ToList();

            foreach (var item in cveListToRemove)
            {
                _context.Entry(item).State = EntityState.Deleted;
            }
            try
            {
                if (ModelState.IsValid)
                {
                    _context.SaveChanges();
                }
            }
            catch (Exception)
            {
                throw;
            }

            return Content("Success");
        }

        // For testing modified data
        [HttpPost]
        public IActionResult TestUpdate(IFormFile testFile)
        {
            // Check file type
            string fileExtension = Path.GetExtension(testFile.FileName);
            if (fileExtension != ".json" || testFile == null)
            {
                return RedirectToAction(nameof(Index));
            }

            // Get the CVE items
            string jsonString = "";
            using (var reader = new StreamReader(testFile.OpenReadStream()))
            {
                jsonString = reader.ReadToEnd();
            }

            JObject jsonObject = JObject.Parse(jsonString);
            IList<JToken> jsonCveList = jsonObject["CVE_Items"].ToList();

            // Foreach CVE
            foreach (JToken currCve in jsonCveList)
            {
                // Ignore rejected CVE's
                if (!currCve["cve"]["description"]["description_data"].First["value"].ToString().Contains("** REJECT **"))
                {
                    JToken cveMeta = currCve["cve"]["CVE_data_meta"];

                    // Get existing cve
                    string existingCveGivenId = cveMeta["ID"].ToString();
                    Cve existingCve = _context.Cves
                        .Where(c => c.GivenID == existingCveGivenId)
                        .Include(c => c.CveConfigurations)
                        .ThenInclude(cc => cc.Product)
                        .Include(c => c.References)
                        .FirstOrDefault();

                    // If the CVE already exists
                    if (existingCve != null)
                    {
                        // Update existing CVE
                        //UpdateExistingCve(existingCve, currCve);

                    }
                    else // If the CVE doesn't exist
                    {
                        // Add new CVE
                        AddNewCve(currCve);
                    }
                }
            }

            // Send emails
            SendEmails();

            return RedirectToAction(nameof(Index));
        }

        private class Email
        {
            public string EmailAddress { get; set; }
            public string RecipientName { get; set; }
            public IList<int> ConfigurationIdList = new List<int>();
        }

    }
}
