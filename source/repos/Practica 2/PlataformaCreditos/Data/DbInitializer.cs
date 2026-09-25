using Microsoft.AspNetCore.Identity;
using PlataformaCreditos.Models;

namespace PlataformaCreditos.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            // 1. Crear Roles si no existen
            string[] roles = { "Administrador", "Analista", "Cliente" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Crear usuario Analista
            var analistaEmail = "analista@gmail.com";
            var analistaUser = await userManager.FindByEmailAsync(analistaEmail);

            if (analistaUser == null)
            {
                analistaUser = new IdentityUser
                {
                    UserName = analistaEmail,
                    Email = analistaEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(analistaUser, "Analista123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(analistaUser, "Analista");
                }
            }
            else
            {
                if (!await userManager.IsInRoleAsync(analistaUser, "Analista"))
                {
                    await userManager.AddToRoleAsync(analistaUser, "Analista");
                }
            }
        }
    }
}