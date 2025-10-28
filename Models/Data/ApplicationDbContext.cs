using MediCare.MediCare;
using Microsoft.EntityFrameworkCore;
using MediCare.Models.Entities;
namespace MediCare.Models.Entities
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


        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<Specialization> Specializations => Set<Specialization>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<DoctorAvailability> DoctorAvailabilities => Set<DoctorAvailability>();
    public DbSet<Consultation> Consultations => Set<Consultation>();
    public DbSet<ConsultationDocument> ConsultationDocuments => Set<ConsultationDocument>();


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


            modelBuilder.Entity<Role>()
            .HasIndex(r => r.Name)
            .IsUnique();

            // ADDED: Permission configurations
            modelBuilder.Entity<Permission>()
                .HasIndex(p => p.Name)
                .IsUnique();

            // ADDED: RolePermission composite key and relationships
            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionId });

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.Permissions)
                .HasForeignKey(rp => rp.RoleId);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId);

            // ADDED: UserRole composite key and relationships
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            modelBuilder.Entity<Specialization>()
            .HasIndex(s => s.Name)
            .IsUnique();

        modelBuilder.Entity<Doctor>()
            .HasIndex(d => d.UserId)
            .IsUnique();

        modelBuilder.Entity<Doctor>()
            .HasOne(d => d.User)
            .WithOne()
            .HasForeignKey<Doctor>(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DoctorAvailability>()
            .HasIndex(da => new { da.DoctorId, da.DayOfWeek, da.StartTime })
            .IsUnique();

        modelBuilder.Entity<Consultation>()
            .HasIndex(c => new { c.DoctorId, c.ScheduledAt })
            .IsUnique();

        modelBuilder.Entity<Consultation>()
            .HasIndex(c => new { c.PatientRecordId, c.ScheduledAt });
    
        }


    }
}