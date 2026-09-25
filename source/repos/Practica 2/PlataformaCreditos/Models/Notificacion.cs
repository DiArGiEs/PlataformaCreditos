using System.ComponentModel.DataAnnotations;

namespace PlataformaCreditos.Models
{
    public class Notificacion
    {
        public int Id { get; set; }

        [Required]
        public string MessageId { get; set; } = string.Empty;

        public int SolicitudId { get; set; }

        public string UsuarioId { get; set; } = string.Empty;

        [Required]
        public string Texto { get; set; } = string.Empty;

        public DateTime FechaProcesamientoUtc { get; set; } = DateTime.UtcNow;
    }
}