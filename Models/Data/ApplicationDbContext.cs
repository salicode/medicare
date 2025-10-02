using Microsoft.EntityFrameworkCore;

namespace MediCare.Models.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<PatientRecord> PatientRecords => Set<PatientRecord>();
        public DbSet<TestResult> TestResults => Set<TestResult>();
        public DbSet<Prescription> Prescriptions => Set<Prescription>();
        public DbSet<Vital> Vitals => Set<Vital>();
        public DbSet<UserPatientAssignment> UserPatientAssignments => Set<UserPatientAssignment>();

        public DbSet<EmailConfirmationToken> EmailConfirmationTokens => Set<EmailConfirmationToken>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ADDED: Unique constraints
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<EmailConfirmationToken>()
                .HasIndex(t => t.Token)
                .IsUnique();

            modelBuilder.Entity<EmailConfirmationToken>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        
    }
}