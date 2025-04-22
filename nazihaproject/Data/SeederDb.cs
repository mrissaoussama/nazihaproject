using System.Linq;
using System.Collections.Generic;
using NazihaProject.Models;

namespace NazihaProject.Data
{
    public static class SeederDb
    {
        public static void Seed(AppDbContext context)
        {
            // Exit if any user already exists
            if (context.Users.Any())
            {
                return;
            }

            // Create admin (using Responsable role)
            var admin = new User
            {
                Username = "admin",
                FirstName = "Admin",
                LastName = "User",
                Email = "admin@example.com",
                Password = "admin123",
                Matricule = "m1",
                Roles = new List<RoleType> { RoleType.Responsable }
            };

            // Create technician for Poste Eau
            var techEau = new User
            {
                Username = "tech_eau",
                FirstName = "Tech",
                LastName = "Eau",
                Email = "tech_eau@example.com",
                Password = "tech123",                Matricule = "m2",

                Roles = new List<RoleType> { RoleType.TechnicienPosteEau }
            };

            // Create technician for Poste Coloration
            var techColoration = new User
            {
                Username = "tech_coloration",
                FirstName = "Tech",
                LastName = "Coloration",
                Email = "tech_coloration@example.com",
                Password = "tech123",                Matricule = "m2",

                Roles = new List<RoleType> { RoleType.TechnicienPosteColoration }
            };

            // Create technician for Poste Purete
            var techPurete = new User
            {
                Username = "tech_purete",
                FirstName = "Tech",
                LastName = "Purete",
                Email = "tech_purete@example.com",
                Password = "tech123",                Matricule = "m3",

                Roles = new List<RoleType> { RoleType.TechnicienPostePurete }
            };

            // Add new users to the context and save changes
            context.Users.AddRange(admin, techEau, techColoration, techPurete);
            context.SaveChanges();
        }
    }
}
