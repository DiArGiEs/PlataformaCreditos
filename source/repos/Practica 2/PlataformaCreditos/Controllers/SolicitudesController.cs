using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Data;
using PlataformaCreditos.Models;
using PlataformaCreditos.Services;

namespace PlataformaCreditos.Controllers
{
    [Authorize]
    public class SolicitudesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IMessageProducer _messageProducer;

        public SolicitudesController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IMessageProducer messageProducer)
        {
            _context = context;
            _userManager = userManager;
            _messageProducer = messageProducer;
        }

        // GET: /Solicitudes/MisSolicitudes
        public async Task<IActionResult> MisSolicitudes(string? estado, decimal? montoMin, decimal? montoMax, DateTime? fechaInicio, DateTime? fechaFin)
        {
            var userId = _userManager.GetUserId(User);

            // Validaciones server-side para filtros de fecha y monto
            if (montoMin < 0 || montoMax < 0)
            {
                ModelState.AddModelError("", "Los montos de búsqueda no pueden ser negativos.");
            }

            if (fechaInicio.HasValue && fechaFin.HasValue && fechaInicio > fechaFin)
            {
                ModelState.AddModelError("", "La fecha de inicio no puede ser posterior a la fecha de fin.");
            }

            var query = _context.SolicitudesCredito
                .Include(s => s.Cliente)
                .Where(s => s.Cliente != null && s.Cliente.UsuarioId == userId)
                .AsQueryable();

            // Aplicar Filtros
            if (!string.IsNullOrEmpty(estado) && Enum.TryParse<EstadoSolicitud>(estado, true, out var estadoEnum))
            {
                query = query.Where(s => s.Estado == estadoEnum);
            }

            if (montoMin.HasValue)
            {
                query = query.Where(s => s.MontoSolicitado >= montoMin.Value);
            }

            if (montoMax.HasValue)
            {
                query = query.Where(s => s.MontoSolicitado <= montoMax.Value);
            }

            if (fechaInicio.HasValue)
            {
                query = query.Where(s => s.FechaSolicitud >= fechaInicio.Value.Date);
            }

            if (fechaFin.HasValue)
            {
                query = query.Where(s => s.FechaSolicitud <= fechaFin.Value.Date.AddDays(1).AddTicks(-1));
            }

            var lista = await query.OrderByDescending(s => s.FechaSolicitud).ToListAsync();

            ViewBag.EstadoActual = estado;
            ViewBag.MontoMin = montoMin;
            ViewBag.MontoMax = montoMax;
            ViewBag.FechaInicio = fechaInicio?.ToString("yyyy-MM-dd");
            ViewBag.FechaFin = fechaFin?.ToString("yyyy-MM-dd");

            return View(lista);
        }

        // GET: /Solicitudes/Detalle/5
        public async Task<IActionResult> Detalle(int? id)
        {
            if (id == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var solicitud = await _context.SolicitudesCredito
                .Include(s => s.Cliente)
                .FirstOrDefaultAsync(s => s.Id == id && s.Cliente != null && s.Cliente.UsuarioId == userId);

            if (solicitud == null) return NotFound();

            return View(solicitud);
        }

        // GET: /Solicitudes/Crear
        public IActionResult Crear()
        {
            return View();
        }

        // POST: /Solicitudes/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(SolicitudCredito solicitud)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            // Obtener el cliente asociado al usuario autenticado
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == userId);

            if (cliente == null)
            {
                ModelState.AddModelError("", "No se encontró un perfil de cliente asociado a tu usuario.");
                return View(solicitud);
            }

            if (!cliente.Activo)
            {
                ModelState.AddModelError("", "Tu perfil de cliente no está activo para registrar nuevas solicitudes.");
                return View(solicitud);
            }

            // Validar que no exista otra solicitud en estado Pendiente
            bool tienePendiente = await _context.SolicitudesCredito
                .AnyAsync(s => s.ClienteId == cliente.Id && s.Estado == EstadoSolicitud.Pendiente);

            if (tienePendiente)
            {
                ModelState.AddModelError("", "Ya tienes una solicitud de crédito en estado Pendiente. Debes esperar a que sea evaluada.");
            }

            // Validar restricción de monto máximo (no superar 10 veces el ingreso mensual para el registro)
            decimal limiteMaximoRegistro = cliente.IngresosMensuales * 10m;
            if (solicitud.MontoSolicitado > limiteMaximoRegistro)
            {
                ModelState.AddModelError("MontoSolicitado", $"El monto solicitado ({solicitud.MontoSolicitado:C}) supera el límite permitido de 10 veces tus ingresos mensuales ({limiteMaximoRegistro:C}).");
            }

            if (solicitud.MontoSolicitado <= 0)
            {
                ModelState.AddModelError("MontoSolicitado", "El monto solicitado debe ser mayor a 0.");
            }

            if (ModelState.IsValid)
            {
                solicitud.ClienteId = cliente.Id;
                solicitud.FechaSolicitud = DateTime.UtcNow;
                solicitud.Estado = EstadoSolicitud.Pendiente;
                solicitud.MotivoRechazo = null;

                _context.SolicitudesCredito.Add(solicitud);
                await _context.SaveChangesAsync();

                // Publicar mensaje asíncrono a Cloud MQ (RabbitMQ) (Pregunta 7)
                await _messageProducer.PublishSolicitudRegistradaAsync(solicitud.Id, userId);

                TempData["MensajeExito"] = $"Solicitud #{solicitud.Id} registrada exitosamente y enviada a evaluación.";
                return RedirectToAction(nameof(MisSolicitudes));
            }

            return View(solicitud);
        }
    }
}