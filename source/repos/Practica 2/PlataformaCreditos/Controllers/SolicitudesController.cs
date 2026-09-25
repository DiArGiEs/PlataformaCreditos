using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Data;
using PlataformaCreditos.Models;
using PlataformaCreditos.Models.ViewModels;
using System.Runtime.InteropServices;
using System.Security.Claims;

namespace PlataformaCreditos.Controllers
{
    [Authorize]
    public class SolicitudesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SolicitudesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> MisSolicitudes(SolicitudesFiltroViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == userId);

            if (cliente == null)
            {
                model.Solicitudes = new List<SolicitudCredito>();
                return View(model);
            }

            if (model.MontoMin < 0 || model.MontoMax < 0)
            {
                ModelState.AddModelError("", "Los montos de búsqueda no pueden ser negativos.");
            }

            if (model.FechaInicio.HasValue && model.FechaFin.HasValue && model.FechaInicio > model.FechaFin)
            {
                ModelState.AddModelError("", "La fecha de inicio no puede ser posterior a la fecha de fin.");
            }

            var query = _context.SolicitudesCredito
                .Include(s => s.Cliente)
                .Where(s => s.ClienteId == cliente.Id);

            if (ModelState.IsValid)
            {
                if (model.Estado.HasValue)
                {
                    query = query.Where(s => s.Estado == model.Estado.Value);
                }

                if (model.MontoMin.HasValue)
                {
                    query = query.Where(s => s.MontoSolicitado >= model.MontoMin.Value);
                }

                if (model.MontoMax.HasValue)
                {
                    query = query.Where(s => s.MontoSolicitado <= model.MontoMax.Value);
                }

                if (model.FechaInicio.HasValue)
                {
                    query = query.Where(s => s.FechaSolicitud >= model.FechaInicio.Value);
                }

                if (model.FechaFin.HasValue)
                {
                    var fechaFinFinDia = model.FechaFin.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(s => s.FechaSolicitud <= fechaFinFinDia);
                }
            }

            model.Solicitudes = await query.OrderByDescending(s => s.FechaSolicitud).ToListAsync();
            return View(model);
        }

        public async Task<IActionResult> Detalle(int? id)
        {
            if (id == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var solicitud = await _context.SolicitudesCredito
                .Include(s => s.Cliente)
                .FirstOrDefaultAsync(m => m.Id == id && m.Cliente != null && m.Cliente.UsuarioId == userId);

            if (solicitud == null) return NotFound();

            return View(solicitud);
        }

        public IActionResult Crear()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear([Bind("MontoSolicitado")] SolicitudCredito solicitud)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == userId);

            if (cliente == null || !cliente.Activo)
            {
                ModelState.AddModelError("", "El usuario no está registrado como cliente activo.");
                return View(solicitud);
            }

            if (solicitud.MontoSolicitado <= 0)
            {
                ModelState.AddModelError("MontoSolicitado", "El monto solicitado debe ser mayor a cero.");
            }

            bool tienePendiente = await _context.SolicitudesCredito
                .AnyAsync(s => s.ClienteId == cliente.Id && s.Estado == EstadoSolicitud.Pendiente);

            if (tienePendiente)
            {
                ModelState.AddModelError("", "Ya cuenta con una solicitud de crédito en estado Pendiente.");
            }

            decimal limiteMonto = cliente.IngresosMensuales * 10;
            if (solicitud.MontoSolicitado > limiteMonto)
            {
                ModelState.AddModelError("", $"El monto solicitado ({solicitud.MontoSolicitado:C}) no puede superar 10 veces sus ingresos mensuales ({limiteMonto:C}).");
            }

            if (ModelState.IsValid)
            {
                solicitud.ClienteId = cliente.Id;
                solicitud.FechaSolicitud = DateTime.UtcNow;
                solicitud.Estado = EstadoSolicitud.Pendiente;

                _context.Add(solicitud);
                await _context.SaveChangesAsync();

                TempData["MensajeExito"] = "¡Solicitud registrada correctamente en estado Pendiente!";
                return RedirectToAction(nameof(Crear));
            }

            return View(solicitud);
        }
    }
}