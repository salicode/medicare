
using MediCare.MediCare;
using MediCare.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace MediCare.Models.Entities
{
    public static class DataSeeder
    {
        public static void Seed(ApplicationDbContext context, IPasswordHasher<User> hasher)
        {
            // Ensure database is created and migrations are applied
            context.Database.EnsureCreated();

            // Seed in correct order
            SeedPermissions(context);
            SeedRoles(context);
            SeedSpecializations(context); // ADD THIS

            // Seed default admin user
            if (!context.Users.Any(u => u.Username == "admin"))
            {
                SeedAdminUser(context, hasher);
                SeedSampleUsers(context, hasher);
                SeedDoctorProfiles(context); // ADD THIS
                SeedSampleConsultations(context); // ADD THIS
            }
        }

        // ADD THIS METHOD - Seed Specializations
        private static void SeedSpecializations(ApplicationDbContext context)
        {
            if (!context.Specializations.Any())
            {
                var specializations = new[]
                {
                    new Specialization { Name = "Cardiology", Description = "Heart and cardiovascular system" },
                    new Specialization { Name = "Dermatology", Description = "Skin, hair, and nails" },
                    new Specialization { Name = "Pediatrics", Description = "Children's health" },
                    new Specialization { Name = "Orthopedics", Description = "Bones and muscles" },
                    new Specialization { Name = "Neurology", Description = "Nervous system" },
                    new Specialization { Name = "Psychiatry", Description = "Mental health" },
                    new Specialization { Name = "General Practice", Description = "Primary care" }
                };

                context.Specializations.AddRange(specializations);
                context.SaveChanges();
            }
        }

        // ADD THIS METHOD - Seed Doctor Profiles
        private static void SeedDoctorProfiles(ApplicationDbContext context)
        {
            var doctorUser = context.Users.FirstOrDefault(u => u.Username == "drjohn");
            var defaultSpecialization = context.Specializations.FirstOrDefault(s => s.Name == "General Practice");

            if (doctorUser != null && defaultSpecialization != null && 
                !context.Doctors.Any(d => d.UserId == doctorUser.Id))
            {
                var doctor = new Doctor
                {
                    UserId = doctorUser.Id,
                    FullName = "Dr. John Smith",
                    SpecializationId = defaultSpecialization.Id,
                    PhoneNumber = "+1234567890",
                    Bio = "Experienced general practitioner with 8 years of practice.",
                    YearsOfExperience = 8,
                    ConsultationFee = 100.00m,
                    IsActive = true
                };

                context.Doctors.Add(doctor);
                context.SaveChanges();

                // Add sample availability
                SeedDoctorAvailability(context, doctor.Id);
            }
        }

        // ADD THIS METHOD - Seed Doctor Availability
        private static void SeedDoctorAvailability(ApplicationDbContext context, Guid doctorId)
        {
            if (!context.DoctorAvailabilities.Any(da => da.DoctorId == doctorId))
            {
                var availabilities = new[]
                {
                    new DoctorAvailability 
                    { 
                        DoctorId = doctorId, 
                        DayOfWeek = DayOfWeek.Monday, 
                        StartTime = TimeSpan.FromHours(9), 
                        EndTime = TimeSpan.FromHours(17),
                        IsRecurring = true,
                        MaxAppointmentsPerSlot = 4
                    },
                    new DoctorAvailability 
                    { 
                        DoctorId = doctorId, 
                        DayOfWeek = DayOfWeek.Wednesday, 
                        StartTime = TimeSpan.FromHours(9), 
                        EndTime = TimeSpan.FromHours(17),
                        IsRecurring = true,
                        MaxAppointmentsPerSlot = 4
                    },
                    new DoctorAvailability 
                    { 
                        DoctorId = doctorId, 
                        DayOfWeek = DayOfWeek.Friday, 
                        StartTime = TimeSpan.FromHours(9), 
                        EndTime = TimeSpan.FromHours(17),
                        IsRecurring = true,
                        MaxAppointmentsPerSlot = 4
                    }
                };

                context.DoctorAvailabilities.AddRange(availabilities);
                context.SaveChanges();
            }
        }

        // ADD THIS METHOD - Seed Sample Consultations
        private static void SeedSampleConsultations(ApplicationDbContext context)
        {
            var doctor = context.Doctors.FirstOrDefault(d => d.UserId == context.Users.First(u => u.Username == "drjohn").Id);
            var patientRecord = context.PatientRecords.FirstOrDefault();
            var nurseUser = context.Users.FirstOrDefault(u => u.Username == "nursemary");

            if (doctor != null && patientRecord != null && !context.Consultations.Any())
            {
                var consultation = new Consultation
                {
                    PatientRecordId = patientRecord.Id,
                    DoctorId = doctor.Id,
                    NurseId = nurseUser?.Id,
                    ScheduledAt = DateTime.UtcNow.AddDays(1).Date.AddHours(10), // Tomorrow at 10 AM
                    Duration = TimeSpan.FromMinutes(30),
                    ConsultationType = ConsultationType.InPerson,
                    Status = AppointmentStatus.Confirmed,
                    Symptoms = "Regular health checkup and consultation",
                    Fee = doctor.ConsultationFee
                };

                context.Consultations.Add(consultation);
                context.SaveChanges();
            }
        }

        private static void SeedPermissions(ApplicationDbContext context)
        {
            var permissions = new[]
            {
                new { Name = "users.view", Description = "View users", Category = "Users" },
                new { Name = "users.create", Description = "Create users", Category = "Users" },
                new { Name = "users.edit", Description = "Edit users", Category = "Users" },
                new { Name = "users.delete", Description = "Delete users", Category = "Users" },
                
                new { Name = "roles.view", Description = "View roles", Category = "Roles" },
                new { Name = "roles.create", Description = "Create roles", Category = "Roles" },
                new { Name = "roles.edit", Description = "Edit roles", Category = "Roles" },
                new { Name = "roles.delete", Description = "Delete roles", Category = "Roles" },
                
                new { Name = "patients.view", Description = "View patients", Category = "Patients" },
                new { Name = "patients.create", Description = "Create patients", Category = "Patients" },
                new { Name = "patients.edit", Description = "Edit patients", Category = "Patients" },
                new { Name = "patients.delete", Description = "Delete patients", Category = "Patients" },
                
                new { Name = "prescriptions.create", Description = "Create prescriptions", Category = "Prescriptions" },
                new { Name = "prescriptions.view", Description = "View prescriptions", Category = "Prescriptions" },
                new { Name = "prescriptions.edit", Description = "Edit prescriptions", Category = "Prescriptions" },
                
                new { Name = "vitals.create", Description = "Create vitals", Category = "Vitals" },
                new { Name = "vitals.view", Description = "View vitals", Category = "Vitals" },
                new { Name = "vitals.edit", Description = "Edit vitals", Category = "Vitals" },
                
                new { Name = "testresults.create", Description = "Create test results", Category = "TestResults" },
                new { Name = "testresults.view", Description = "View test results", Category = "TestResults" },
                new { Name = "testresults.edit", Description = "Edit test results", Category = "TestResults" },

                // ADD NEW PERMISSIONS FOR CONSULTATION SYSTEM
                new { Name = "doctors.view", Description = "View doctors", Category = "Doctors" },
                new { Name = "doctors.create", Description = "Create doctors", Category = "Doctors" },
                new { Name = "doctors.edit", Description = "Edit doctors", Category = "Doctors" },
                new { Name = "consultations.book", Description = "Book consultations", Category = "Consultations" },
                new { Name = "consultations.view", Description = "View consultations", Category = "Consultations" },
                new { Name = "consultations.update", Description = "Update consultations", Category = "Consultations" },
                new { Name = "availability.manage", Description = "Manage availability", Category = "Doctors" }
            };

            foreach (var perm in permissions)
            {
                if (!context.Permissions.Any(p => p.Name == perm.Name))
                {
                    context.Permissions.Add(new Permission
                    {
                        Name = perm.Name,
                        Description = perm.Description,
                        Category = perm.Category
                    });
                }
            }
            
            context.SaveChanges();
        }

        private static void SeedAdminUser(ApplicationDbContext context, IPasswordHasher<User> hasher)
        {
            var superAdminRole = context.Roles.FirstOrDefault(r => r.Name == "SuperAdmin");
            if (superAdminRole == null)
            {
                throw new InvalidOperationException("SuperAdmin role not found. Please run database migrations first.");
            }

            var admin = new User
            {
                Username = "admin",
                Email = "admin@medicare.com", 
                PasswordHash = hasher.HashPassword(null!, "Admin@123"),
                IsEmailConfirmed = true, 
                CreatedAt = DateTime.UtcNow 
            };

            context.Users.Add(admin);
            context.SaveChanges();

            var adminUserRole = new UserRole
            {
                UserId = admin.Id,
                RoleId = superAdminRole.Id,
                AssignedAt = DateTime.UtcNow
            };
            context.UserRoles.Add(adminUserRole);
            context.SaveChanges();
        }

        private static void SeedSampleUsers(ApplicationDbContext context, IPasswordHasher<User> hasher)
        {
            var doctorRole = context.Roles.FirstOrDefault(r => r.Name == "Doctor");
            var nurseRole = context.Roles.FirstOrDefault(r => r.Name == "Nurse");
            var patientRole = context.Roles.FirstOrDefault(r => r.Name == "Patient");

            // Create doctor user
            if (!context.Users.Any(u => u.Username == "drjohn"))
            {
                var doctor = new User
                {
                    Username = "drjohn",
                    Email = "drjohn@medicare.com",
                    PasswordHash = hasher.HashPassword(null!, "Doctor@123"),
                    IsEmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                context.Users.Add(doctor);
                context.SaveChanges();

                if (doctorRole != null)
                {
                    context.UserRoles.Add(new UserRole
                    {
                        UserId = doctor.Id,
                        RoleId = doctorRole.Id,
                        AssignedAt = DateTime.UtcNow
                    });
                    context.SaveChanges();
                }
            }

            // Create nurse user
            if (!context.Users.Any(u => u.Username == "nursemary"))
            {
                var nurse = new User
                {
                    Username = "nursemary",
                    Email = "nursemary@medicare.com", 
                    PasswordHash = hasher.HashPassword(null!, "Nurse@123"),
                    IsEmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                context.Users.Add(nurse);
                context.SaveChanges();

                if (nurseRole != null)
                {
                    context.UserRoles.Add(new UserRole
                    {
                        UserId = nurse.Id,
                        RoleId = nurseRole.Id,
                        AssignedAt = DateTime.UtcNow
                    });
                    context.SaveChanges();
                }
            }

            // Create patient user with patient record
            if (!context.Users.Any(u => u.Username == "patient1"))
            {
                SeedPatientUser(context, hasher, patientRole);
            }
        }

        private static void SeedPatientUser(ApplicationDbContext context, IPasswordHasher<User> hasher, Role? patientRole)
        {
            var patientRecord = new PatientRecord
            {
                Id = Guid.NewGuid(),
                FullName = "John Patient"
            };

            context.PatientRecords.Add(patientRecord);
            context.SaveChanges();

            var patient = new User
            {
                Username = "patient1",
                Email = "patient1@medicare.com",
                PasswordHash = hasher.HashPassword(null!, "Patient@123"),
                PatientProfileId = patientRecord.Id,
                IsEmailConfirmed = true, // Set to true for testing
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(patient);
            context.SaveChanges();

            if (patientRole != null)
            {
                context.UserRoles.Add(new UserRole
                {
                    UserId = patient.Id,
                    RoleId = patientRole.Id,
                    AssignedAt = DateTime.UtcNow
                });
                context.SaveChanges();
            }

            SeedSampleAssignments(context, patientRecord.Id);
        }

        private static void SeedSampleAssignments(ApplicationDbContext context, Guid patientRecordId)
        {
            var doctor = context.Users.FirstOrDefault(u => u.Username == "drjohn");
            var nurse = context.Users.FirstOrDefault(u => u.Username == "nursemary");

            if (doctor != null && !context.UserPatientAssignments.Any(a => a.UserId == doctor.Id && a.PatientRecordId == patientRecordId))
            {
                context.UserPatientAssignments.Add(new UserPatientAssignment
                {
                    UserId = doctor.Id,
                    PatientRecordId = patientRecordId
                });
            }

            if (nurse != null && !context.UserPatientAssignments.Any(a => a.UserId == nurse.Id && a.PatientRecordId == patientRecordId))
            {
                context.UserPatientAssignments.Add(new UserPatientAssignment
                {
                    UserId = nurse.Id,
                    PatientRecordId = patientRecordId
                });
            }

            context.SaveChanges();
        }

        private static void SeedRoles(ApplicationDbContext context)
        {
            var requiredRoles = new[] 
            { 
                "SuperAdmin", 
                "Doctor", 
                "Nurse", 
                "Patient" 
            };
            
            foreach (var roleName in requiredRoles)
            {
                if (!context.Roles.Any(r => r.Name == roleName))
                {
                    var role = new Role
                    {
                        Name = roleName,
                        Description = roleName switch
                        {
                            "SuperAdmin" => "Full system access",
                            "Doctor" => "Medical doctor role",
                            "Nurse" => "Nursing staff role", 
                            "Patient" => "Patient role",
                            _ => $"{roleName} role"
                        },
                        IsSystemRole = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.Roles.Add(role);
                }
            }
            
            context.SaveChanges();
        }
    }
}