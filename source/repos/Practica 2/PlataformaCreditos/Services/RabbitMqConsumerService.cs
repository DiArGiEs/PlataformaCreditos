using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Data;
using PlataformaCreditos.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PlataformaCreditos.Services
{
    public class RabbitMqConsumerService : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RabbitMqConsumerService> _logger;

        public RabbitMqConsumerService(IConfiguration configuration, IServiceProvider serviceProvider, ILogger<RabbitMqConsumerService> logger)
        {
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            bool consumerEnabled = bool.Parse(_configuration["RabbitMq:ConsumerEnabled"] ?? "true");
            if (!consumerEnabled)
            {
                _logger.LogInformation("Consumidor de RabbitMQ desactivado por configuración.");
                return;
            }

            var connectionString = _configuration["RabbitMq:ConnectionString"] ?? _configuration.GetConnectionString("RabbitMq");
            var queueName = _configuration["RabbitMq:QueueName"] ?? "solicitudes.notificaciones";

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogWarning("RabbitMQ ConnectionString no configurada. El consumidor no iniciará.");
                return;
            }

            try
            {
                var factory = new ConnectionFactory { Uri = new Uri(connectionString) };
                var connection = await factory.CreateConnectionAsync(stoppingToken);
                var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await channel.QueueDeclareAsync(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: stoppingToken
                );

                var consumer = new AsyncEventingBasicConsumer(channel);

                consumer.ReceivedAsync += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var messageText = Encoding.UTF8.GetString(body);

                    try
                    {
                        using var doc = JsonDocument.Parse(messageText);
                        var root = doc.RootElement;

                        string messageId = root.GetProperty("MessageId").GetString()!;
                        int solicitudId = root.GetProperty("SolicitudId").GetInt32();
                        string usuarioId = root.GetProperty("UsuarioId").GetString()!;

                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                            bool existe = await dbContext.Notificaciones.AnyAsync(n => n.MessageId == messageId, cancellationToken: stoppingToken);
                            if (!existe)
                            {
                                var notificacion = new Notificacion
                                {
                                    MessageId = messageId,
                                    SolicitudId = solicitudId,
                                    UsuarioId = usuarioId,
                                    Texto = "Recibimos tu solicitud de crédito y está pendiente de evaluación.",
                                    FechaProcesamientoUtc = DateTime.UtcNow
                                };

                                dbContext.Notificaciones.Add(notificacion);
                                await dbContext.SaveChangesAsync(stoppingToken);
                            }
                        }

                        await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error procesando el mensaje de RabbitMQ.");
                        await channel.BasicRejectAsync(deliveryTag: ea.DeliveryTag, requeue: false, cancellationToken: stoppingToken);
                    }
                };

                await channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fatal al iniciar el consumidor de RabbitMQ.");
            }
        }
    }
}