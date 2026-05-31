using Microsoft.EntityFrameworkCore;
using WpfApp1.Models;

namespace WpfApp1
{
    public class BloodBankContext : DbContext
    {
        public DbSet<Recipient> Recipients { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Donor> Donors { get; set; }
        public DbSet<MedicalExam> MedicalExams { get; set; }
        public DbSet<Donation> Donations { get; set; }
        public DbSet<BloodComponent> BloodComponents { get; set; }
        public DbSet<ComponentIssue> ComponentIssues { get; set; }
        public DbSet<LaboratoryTest> LaboratoryTests { get; set; }
        public DbSet<PlasmaQuarantine> PlasmaQuarantines { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=PC;Database=BloodBank;Trusted_Connection=True;TrustServerCertificate=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Donor>()
                .HasIndex(d => d.PassportData)
                .IsUnique();
        }
    }
}