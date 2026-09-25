using PlataformaCreditos.Models;

namespace PlataformaCreditos.Models.ViewModels
{
    public class SolicitudesFiltroViewModel
    {
        public List<SolicitudCredito> Solicitudes { get; set; } = new();

        // Filtros
        public EstadoSolicitud? Estado { get; set; }
        public decimal? MontoMin { get; set; }
        public decimal? MontoMax { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
    }
}