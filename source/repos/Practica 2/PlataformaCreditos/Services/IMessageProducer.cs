namespace PlataformaCreditos.Services
{
    public interface IMessageProducer
    {
        Task PublishSolicitudRegistradaAsync(int solicitudId, string usuarioId);
    }
}