using System.ComponentModel.DataAnnotations;
using PlataformaCreditos.Models;

namespace PlataformaCreditos.Models.ViewModels
{
    public class EvaluacionSolicitudViewModel
    {
        public int SolicitudId { get; set; }
        public SolicitudCredito? Solicitud { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una acción.")]
        public string Accion { get; set; } = string.Empty; // "Aprobar" o "Rechazar"

        public string? MotivoRechazo { get; set; }
    }
}