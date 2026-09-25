using System.ComponentModel.DataAnnotations;
using PlataformaCreditos.Models;

namespace PlataformaCreditos.Models.ViewModels
{
    public class AnalistaEvaluarViewModel
    {
        public int SolicitudId { get; set; }

        public SolicitudCredito? Solicitud { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una acción (Aprobar o Rechazar).")]
        public string Accion { get; set; } = "Aprobar";

        public string? MotivoRechazo { get; set; }
    }
}