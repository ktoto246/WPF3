using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WpfApp1.Models
{
    [Table("Recipients")]
    public class Recipient
    {
        [Key]
        public int RecipientId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

        [MaxLength(20)]
        public string? ContactPhone { get; set; }

        [MaxLength(150)]
        public string? ContactPerson { get; set; }

        public virtual ICollection<ComponentIssue> ComponentIssues { get; set; } = new List<ComponentIssue>();
    }

    [Table("Employees")]
    public class Employee
    {
        [Key]
        public int EmployeeId { get; set; }

        [Required]
        [MaxLength(150)]
        public string FullName { get; set; }

        [Required]
        [MaxLength(50)]
        public string Login { get; set; }

        [Required]
        [MaxLength(100)]
        public string Position { get; set; }

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "Регистратор";

        [MaxLength(100)]
        public string? ContactInfo { get; set; }

        [Required]
        [MaxLength(100)]
        public string Password { get; set; } = "123456";

        public bool IsActive { get; set; } = true;

        public virtual ICollection<MedicalExam> MedicalExams { get; set; } = new List<MedicalExam>();
        public virtual ICollection<Donation> Donations { get; set; } = new List<Donation>();
        public virtual ICollection<ComponentIssue> ComponentIssues { get; set; } = new List<ComponentIssue>();
    }

    [Table("Donors")]
    public class Donor
    {
        [Key]
        public int DonorId { get; set; }

        [Required]
        [MaxLength(150)]
        public string FullName { get; set; }

        public DateTime BirthDate { get; set; }

        [Required]
        [MaxLength(1)]
        public string Gender { get; set; }

        [Required]
        [MaxLength(50)]
        public string PassportData { get; set; }

        [Required]
        [MaxLength(5)]
        public string BloodGroup { get; set; }

        [Required]
        [MaxLength(1)]
        public string RhFactor { get; set; }

        [MaxLength(10)]
        public string? KellAntigen { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

        [MaxLength(20)]
        public string? ContactPhone { get; set; }

        [MaxLength(150)]
        public string? Email { get; set; }

        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Активен";

        public DateTime? DisqualifiedUntil { get; set; }

        public bool IsHonoraryDonor { get; set; } = false;

        [MaxLength(50)]
        public string? HonoraryDonorNumber { get; set; }

        public string? Notes { get; set; }

        public virtual ICollection<MedicalExam> MedicalExams { get; set; } = new List<MedicalExam>();
        public virtual ICollection<Donation> Donations { get; set; } = new List<Donation>();
    }

    [Table("MedicalExams")]
    public class MedicalExam
    {
        [Key]
        public int ExamId { get; set; }

        public int DonorId { get; set; }

        public int EmployeeId { get; set; }

        public DateTime ExamDate { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(20)]
        public string Result { get; set; }

        [MaxLength(200)]
        public string? RejectionReason { get; set; }

        public decimal? HemoglobinGdl { get; set; }

        public short? SystolicBP { get; set; }
        public short? DiastolicBP { get; set; }

        public short? PulseBpm { get; set; }

        public decimal? WeightKg { get; set; }

        public decimal? TemperatureC { get; set; }

        public decimal? TotalProteinGdl { get; set; }
        public decimal? AltUL { get; set; }

        public string? Notes { get; set; }

        [ForeignKey("DonorId")]
        public virtual Donor Donor { get; set; }

        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; }

        public virtual ICollection<Donation> Donations { get; set; } = new List<Donation>();
    }

    [Table("Donations")]
    public class Donation
    {
        [Key]
        public int DonationId { get; set; }

        [Required]
        [MaxLength(50)]
        public string DonationNumber { get; set; }

        public int DonorId { get; set; }

        public int EmployeeId { get; set; }

        public int? ExamId { get; set; }

        public DateTime DonationDate { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(50)]
        public string DonationType { get; set; }

        public int VolumeMl { get; set; }

        [Required]
        [MaxLength(30)]
        public string MedicalStatus { get; set; } = "На проверке";

        [ForeignKey("DonorId")]
        public virtual Donor Donor { get; set; }

        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; }

        [ForeignKey("ExamId")]
        public virtual MedicalExam? MedicalExam { get; set; }

        public virtual ICollection<BloodComponent> BloodComponents { get; set; } = new List<BloodComponent>();
    }

    [Table("BloodComponents")]
    public class BloodComponent
    {
        [Key]
        public int ComponentId { get; set; }

        public int DonationId { get; set; }

        [Required]
        [MaxLength(50)]
        public string LotNumber { get; set; }

        [Required]
        [MaxLength(100)]
        public string ComponentType { get; set; }

        public int VolumeMl { get; set; }

        public DateTime CollectionDate { get; set; }

        public DateTime ExpirationDate { get; set; }

        [MaxLength(100)]
        public string? StorageLocation { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "В наличии";

        [ForeignKey("DonationId")]
        public virtual Donation Donation { get; set; }

        public virtual ICollection<ComponentIssue> ComponentIssues { get; set; } = new List<ComponentIssue>();
    }

    [Table("ComponentIssues")]
    public class ComponentIssue
    {
        [Key]
        public int IssueId { get; set; }

        public int ComponentId { get; set; }

        public int EmployeeId { get; set; }

        public int? RecipientId { get; set; }

        public DateTime IssueDate { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(20)]
        public string IssueType { get; set; } = "Выдача";

        [MaxLength(100)]
        public string? WriteOffReason { get; set; }

        public string? Comments { get; set; }

        [ForeignKey("ComponentId")]
        public virtual BloodComponent BloodComponent { get; set; }

        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; }

        [ForeignKey("RecipientId")]
        public virtual Recipient? Recipient { get; set; }
    }
    [Table("LaboratoryTests")]
    public class LaboratoryTest
    {
        [Key]
        public int TestId { get; set; }

        public int DonationId { get; set; }

        public int EmployeeId { get; set; }

        public DateTime TestDate { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(20)]
        public string HIV_Result { get; set; }

        [Required]
        [MaxLength(20)]
        public string HBsAg_Result { get; set; }

        [Required]
        [MaxLength(20)]
        public string HCV_Result { get; set; }

        [Required]
        [MaxLength(20)]
        public string Syphilis_Result { get; set; }

        public decimal? AltUL { get; set; }

        [MaxLength(20)]
        public string? NAT_HIV { get; set; }

        [MaxLength(20)]
        public string? NAT_HBV { get; set; }

        [MaxLength(20)]
        public string? NAT_HCV { get; set; }

        [Required]
        [MaxLength(20)]
        public string OverallResult { get; set; }

        public string? Notes { get; set; }

        [ForeignKey("DonationId")]
        public virtual Donation Donation { get; set; }

        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; }
    }

    [Table("PlasmaQuarantine")]
    public class PlasmaQuarantine
    {
        [Key]
        public int QuarantineId { get; set; }

        public int ComponentId { get; set; }

        public DateTime StartDate { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime PlannedReleaseDate { get; set; }

        public int? ConfirmationDonationId { get; set; }

        public int? ConfirmationTestId { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "На карантине";

        public DateTime? ReleasedDate { get; set; }

        public int? ReleasedByEmployeeId { get; set; }

        [ForeignKey("ComponentId")]
        public virtual BloodComponent BloodComponent { get; set; }

        [ForeignKey("ConfirmationDonationId")]
        public virtual Donation? ConfirmationDonation { get; set; }

        [ForeignKey("ConfirmationTestId")]
        public virtual LaboratoryTest? ConfirmationTest { get; set; }

        [ForeignKey("ReleasedByEmployeeId")]
        public virtual Employee? ReleasedByEmployee { get; set; }
    }

    [Table("AuditLog")]
    public class AuditLog
    {
        [Key]
        public long LogId { get; set; }

        public int? EmployeeId { get; set; }

        public DateTime ActionTimestamp { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(100)]
        public string TableName { get; set; }

        public int? RecordId { get; set; }

        [Required]
        [MaxLength(10)]
        public string ActionType { get; set; }

        public string? OldValues { get; set; }

        public string? NewValues { get; set; }

        [MaxLength(50)]
        public string? IPAddress { get; set; }

        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }
    }
}