using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vulnaware.Models;

namespace Vulnaware.Data
{
    public class DatabaseContext : IdentityDbContext<ApplicationUser>
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Cve> Cves { get; set; }
        public DbSet<Reference> References { get; set; }
        public DbSet<Cwe> Cwes { get; set; }
        public DbSet<CveConfiguration> CveConfigurations { get; set; }
        public DbSet<TrackedCve> TrackedCves { get; set; }
        public DbSet<UserPreference> UserPreferences { get; set; }
        public DbSet<Configuration> Configurations { get; set; }
        public DbSet<UserCveConfiguration> UserCveConfigurations { get; set; }
        public DbSet<Status> Status { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Override and remove "s" from table names to match convention
            modelBuilder.Entity<Product>().ToTable("Product");
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Concatenated).IsUnique();

            modelBuilder.Entity<Cve>().ToTable("Cve");
            modelBuilder.Entity<Reference>().ToTable("Reference");
            modelBuilder.Entity<Cwe>().ToTable("Cwe");
            modelBuilder.Entity<CveConfiguration>().ToTable("CveConfiguration");
            modelBuilder.Entity<TrackedCve>().ToTable("TrackedCve");
            modelBuilder.Entity<UserPreference>().ToTable("UserPreference");
            modelBuilder.Entity<Configuration>().ToTable("Configuration");
            modelBuilder.Entity<UserCveConfiguration>().ToTable("UserCveConfiguration");
            modelBuilder.Entity<Status>().ToTable("Status");

            // CveConfiguration composite key
            modelBuilder.Entity<CveConfiguration>()
                .HasKey(cc => new { cc.CveID, cc.ProductID});
            modelBuilder.Entity<CveConfiguration>()
                .HasOne(cc => cc.Cve)
                .WithMany(c => c.CveConfigurations)
                .HasForeignKey(cc => cc.CveID);
            modelBuilder.Entity<CveConfiguration>()
                .HasOne(cc => cc.Product)
                .WithMany(p => p.CveConfigurations)
                .HasForeignKey(p => p.ProductID);

            // TrackedCve composite key
            modelBuilder.Entity<TrackedCve>()
                .HasKey(tc => new { tc.AspNetUserID, tc.CveID });
            modelBuilder.Entity<TrackedCve>()
                .HasOne(tc => tc.ApplicationUser)
                .WithMany(au => au.TrackedCves)
                .HasForeignKey(au => au.AspNetUserID);
            modelBuilder.Entity<TrackedCve>()
                .HasOne(tc => tc.Cve)
                .WithMany(c => c.TrackedCves)
                .HasForeignKey(c => c.CveID);

            // UserCveConfiguration
            modelBuilder.Entity<UserCveConfiguration>()
                .HasKey(ucc => new { ucc.ConfigurationID, ucc.ProductID, ucc.CveID });
            modelBuilder.Entity<UserCveConfiguration>()
                .HasOne(ucc => ucc.Configuration)
                .WithMany(au => au.UserCveConfigurations)
                .HasForeignKey(au => au.ConfigurationID);
            modelBuilder.Entity<UserCveConfiguration>()
                .HasOne(ucc => ucc.Product)
                .WithMany(p => p.UserCveConfigurations)
                .HasForeignKey(p => p.ProductID);
            modelBuilder.Entity<UserCveConfiguration>()
                .HasOne(ucc => ucc.Cve)
                .WithMany(c => c.UserCveConfigurations)
                .HasForeignKey(c => c.CveID);

            // Resolve Configuration user key issue
            modelBuilder.Entity<Configuration>()
                .HasOne(c => c.ApplicationUser)
                .WithMany(c => c.Configurations)
                .HasForeignKey(au => au.AspNetUserID);
        }
    }
}
