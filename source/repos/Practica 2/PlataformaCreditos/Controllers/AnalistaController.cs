using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Data;
using PlataformaCreditos.Models;
using PlataformaCreditos.Models.ViewModels;
using System.Runtime.InteropServices;

namespace PlataformaCreditos.Controllers
{
    [Authorize(Roles = "Analista")]
    [Route("Analista")]
    public class AnalistaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AnalistaController(ApplicationDbContext context)
        {
            _context = context;
        }


        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var pendientes = await _context.SolicitudesCredito
                .Include(s => s.Cliente)
                .Where(s => s.Estado == EstadoSolicitud.Pendiente)
                .OrderBy(s => s.FechaSolicitud)
                .ToListAsync();

            return View(pendientes);
        }


        [HttpGet("Evaluar/{id}")]
        public async Task<IActionResult> Evaluar(int? id)
        {
            if (id == null) return NotFound();

            var solicitud = await _context.SolicitudesCredito
                .Include(s => s.Cliente)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (solicitud == null) return NotFound();

            if (solicitud.Estado != EstadoSolicitud.Pendiente)
            {
                TempData["MensajeError"] = "Esta solicitud ya fue procesada anteriormente.";
                return RedirectToAction(nameof(Index));
            }

            var model = new AnalistaEvaluarViewModel
            {
                SolicitudId = solicitud.Id,
                Solicitud = solicitud
            };

            return View(model);
        }


        [HttpPost("Evaluar/{id?}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Evaluar(AnalistaEvaluarViewModel model)
        {
            var solicitud = await _context.SolicitudesCredito
                .Include(s => s.Cliente)
                .FirstOrDefaultAsync(s => s.Id == model.SolicitudId);

            if (solicitud == null) return NotFound();

            if (solicitud.Estado != EstadoSolicitud.Pendiente)
            {
                ModelState.AddModelError("", "La solicitud ya fue aprobada o rechazada previamente.");
            }

            if (model.Accion == "Rechazar" && string.IsNullOrWhiteSpace(model.MotivoRechazo))
            {
                ModelState.AddModelError("MotivoRechazo", "El motivo de rechazo es obligatorio.");
            }

            if (model.Accion == "Aprobar" && solicitud.Cliente != null)
            {
                decimal limiteAprobacion = solicitud.Cliente.IngresosMensuales * 5m;
                if (solicitud.MontoSolicitado > limiteAprobacion)
                {
                    ModelState.AddModelError("", $"No se puede aprobar: El monto solicitado ({solicitud.MontoSolicitado:C}) supera el límite máximo permitido para aprobación de 5 veces el ingreso del cliente ({limiteAprobacion:C}).");
                }
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

                TempData["MensajeExito"] = $"La solicitud #{solicitud.Id} se procesó correctamente como {solicitud.Estado}.";
                return RedirectToAction(nameof(Index));
            }

            model.Solicitud = solicitud;
            return View(model);
        }
    }
}