

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace MediCare.Models.Data
{
    public static class DataSeeder
    {
        public static void Seed(ApplicationDbContext context, IPasswordHasher<User> hasher)
        {
            // Ensure database is created and migrations are applied
            context.Database.EnsureCreated();

            // Seed in correct order: Permissions -> Roles -> Users
            SeedPermissions(context); // ADD THIS LINE - MUST BE BEFORE ROLES
            SeedRoles(context);

            // Seed default admin user
            if (!context.Users.Any(u => u.Username == "admin"))
            {
                SeedAdminUser(context, hasher);
                SeedSampleUsers(context, hasher);
            }
        }

        // ADD THIS ENTIRE METHOD
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
                new { Name = "testresults.edit", Description = "Edit test results", Category = "TestResults" }
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
                // This shouldn't happen if roles were seeded properly
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

            // Assign SuperAdmin role to admin user
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
            // Get roles
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
            // First create patient record
            var patientRecord = new PatientRecord
            {
                Id = Guid.NewGuid(),
                FullName = "John Patient"
            };

            context.PatientRecords.Add(patientRecord);
            context.SaveChanges();

            // Then create patient user
            var patient = new User
            {
                Username = "patient1",
                Email = "patient1@medicare.com",
                PasswordHash = hasher.HashPassword(null!, "Patient@123"),
                PatientProfileId = patientRecord.Id, // Link to patient record
                IsEmailConfirmed = false, // Patient needs to confirm email
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

            // Create sample assignments for doctors/nurses to this patient
            SeedSampleAssignments(context, patientRecord.Id);
        }

        private static void SeedSampleAssignments(ApplicationDbContext context, Guid patientRecordId)
        {
            // Get doctor and nurse users
            var doctor = context.Users.FirstOrDefault(u => u.Username == "drjohn");
            var nurse = context.Users.FirstOrDefault(u => u.Username == "nursemary");

            // Assign doctor to patient
            if (doctor != null && !context.UserPatientAssignments.Any(a => a.UserId == doctor.Id && a.PatientRecordId == patientRecordId))
            {
                context.UserPatientAssignments.Add(new UserPatientAssignment
                {
                    UserId = doctor.Id,
                    PatientRecordId = patientRecordId
                });
            }

            // Assign nurse to patient
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