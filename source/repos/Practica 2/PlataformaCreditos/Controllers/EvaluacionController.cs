using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Data;
using PlataformaCreditos.Models;
using PlataformaCreditos.Models.ViewModels;

namespace PlataformaCreditos.Controllers
{
    [Authorize(Roles = "Analista")]
    public class EvaluacionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EvaluacionController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Pendientes()
        {
            var pendientes = await _context.SolicitudesCredito
                .Include(s => s.Cliente)
                .Where(s => s.Estado == EstadoSolicitud.Pendiente)
                .OrderBy(s => s.FechaSolicitud)
                .ToListAsync();

            return View(pendientes);
        }

        public async Task<IActionResult> Evaluar(int? id)
        {
            if (id == null) return NotFound();

            var solicitud = await _context.SolicitudesCredito
                .Include(s => s.Cliente)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (solicitud == null) return NotFound();

            if (solicitud.Estado != EstadoSolicitud.Pendiente)
            {
                TempData["MensajeError"] = "Esta solicitud ya ha sido evaluada previamente.";
                return RedirectToAction(nameof(Pendientes));
            }

            var model = new EvaluacionSolicitudViewModel
            {
                SolicitudId = solicitud.Id,
                Solicitud = solicitud
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Evaluar(EvaluacionSolicitudViewModel model)
        {
            var solicitud = await _context.SolicitudesCredito
                .Include(s => s.Cliente)
                .FirstOrDefaultAsync(s => s.Id == model.SolicitudId);

            if (solicitud == null) return NotFound();

            if (solicitud.Estado != EstadoSolicitud.Pendiente)
            {
                ModelState.AddModelError("", "La solicitud ya fue procesada anteriormente.");
            }

            if (model.Accion == "Rechazar" && string.IsNullOrWhiteSpace(model.MotivoRechazo))
            {
                ModelState.AddModelError("MotivoRechazo", "El motivo de rechazo es obligatorio al rechazar una solicitud.");
            }

            if (ModelState.IsValid)
            {
                if (model.Accion == "Aprobar")
                {
                    solicitud.Estado = EstadoSolicitud.Aprobado;
                    solicitud.MotivoRechazo = null;
                }
                else if (model.Accion == "Rechazar")
                {
                    solicitud.Estado = EstadoSolicitud.Rechazado;
                    solicitud.MotivoRechazo = model.MotivoRechazo;
                }

                _context.Update(solicitud);
                await _context.SaveChangesAsync();

                TempData["MensajeExito"] = $"La solicitud #{solicitud.Id} fue evaluada correctamente como: {solicitud.Estado}.";
                return RedirectToAction(nameof(Pendientes));
            }

            model.Solicitud = solicitud;
            return View(model);
        }
    }
}