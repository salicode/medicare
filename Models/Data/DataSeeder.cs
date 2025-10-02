// using Microsoft.AspNetCore.Identity;
// using System.Linq;

// namespace MediCare.Models.Data
// {
//     public static class DataSeeder
//     {
//         public static void Seed(ApplicationDbContext context, IPasswordHasher<User> hasher)
//         {
//             // Ensure database is created
//             context.Database.EnsureCreated();

//             // Seed default admin user
//             if (!context.Users.Any())
//             {
//                 var admin = new User
//                 {
//                     Username = "admin",
//                     PasswordHash = hasher.HashPassword(null!, "Admin@123"), // default password
//                     Role = Role.SystemAdmin
//                 };

//                 context.Users.Add(admin);
//                 context.SaveChanges();
//             }

            
//         }
//     }
// }


using Microsoft.AspNetCore.Identity;
using System.Linq;

namespace MediCare.Models.Data
{
    public static class DataSeeder
    {
        public static void Seed(ApplicationDbContext context, IPasswordHasher<User> hasher)
        {
            // Ensure database is created
            context.Database.EnsureCreated();

            // Seed default admin user
            if (!context.Users.Any())
            {
                var admin = new User
                {
                    Username = "admin",
                    Email = "admin@medicare.com", // ADDED: Email field
                    PasswordHash = hasher.HashPassword(null!, "Admin@123"),
                    Role = Role.SystemAdmin,
                    IsEmailConfirmed = true, // ADDED: Admin is pre-confirmed
                    CreatedAt = DateTime.UtcNow // ADDED: Creation timestamp
                };

                context.Users.Add(admin);
                context.SaveChanges();

                // Optional: Seed sample healthcare professionals
                var doctor = new User
                {
                    Username = "drjohn",
                    Email = "drjohn@medicare.com",
                    PasswordHash = hasher.HashPassword(null!, "Doctor@123"),
                    Role = Role.Doctor,
                    IsEmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                var nurse = new User
                {
                    Username = "nursemary",
                    Email = "nursemary@medicare.com", 
                    PasswordHash = hasher.HashPassword(null!, "Nurse@123"),
                    Role = Role.Nurse,
                    IsEmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                context.Users.AddRange(doctor, nurse);
                context.SaveChanges();

                // Optional: Seed a sample patient user
                var patient = new User
                {
                    Username = "patient1",
                    Email = "patient1@medicare.com",
                    PasswordHash = hasher.HashPassword(null!, "Patient@123"),
                    Role = Role.Patient,
                    IsEmailConfirmed = false, // Patient needs to confirm email
                    CreatedAt = DateTime.UtcNow
                };

                context.Users.Add(patient);
                context.SaveChanges();

                // Optional: Seed sample patient record
                var patientRecord = new PatientRecord
                {
                    Id = Guid.NewGuid(),
                    FullName = "John Patient"
                };

                context.PatientRecords.Add(patientRecord);
                context.SaveChanges();

                // Link patient user to patient record
                patient.PatientProfileId = patientRecord.Id;
                context.SaveChanges();
            }
        }
    }
}
