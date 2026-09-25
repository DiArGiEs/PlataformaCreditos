using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Models;

namespace PlataformaCreditos.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            await context.Database.MigrateAsync();

            string roleAnalista = "Analista";
            if (!await roleManager.RoleExistsAsync(roleAnalista))
            {
                await roleManager.CreateAsync(new IdentityRole(roleAnalista));
            }

            string analistaEmail = "analista@banco.com";
            var userAnalista = await userManager.FindByEmailAsync(analistaEmail);
            if (userAnalista == null)
            {
                userAnalista = new IdentityUser
                {
                    UserName = analistaEmail,
                    Email = analistaEmail,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(userAnalista, "Analista123!");
                await userManager.AddToRoleAsync(userAnalista, roleAnalista);
            }

            string cliente1Email = "cliente1@gmail.com";
            var userCliente1 = await userManager.FindByEmailAsync(cliente1Email);
            if (userCliente1 == null)
            {
                userCliente1 = new IdentityUser
                {
                    UserName = cliente1Email,
                    Email = cliente1Email,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(userCliente1, "Cliente123!");
            }

            string cliente2Email = "cliente2@gmail.com";
            var userCliente2 = await userManager.FindByEmailAsync(cliente2Email);
            if (userCliente2 == null)
            {
                userCliente2 = new IdentityUser
                {
                    UserName = cliente2Email,
                    Email = cliente2Email,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(userCliente2, "Cliente123!");
            }

            if (!await context.Clientes.AnyAsync())
            {
                var cliente1 = new Cliente
                {
                    UsuarioId = userCliente1.Id,
                    IngresosMensuales = 3000m,
                    Activo = true
                };

                var cliente2 = new Cliente
                {
                    UsuarioId = userCliente2.Id,
                    IngresosMensuales = 5000m,
                    Activo = true
                };

                context.Clientes.AddRange(cliente1, cliente2);
                await context.SaveChangesAsync();

                var solicitud1 = new SolicitudCredito
                {
                    ClienteId = cliente1.Id,
                    MontoSolicitado = 5000m,
                    FechaSolicitud = DateTime.UtcNow,
                    Estado = EstadoSolicitud.Pendiente
                };

                var solicitud2 = new SolicitudCredito
                {
                    ClienteId = cliente2.Id,
                    MontoSolicitado = 10000m,
                    FechaSolicitud = DateTime.UtcNow.AddDays(-2),
                    Estado = EstadoSolicitud.Aprobado
                };

                context.SolicitudesCredito.AddRange(solicitud1, solicitud2);
                await context.SaveChangesAsync();
            }
        }
    }
}