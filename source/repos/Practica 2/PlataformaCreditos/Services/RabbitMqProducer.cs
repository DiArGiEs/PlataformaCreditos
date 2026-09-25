using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace PlataformaCreditos.Services
{
    public class RabbitMqProducer : IMessageProducer
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitMqProducer> _logger;

        public RabbitMqProducer(IConfiguration configuration, ILogger<RabbitMqProducer> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task PublishSolicitudRegistradaAsync(int solicitudId, string usuarioId)
        {
            var connectionString = _configuration["RabbitMq:ConnectionString"] ?? _configuration.GetConnectionString("RabbitMq");
            var queueName = _configuration["RabbitMq:QueueName"] ?? "solicitudes.notificaciones";

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogWarning("RabbitMQ ConnectionString no configurada. Omitiendo publicación.");
                return;
            }

            try
            {
                var factory = new ConnectionFactory { Uri = new Uri(connectionString) };
                using var connection = await factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();

                await channel.QueueDeclareAsync(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                var evento = new
                {
                    MessageId = Guid.NewGuid().ToString(),
                    SolicitudId = solicitudId,
                    UsuarioId = usuarioId,
                    FechaEventoUtc = DateTime.UtcNow
                };

                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evento));

                var properties = new BasicProperties
                {
                    Persistent = true
                };

                await channel.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: queueName,
                    mandatory: true,
                    basicProperties: properties,
                    body: body
                );

                _logger.LogInformation("Mensaje SolicitudRegistrada publicado en RabbitMQ para Solicitud #{SolicitudId}", solicitudId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falla al publicar mensaje en Cloud MQ para Solicitud #{SolicitudId}", solicitudId);
            }
        }
    }
}